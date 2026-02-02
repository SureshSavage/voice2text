using System.Diagnostics;

namespace VoiceToText.Services;

/// <summary>
/// DATA CLASS: Contains the result of a transcription operation.
///
/// CLASS TYPE: Data Class / DTO (Plain Old CLR Object)
/// - Simple data container with no business logic
/// - Holds transcribed text and performance statistics
/// </summary>
public class TranscriptionResult
{
    public string Text { get; set; } = string.Empty;
    public TranscriptionStats Stats { get; set; } = new();
}

/// <summary>
/// DATA CLASS: Contains statistics about a transcription operation.
///
/// CLASS TYPE: Data Class / DTO
/// - Performance metrics for transcription
/// - Processing time, file size, audio duration
/// </summary>
public class TranscriptionStats
{
    public double ProcessingTimeMs { get; set; }
    public long AudioFileSizeBytes { get; set; }
    public double AudioDurationSeconds { get; set; }
}

/// <summary>
/// DATA CLASS: Contains information about the Whisper model.
///
/// CLASS TYPE: Data Class / DTO
/// - Model metadata (name, size, path)
/// - Used for displaying model information in UI
/// </summary>
public class ModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public long ModelSizeBytes { get; set; }
    public string ModelSizeFormatted { get; set; } = string.Empty;
    public string ModelPath { get; set; } = string.Empty;
}

/// <summary>
/// INTERFACE: Defines the contract for Whisper transcription operations.
///
/// CLASS TYPE: Interface (Abstract Contract)
/// - Defines WHAT transcription operations must be implemented
/// - Enables dependency injection and unit testing
/// - Single implementation: WhisperService
///
/// IMPLEMENTATIONS:
/// - WhisperService: Uses whisper.cpp CLI for transcription
/// </summary>
public interface IWhisperService
{
    /// <summary>Transcribes audio from a stream to text.</summary>
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language);

    /// <summary>Checks if Whisper is available on the system.</summary>
    bool IsAvailable();

    /// <summary>Gets information about the loaded Whisper model.</summary>
    ModelInfo GetModelInfo();
}

/// <summary>
/// CONCRETE CLASS: Implements IWhisperService using whisper.cpp CLI.
///
/// CLASS TYPE: Concrete Class (Full Implementation)
/// - Implements IWhisperService interface
/// - Uses external whisper.cpp binary for transcription
/// - Handles audio format conversion via FFmpeg
///
/// KEY CHARACTERISTICS:
/// - IMPLEMENTS: IWhisperService interface
/// - Uses COMPOSITION: Contains ILogger and IConfiguration (HAS-A)
/// - Process management for external CLI tools
/// - Temporary file management with cleanup
///
/// EXTERNAL DEPENDENCIES:
/// - whisper.cpp: Native Whisper implementation (C++)
/// - FFmpeg: Audio format conversion (WebM -> WAV)
///
/// WORKFLOW:
/// 1. Save uploaded audio to temp file
/// 2. Convert to 16kHz WAV using FFmpeg
/// 3. Run whisper.cpp with language parameter
/// 4. Read output text file
/// 5. Cleanup temporary files
///
/// NESTED CLASSES:
/// - ProcessResult: Private class for process execution results
///
/// USAGE:
/// <code>
/// // Registration
/// builder.Services.AddSingleton&lt;IWhisperService, WhisperService&gt;();
///
/// // Usage
/// if (_whisperService.IsAvailable())
/// {
///     var result = await _whisperService.TranscribeAsync(audioStream, "en");
///     Console.WriteLine(result.Text);
/// }
/// </code>
/// </summary>
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
