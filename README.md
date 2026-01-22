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
- For Whisper mode:
  - FFmpeg installed (`brew install ffmpeg` on macOS)
  - whisper.cpp compiled with a model file

### Running the Application

```bash
# Clone the repository
git clone https://github.com/SureshSavage/voice2text.git
cd voice2text

# Run the application
dotnet run

# Open browser to https://localhost:5001
```

### Configuring Whisper (Optional)

Add to `appsettings.json`:

```json
{
  "Whisper": {
    "ExecutablePath": "/path/to/whisper.cpp/main",
    "ModelPath": "/path/to/models/ggml-base.bin"
  }
}
```

## Supported Languages

| Language | Browser API | Whisper |
|----------|-------------|---------|
| English (US/UK) | Yes | Yes |
| Spanish | Yes | Yes |
| French | Yes | Yes |
| German | Yes | Yes |
| Italian | Yes | Yes |
| Portuguese | Yes | Yes |
| Chinese | Yes | Yes |
| Japanese | Yes | Yes |
| Korean | Yes | Yes |
| Hindi | Limited | Yes |
| Tamil | No | Yes |

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

## Tech Stack

- **Backend**: ASP.NET Core 9.0, Razor Pages
- **Frontend**: Vanilla JavaScript, Web Speech API, MediaRecorder API
- **Transcription**: whisper.cpp (OpenAI Whisper)
- **Audio Processing**: FFmpeg

## License

MIT
