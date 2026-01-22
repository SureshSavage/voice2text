# Voice to Text (.NET)

A web-based voice-to-text application built with ASP.NET Core that supports two transcription modes: Browser Speech API and Whisper (server-side).

## Features

- **Dual Transcription Modes**:
  - **Browser API**: Uses the Web Speech API (Chrome/Edge) for real-time transcription
  - **Whisper Mode**: Server-side transcription using OpenAI's Whisper model via whisper.cpp
- **Multi-language Support**: English, Spanish, French, German, Italian, Portuguese, Chinese, Japanese, Korean, Hindi, and Tamil
- **Technical Terms Auto-correction**: Automatically corrects common programming terms (e.g., "use state" -> "useState", "java script" -> "JavaScript")
- **Continuous Listening Mode**: Keep transcribing without manually restarting
- **Copy to Clipboard**: One-click copy of transcribed text
- **Word/Character Count**: Real-time statistics

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

Whisper supports 99 languages total. See [whisper.cpp language list](https://github.com/ggerganov/whisper.cpp/blob/master/src/whisper.cpp#L309) for all supported languages.

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
- **Transcription**: whisper.cpp (OpenAI Whisper)
- **Audio Processing**: FFmpeg

## License

MIT
