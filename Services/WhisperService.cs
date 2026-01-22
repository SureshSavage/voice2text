using System.Diagnostics;

namespace VoiceToText.Services;

public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public TranscriptionStats Stats { get; set; } = new();
}

public class TranscriptionStats
{
    public double ProcessingTimeMs { get; set; }
    public long AudioFileSizeBytes { get; set; }
    public double AudioDurationSeconds { get; set; }
}

public class ModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public long ModelSizeBytes { get; set; }
    public string ModelSizeFormatted { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
}

public interface IWhisperService
{
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language);
    bool IsAvailable();
    ModelInfo GetModelInfo();
}

public class WhisperService : IWhisperService
{
    private readonly ILogger<WhisperService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _whisperPath;
    private readonly string _modelPath;
    private readonly string _tempDir;

    public WhisperService(ILogger<WhisperService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Configure paths - can be overridden in appsettings.json
        _whisperPath = _configuration["Whisper:ExecutablePath"] ?? "/usr/local/bin/whisper";
        _modelPath = _configuration["Whisper:ModelPath"] ?? "models/ggml-base.en.bin";
        _tempDir = Path.Combine(Path.GetTempPath(), "voice-to-text");

        Directory.CreateDirectory(_tempDir);
    }

    public bool IsAvailable()
    {
        // Check if whisper.cpp executable exists
        if (File.Exists(_whisperPath))
            return true;

        // Also check common locations
        var commonPaths = new[]
        {
            "/usr/local/bin/whisper",
            "/opt/homebrew/bin/whisper",
            "./whisper.cpp/build/bin/whisper-cli",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "whisper.cpp", "build", "bin", "whisper-cli"),
            "./whisper.cpp/main"
        };

        return commonPaths.Any(File.Exists);
    }

    public ModelInfo GetModelInfo()
    {
        var info = new ModelInfo
        {
            ModelPath = _modelPath,
            ModelName = Path.GetFileName(_modelPath)
        };

        if (File.Exists(_modelPath))
        {
            var fileInfo = new FileInfo(_modelPath);
            info.ModelSizeBytes = fileInfo.Length;
            info.ModelSizeFormatted = FormatBytes(fileInfo.Length);
        }

        return info;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language)
    {
        var inputPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.webm");
        var wavPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.wav");
        var outputPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}");
        var stopwatch = Stopwatch.StartNew();
        var stats = new TranscriptionStats();

        try
        {
            // Save the uploaded audio
            await using (var fileStream = File.Create(inputPath))
            {
                await audioStream.CopyToAsync(fileStream);
            }
            stats.AudioFileSizeBytes = new FileInfo(inputPath).Length;

            // Convert to WAV format (16kHz mono) using ffmpeg
            var convertResult = await RunProcessAsync("ffmpeg",
                $"-i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{wavPath}\" -y");

            if (!convertResult.Success)
            {
                _logger.LogError("FFmpeg conversion failed: {Error}", convertResult.Error);
                throw new Exception("Failed to convert audio format. Make sure ffmpeg is installed.");
            }

            // Calculate audio duration from WAV file (16kHz, mono, 16-bit = 32000 bytes per second)
            if (File.Exists(wavPath))
            {
                var wavSize = new FileInfo(wavPath).Length;
                // WAV header is 44 bytes, 16kHz mono 16-bit = 32000 bytes/sec
                stats.AudioDurationSeconds = Math.Max(0, (wavSize - 44) / 32000.0);
            }

            // Run whisper.cpp
            var whisperArgs = $"-m \"{_modelPath}\" -f \"{wavPath}\" -l {language} -otxt -of \"{outputPath}\"";
            var whisperResult = await RunProcessAsync(_whisperPath, whisperArgs);

            if (!whisperResult.Success)
            {
                _logger.LogError("Whisper transcription failed: {Error}", whisperResult.Error);
                throw new Exception("Whisper transcription failed. Check server logs.");
            }

            stopwatch.Stop();
            stats.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;

            // Read the output
            var textFile = outputPath + ".txt";
            var text = string.Empty;
            if (File.Exists(textFile))
            {
                text = (await File.ReadAllTextAsync(textFile)).Trim();
            }
            else
            {
                text = whisperResult.Output?.Trim() ?? string.Empty;
            }

            return new TranscriptionResult { Text = text, Stats = stats };
        }
        finally
        {
            // Cleanup temporary files
            CleanupFile(inputPath);
            CleanupFile(wavPath);
            CleanupFile(outputPath + ".txt");
        }
    }

    private async Task<ProcessResult> RunProcessAsync(string fileName, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return new ProcessResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run process: {FileName}", fileName);
            return new ProcessResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private void CleanupFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup file: {Path}", path);
        }
    }

    private class ProcessResult
    {
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
    }
}
