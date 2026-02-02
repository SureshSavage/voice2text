# Voice to Text (.NET)

A web-based voice-to-text application built with ASP.NET Core that supports two transcription modes: Browser Speech API and Whisper (server-side).

## Features

- **Dual Transcription Modes**:
  - **Browser API**: Uses the Web Speech API (Chrome/Edge) for real-time transcription
  - **Whisper Mode**: Server-side transcription using OpenAI's Whisper model via [whisper.cpp](https://github.com/ggml-org/whisper.cpp)
- **Multi-language Support**: English, Spanish, French, German, Italian, Portuguese, Chinese, Japanese, Korean, Hindi, and Tamil
- **Technical Terms Auto-correction**: Automatically corrects common programming terms (e.g., "use state" -> "useState", "java script" -> "JavaScript")
- **Continuous Listening Mode**: Keep transcribing without manually restarting
- **Copy to Clipboard**: One-click copy of transcribed text
- **Word/Character Count**: Real-time statistics

## Class Types Reference

This section provides a comprehensive breakdown of all class types used in the Voice-to-Text application, including both the C#/.NET backend and Java/Android components.

### Class Types Overview

| Class Type | C# Count | Java Count | Purpose |
|------------|----------|------------|---------|
| **Interfaces** | 0 | 8+ | Callback contracts (Java) |
| **Abstract Classes** | 0 | 0 | N/A |
| **Concrete Classes** | 5+ | 15+ | Full implementations |
| **Static Classes** | 1+ | 2+ | Utilities |
| **Enum Types** | 0 | 3+ | Constants |
| **Record Classes** | 0 | 0 | N/A |
| **Model/Bean Classes** | 2+ | 10+ | Data structures |

---

### C# / .NET Classes

#### 1. Service Classes

```csharp
// Whisper transcription service
public class WhisperService
{
    private readonly IConfiguration _configuration;
    private readonly string _executablePath;
    private readonly string _modelPath;

    public WhisperService(IConfiguration configuration)
    {
        _configuration = configuration;
        _executablePath = configuration["Whisper:ExecutablePath"];
        _modelPath = configuration["Whisper:ModelPath"];
    }

    public bool IsAvailable()
    {
        return File.Exists(_executablePath) && File.Exists(_modelPath);
    }

    public async Task<string> TranscribeAsync(IFormFile audioFile, string language)
    {
        // 1. Save audio to temp file
        // 2. Convert to 16kHz WAV using FFmpeg
        // 3. Run whisper.cpp CLI
        // 4. Return transcribed text
    }
}
```

| C# Class | Location | Purpose |
|----------|----------|---------|
| `WhisperService` | Services/ | Audio transcription via whisper.cpp |
| `IndexModel` | Pages/ | Razor page model for main UI |

#### 2. Page Model Classes (Razor Pages)

```csharp
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        // Page load logic
    }
}
```

#### 3. Configuration/Startup Classes

```csharp
// Program.cs - minimal hosting model
var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddRazorPages();
builder.Services.AddSingleton<WhisperService>();

var app = builder.Build();

// API endpoints
app.MapPost("/api/transcribe", async (
    IFormFile audio,
    string language,
    WhisperService whisperService) =>
{
    var text = await whisperService.TranscribeAsync(audio, language);
    return Results.Json(new { text });
});

app.MapGet("/api/whisper/status", (WhisperService whisperService) =>
{
    return Results.Json(new { available = whisperService.IsAvailable() });
});

app.Run();
```

---

### Java Classes (Whisper.cpp JNA Bindings)

#### 1. Main JNA Wrapper Class

```java
// Main Whisper C++ wrapper using JNA
public class WhisperCpp {
    private WhisperContext context;
    private WhisperFullParams params;

    public WhisperCpp() {
        this.params = new WhisperFullParams();
    }

    public void initContext(String modelPath) throws IOException {
        WhisperContextParams ctxParams = WhisperCppJnaLibrary.INSTANCE
            .whisper_context_default_params();
        context = WhisperCppJnaLibrary.INSTANCE
            .whisper_init_from_file_with_params(modelPath, ctxParams);
    }

    public String transcribe(float[] audioData, int sampleCount) {
        WhisperCppJnaLibrary.INSTANCE.whisper_full(
            context, params, audioData, sampleCount);
        return extractText();
    }

    public void close() {
        if (context != null) {
            WhisperCppJnaLibrary.INSTANCE.whisper_free(context);
        }
    }
}
```

