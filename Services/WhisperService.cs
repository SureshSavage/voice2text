using System.Diagnostics;

namespace VoiceToText.Services;

public interface IWhisperService
{
    Task<string> TranscribeAsync(Stream audioStream, string language);
    bool IsAvailable();
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
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "whisper.cpp", "main"),
            "./whisper.cpp/main"
        };

        return commonPaths.Any(File.Exists);
    }

    public async Task<string> TranscribeAsync(Stream audioStream, string language)
    {
        var inputPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.webm");
        var wavPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.wav");
        var outputPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}");

        try
        {
            // Save the uploaded audio
            await using (var fileStream = File.Create(inputPath))
            {
                await audioStream.CopyToAsync(fileStream);
            }

            // Convert to WAV format (16kHz mono) using ffmpeg
            var convertResult = await RunProcessAsync("ffmpeg",
                $"-i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{wavPath}\" -y");

            if (!convertResult.Success)
            {
                _logger.LogError("FFmpeg conversion failed: {Error}", convertResult.Error);
                throw new Exception("Failed to convert audio format. Make sure ffmpeg is installed.");
            }

            // Run whisper.cpp
            var whisperArgs = $"-m \"{_modelPath}\" -f \"{wavPath}\" -l {language} -otxt -of \"{outputPath}\"";
            var whisperResult = await RunProcessAsync(_whisperPath, whisperArgs);

            if (!whisperResult.Success)
            {
                _logger.LogError("Whisper transcription failed: {Error}", whisperResult.Error);
                throw new Exception("Whisper transcription failed. Check server logs.");
            }

            // Read the output
            var textFile = outputPath + ".txt";
            if (File.Exists(textFile))
            {
                var text = await File.ReadAllTextAsync(textFile);
                return text.Trim();
            }

            return whisperResult.Output?.Trim() ?? string.Empty;
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