| Java Class | Package | Purpose |
|------------|---------|---------|
| `WhisperCpp` | io.github.ggerganov.whispercpp | Main wrapper for whisper.cpp |
| `WhisperCppJnaLibrary` | io.github.ggerganov.whispercpp.jna | JNA native library bindings |

#### 2. JNA Interface (Native Bindings)

```java
// JNA interface to native whisper.cpp library
public interface WhisperCppJnaLibrary extends Library {
    WhisperCppJnaLibrary INSTANCE = Native.load("whisper", WhisperCppJnaLibrary.class);

    // Context management
    WhisperContextParams whisper_context_default_params();
    WhisperContext whisper_init_from_file_with_params(String path, WhisperContextParams params);
    void whisper_free(WhisperContext ctx);

    // Transcription
    int whisper_full(WhisperContext ctx, WhisperFullParams params, float[] samples, int n_samples);
    int whisper_full_n_segments(WhisperContext ctx);
    String whisper_full_get_segment_text(WhisperContext ctx, int i_segment);

    // Callbacks
    void whisper_set_progress_callback(WhisperProgressCallback callback);
    void whisper_set_new_segment_callback(WhisperNewSegmentCallback callback);
}
```

#### 3. Callback Interfaces (Functional Interfaces)

```java
// Progress callback for transcription updates
@FunctionalInterface
public interface WhisperProgressCallback extends Callback {
    void invoke(WhisperContext ctx, WhisperState state, int progress, Pointer user_data);
}

// New segment callback when text is recognized
@FunctionalInterface
public interface WhisperNewSegmentCallback extends Callback {
    void invoke(WhisperContext ctx, WhisperState state, int n_new, Pointer user_data);
}

// Encoder begin callback
@FunctionalInterface
public interface WhisperEncoderBeginCallback extends Callback {
    boolean invoke(WhisperContext ctx, WhisperState state, Pointer user_data);
}

// Logits filter callback
@FunctionalInterface
public interface WhisperLogitsFilterCallback extends Callback {
    void invoke(WhisperContext ctx, WhisperState state, WhisperTokenData tokens,
                int n_tokens, FloatByReference logits, Pointer user_data);
}

// Abort callback
@FunctionalInterface
public interface GgmlAbortCallback extends Callback {
    boolean invoke(Pointer user_data);
}
```

| Callback Interface | Purpose |
|-------------------|---------|
| `WhisperProgressCallback` | Track transcription progress (0-100%) |
| `WhisperNewSegmentCallback` | Notify when new text segment recognized |
| `WhisperEncoderBeginCallback` | Called before encoder starts |
| `WhisperLogitsFilterCallback` | Filter/modify model output logits |
| `GgmlAbortCallback` | Allow aborting long operations |

#### 4. Parameter/Configuration Classes

```java
// Context initialization parameters
public class WhisperContextParams extends Structure {
    public boolean use_gpu;
    public int gpu_device;
    public boolean flash_attn;
    public boolean dtw_token_timestamps;

    @Override
    protected List<String> getFieldOrder() {
        return Arrays.asList("use_gpu", "gpu_device", "flash_attn", "dtw_token_timestamps");
    }
}

// Full transcription parameters
public class WhisperFullParams extends Structure {
    public int strategy;           // Sampling strategy (greedy, beam search)
    public int n_threads;          // Number of threads
    public int n_max_text_ctx;     // Max text context
    public int offset_ms;          // Start offset in milliseconds
    public int duration_ms;        // Duration to process
    public boolean translate;       // Translate to English
    public boolean no_timestamps;   // Disable timestamps
    public String language;         // Source language code

    // Callbacks
    public WhisperProgressCallback progress_callback;
    public WhisperNewSegmentCallback new_segment_callback;
}

// Beam search parameters
public class BeamSearchParams extends Structure {
    public int beam_size;
    public float patience;
}

// Greedy search parameters
public class GreedyParams extends Structure {
    public int best_of;
}
```

| Parameter Class | Purpose |
|-----------------|---------|
| `WhisperContextParams` | GPU, flash attention settings |
| `WhisperFullParams` | Complete transcription configuration |
| `BeamSearchParams` | Beam search decoding settings |
| `GreedyParams` | Greedy decoding settings |

#### 5. Model/Bean Classes (Data Structures)

```java
// Transcription segment with timing
public class WhisperSegment {
    private long startTime;
    private long endTime;
    private String text;

    public WhisperSegment(long start, long end, String text) {
        this.startTime = start;
        this.endTime = end;
        this.text = text;
    }

    // Getters and setters
}

// Token data from model
public class WhisperTokenData extends Structure {
    public int id;
    public int tid;
    public float p;
    public float plog;
    public float pt;
    public float ptsum;
    public long t0;
    public long t1;
    public float vlen;
}

// CPU configuration
public class CpuInfo {
    public int numCores;
    public boolean hasNeon;
    public boolean hasAvx;
    public boolean hasAvx2;
}
```

| Bean Class | Purpose |
|------------|---------|
| `WhisperSegment` | Text segment with timestamps |
| `WhisperTokenData` | Token probability data |
| `CpuInfo` | CPU capability detection |
| `WhisperModel` | Model metadata |

#### 6. Enum Types

```java
// Whisper model sizes
public enum EModel {
    TINY("tiny", "ggml-tiny.bin"),
    BASE("base", "ggml-base.bin"),
    SMALL("small", "ggml-small.bin"),
    MEDIUM("medium", "ggml-medium.bin"),
    LARGE("large", "ggml-large.bin");

    private final String name;
    private final String filename;

    EModel(String name, String filename) {
        this.name = name;
        this.filename = filename;
    }
}

// GGML tensor data types
public enum GgmlType {
    F32(0),
    F16(1),
    Q4_0(2),
    Q4_1(3),
    Q5_0(6),
    Q5_1(7),
    Q8_0(8);

    private final int value;
}

// Sampling strategy
public enum WhisperSamplingStrategy {
    GREEDY(0),
    BEAM_SEARCH(1);

    private final int value;
}
```

| Enum | Values | Purpose |
|------|--------|---------|
| `EModel` | TINY, BASE, SMALL, MEDIUM, LARGE | Model size selection |
| `GgmlType` | F32, F16, Q4_0, Q5_0, Q8_0, etc. | Tensor quantization types |
| `WhisperSamplingStrategy` | GREEDY, BEAM_SEARCH | Decoding strategy |

---

### Android-Specific Classes

```java
// Main Android activity
public class MainActivity extends AppCompatActivity {
    private WhisperService whisperService;
    private AudioRecorder recorder;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        whisperService = new WhisperService(this);
    }
}

// Android Whisper service
public class WhisperService {
    private Context context;
    private LocalWhisper whisper;

    public WhisperService(Context context) {
        this.context = context;
        this.whisper = LocalWhisper.getInstance();
    }

    public void transcribe(byte[] audioData, TranscriptionCallback callback) {
        // Background transcription
    }
}

// Singleton for local Whisper instance
public class LocalWhisper {
    private static LocalWhisper instance;
    private WhisperCpp whisperCpp;

    public static synchronized LocalWhisper getInstance() {
        if (instance == null) {
            instance = new LocalWhisper();
        }
        return instance;
    }
}

// Async task for model loading
public class LoadModelTask extends AsyncTask<String, Integer, Boolean> {
    @Override
    protected Boolean doInBackground(String... params) {
        // Load model in background
    }
}

// Async task for transcription
public class TranscriptionTask extends AsyncTask<byte[], Integer, String> {
    @Override
    protected String doInBackground(byte[]... params) {
        // Transcribe in background
    }
}

// Utility classes
public class AssetUtils {
    public static void copyAssetToFile(Context context, String assetName, File destFile) {
        // Copy model from assets to storage
    }
}

public class WaveEncoder {
    public static byte[] encodeToWav(short[] pcmData, int sampleRate) {
        // Encode PCM to WAV format
    }
}
```

| Android Class | Purpose |
|---------------|---------|
| `MainActivity` | Main UI activity |
| `WhisperService` | Android transcription service |
| `LocalWhisper` | Singleton Whisper instance |
| `LoadModelTask` | Async model loading |
| `TranscriptionTask` | Async transcription |
| `AssetUtils` | Asset file management |
| `WaveEncoder` | Audio format conversion |

---

### Class Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   C# / ASP.NET CORE                              │
│  ┌─────────────┐     ┌─────────────────┐     ┌──────────────┐   │
│  │  Program.cs │────▶│ WhisperService  │────▶│ whisper-cli  │   │
│  │  (Endpoints)│     │   (C# Class)    │     │  (Native)    │   │
│  └─────────────┘     └─────────────────┘     └──────────────┘   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                   JAVA / JNA BINDINGS                            │
│                                                                  │
│  ┌───────────────┐     ┌────────────────────────┐               │
│  │  WhisperCpp   │────▶│ WhisperCppJnaLibrary   │               │
│  │ (Java Class)  │     │    (JNA Interface)     │               │
│  └───────────────┘     └───────────┬────────────┘               │
│                                    │ native                      │
│                                    ▼                             │
│                         ┌────────────────────┐                   │
│                         │   libwhisper.so    │                   │
│                         │   (Native C++)     │                   │
│                         └────────────────────┘                   │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    CALLBACKS                             │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │    │
│  │  │ Progress     │  │ NewSegment   │  │ Encoder      │   │    │
│  │  │ Callback     │  │ Callback     │  │ Callback     │   │    │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    PARAMETERS                            │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │    │
│  │  │ContextParams │  │  FullParams  │  │ BeamSearch   │   │    │
│  │  │              │  │              │  │ Params       │   │    │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    DATA CLASSES                          │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │    │
│  │  │WhisperSegment│  │ TokenData    │  │   CpuInfo    │   │    │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                       ANDROID                                    │
│  ┌─────────────┐     ┌─────────────────┐     ┌──────────────┐   │
│  │MainActivity │────▶│ WhisperService  │────▶│ LocalWhisper │   │
│  │ (Activity)  │     │   (Service)     │     │ (Singleton)  │   │
│  └─────────────┘     └─────────────────┘     └──────┬───────┘   │
│                                                      │           │
│                                                      ▼           │
│                                              ┌──────────────┐    │
│                                              │  WhisperCpp  │    │
│                                              │ (JNA Wrapper)│    │
│                                              └──────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Architecture

```
voice-to-text-dotnet/
├── Program.cs                 # Application entry point & API endpoints
├── Pages/
│   ├── Index.cshtml           # Main UI page
│   └── Index.cshtml.cs        # Page model
├── Services/
│   └── WhisperService.cs      # Whisper transcription service
├── wwwroot/
│   ├── css/site.css           # Styles
│   └── js/voice-to-text.js    # Frontend JavaScript logic
└── whisper.cpp/               # Whisper.cpp submodule (for server mode)
```

## How It Works

### Mode 1: Browser Speech API

```
┌──────────────┐    ┌─────────────────┐    ┌──────────────┐
│  Microphone  │───>│ Web Speech API  │───>│   Browser    │
│              │    │ (Google Cloud)  │    │   Display    │
└──────────────┘    └─────────────────┘    └──────────────┘
```

1. User clicks "Start Listening"
2. Browser requests microphone permission
3. `SpeechRecognition` API captures audio and sends to Google's speech service
4. Real-time transcription results are displayed
5. Technical terms are auto-corrected before display

**Limitations**: Some languages (Tamil, Hindi) have limited or no support in Browser API.

### Mode 2: Whisper (Server)

Uses [whisper.cpp](https://github.com/ggml-org/whisper.cpp) for server-side transcription.

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Microphone  │───>│  MediaRecorder  │───>│  ASP.NET API  │───>│  whisper.cpp │
│              │    │  (WebM/Opus)    │    │  /api/transcribe │    │              │
└──────────────┘    └──────────────┘    └──────────────┘    └──────────────┘
                                                │
                                                v
                                        ┌──────────────┐
                                        │    FFmpeg    │
                                        │  (Convert to │
                                        │   16kHz WAV) │
                                        └──────────────┘
```

1. User clicks "Start Listening" in Whisper mode
2. `MediaRecorder` captures audio as WebM/Opus format
3. On stop, audio blob is sent to `/api/transcribe` endpoint
4. Server converts audio to 16kHz WAV using FFmpeg
5. whisper.cpp processes the audio with the specified language model
6. Transcribed text is returned and displayed

## Code Walkthrough

### Frontend (`wwwroot/js/voice-to-text.js`)

- **Technical Terms Dictionary** (lines 8-207): Maps common misrecognitions to correct technical terms
- **`correctTechnicalTerms()`**: Applies regex-based corrections to transcribed text
- **`setupBrowserSpeechRecognition()`**: Initializes the Web Speech API with event handlers
- **`startWhisperRecording()`**: Uses MediaRecorder to capture audio for server processing
- **`sendAudioToServer()`**: POSTs audio to `/api/transcribe` endpoint

### Backend (`Program.cs`)

- **`/api/transcribe`** (POST): Receives audio file, converts with FFmpeg, transcribes with Whisper
- **`/api/whisper/status`** (GET): Checks if Whisper is available on the server

### Whisper Service (`Services/WhisperService.cs`)

- **`IsAvailable()`**: Checks if whisper.cpp executable exists
- **`TranscribeAsync()`**:
  1. Saves uploaded audio to temp file
  2. Converts to 16kHz mono WAV using FFmpeg
  3. Runs whisper.cpp with language parameter
  4. Returns transcribed text

## Setup

### Prerequisites

- .NET 9.0 SDK
- Git

### Quick Start (Browser API Only)

```bash
# Clone the repository
git clone https://github.com/SureshSavage/voice2text.git
cd voice2text

# Run the application
dotnet run

# Open browser to https://localhost:5001
```

This will work immediately with Browser API mode for supported languages (English, Spanish, French, etc.).

---

## Installing Whisper.cpp (For Tamil, Hindi & Offline Transcription)

> **whisper.cpp** is a high-performance C/C++ implementation of OpenAI's Whisper automatic speech recognition model.
> GitHub: [https://github.com/ggml-org/whisper.cpp](https://github.com/ggml-org/whisper.cpp)

Whisper mode is required for:
- Tamil language support
- Hindi language support (better accuracy than Browser API)
- Offline transcription (no internet required)
- Privacy-sensitive applications (audio stays on your machine)

### Step 1: Install Dependencies

#### macOS (using Homebrew)

```bash
# Install FFmpeg (required for audio conversion)
brew install ffmpeg

# Install CMake (required for building whisper.cpp)
brew install cmake
```

#### Ubuntu/Debian

```bash
# Install FFmpeg and build tools
sudo apt update
sudo apt install ffmpeg cmake build-essential
```

#### Windows

1. Install FFmpeg: Download from https://ffmpeg.org/download.html and add to PATH
2. Install CMake: Download from https://cmake.org/download/
3. Install Visual Studio Build Tools or Visual Studio with C++ workload

### Step 2: Build whisper.cpp

The whisper.cpp source is included as a submodule in this repository.

```bash
# Navigate to the project directory
cd voice2text

# Build whisper.cpp using CMake
cd whisper.cpp
cmake -B build
cmake --build build --config Release

# Verify the build succeeded
ls build/bin/whisper-cli
```

On successful build, you should see `whisper-cli` (or `whisper-cli.exe` on Windows) in the `build/bin/` directory.

### Step 3: Download a Whisper Model

Whisper models come in different sizes. Larger models are more accurate but slower.

| Model  | Size   | RAM Required | Speed    | Accuracy |
|--------|--------|--------------|----------|----------|
| tiny   | 75 MB  | ~1 GB        | Fastest  | Basic    |
| base   | 142 MB | ~1 GB        | Fast     | Good     |
| small  | 466 MB | ~2 GB        | Medium   | Better   |
| medium | 1.5 GB | ~5 GB        | Slow     | Great    |
| large  | 3.1 GB | ~10 GB       | Slowest  | Best     |

**Recommended**: Start with `base` model for a good balance of speed and accuracy.

```bash
# Download the base model (142 MB)
cd whisper.cpp
bash models/download-ggml-model.sh base

# The model will be saved to: whisper.cpp/models/ggml-base.bin
```

To download other models:
```bash
# For tiny model (fastest, least accurate)
bash models/download-ggml-model.sh tiny

# For small model (better accuracy)
bash models/download-ggml-model.sh small

# For medium model (high accuracy, slower)
bash models/download-ggml-model.sh medium
```

### Step 4: Configure the Application

The application is pre-configured to use the default paths. Verify `appsettings.json`:

```json
{
  "Whisper": {
    "ExecutablePath": "./whisper.cpp/build/bin/whisper-cli",
    "ModelPath": "./whisper.cpp/models/ggml-base.bin"
  }
}
```

If you downloaded a different model, update the `ModelPath` accordingly.

### Step 5: Test Whisper Installation

```bash
# Test whisper.cpp directly with a sample audio file
cd whisper.cpp
./build/bin/whisper-cli -m models/ggml-base.bin -f samples/jfk.wav -l en

# Expected output:
# [00:00:00.000 --> 00:00:10.500] And so my fellow Americans ask not what your country can do for you...
```

### Step 6: Run the Application

```bash
# Go back to project root
cd ..

# Run the application
dotnet run
```

Now select "Whisper (Server)" mode in the UI to use server-side transcription.

---

## Troubleshooting Whisper Installation

### "Whisper is not available on this server"

1. Check if whisper-cli exists:
   ```bash
   ls ./whisper.cpp/build/bin/whisper-cli
   ```

2. Check if the model file exists:
   ```bash
   ls ./whisper.cpp/models/ggml-base.bin
   ```

3. Verify paths in `appsettings.json` match your installation.

### "Failed to convert audio format"

FFmpeg is not installed or not in PATH.

```bash
# Check if ffmpeg is installed
ffmpeg -version

# Install if missing (macOS)
brew install ffmpeg
```

### Build Errors

```bash
# Clean and rebuild
cd whisper.cpp
rm -rf build
cmake -B build
cmake --build build --config Release
```

### Model Download Fails

Download manually from Hugging Face:
- Base model: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin

Save to `whisper.cpp/models/ggml-base.bin`

---

## Supported Languages

| Language | Browser API | Whisper | Language Code |
|----------|-------------|---------|---------------|
| English (US) | Yes | Yes | en |
| English (UK) | Yes | Yes | en |
| Spanish | Yes | Yes | es |
| French | Yes | Yes | fr |
| German | Yes | Yes | de |
| Italian | Yes | Yes | it |
| Portuguese | Yes | Yes | pt |
| Chinese | Yes | Yes | zh |
| Japanese | Yes | Yes | ja |
| Korean | Yes | Yes | ko |
| Hindi | Limited | Yes | hi |
| Tamil | No | Yes | ta |

Whisper supports 99 languages total. See [whisper.cpp language list](https://github.com/ggml-org/whisper.cpp) for all supported languages.

---

## API Reference

### POST /api/transcribe

Transcribe audio using Whisper.

**Request:**
- Content-Type: `multipart/form-data`
- Body:
  - `audio`: Audio file (WebM format)
  - `language`: Language code (e.g., "en", "ta", "hi")

**Response:**
```json
{
  "text": "Transcribed text here"
}
```

### GET /api/whisper/status

Check Whisper availability.

**Response:**
```json
{
  "available": true
}
```

---

## Tech Stack

- **Backend**: ASP.NET Core 9.0, Razor Pages
- **Frontend**: Vanilla JavaScript, Web Speech API, MediaRecorder API
- **Transcription**: [whisper.cpp](https://github.com/ggml-org/whisper.cpp) (OpenAI Whisper)
- **Audio Processing**: FFmpeg

## License

MIT
