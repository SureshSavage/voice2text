// Voice to Text - JavaScript Module

(function() {
    'use strict';

    // DOM Elements
    const startBtn = document.getElementById('startBtn');
    const stopBtn = document.getElementById('stopBtn');
    const clearBtn = document.getElementById('clearBtn');
    const copyBtn = document.getElementById('copyBtn');
    const output = document.getElementById('output');
    const status = document.getElementById('status');
    const languageSelect = document.getElementById('language');
    const continuousCheckbox = document.getElementById('continuous');
    const wordCountEl = document.getElementById('wordCount');
    const charCountEl = document.getElementById('charCount');
    const modeBtns = document.querySelectorAll('.mode-btn');
    const whisperInfo = document.getElementById('whisperInfo');

    // State
    let recognition = null;
    let finalTranscript = '';
    let currentMode = 'browser';
    let mediaRecorder = null;
    let audioChunks = [];
    let isRecording = false;

    // Initialize
    init();

    function init() {
        setupModeSelector();
        setupBrowserSpeechRecognition();
        setupEventListeners();
    }

    function setupModeSelector() {
        modeBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                modeBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                currentMode = btn.dataset.mode;

                if (currentMode === 'whisper') {
                    whisperInfo.style.display = 'block';
                } else {
                    whisperInfo.style.display = 'none';
                }

                // Stop any ongoing recognition when switching modes
                stopRecognition();
            });
        });
    }

    function setupBrowserSpeechRecognition() {
        if (!('webkitSpeechRecognition' in window) && !('SpeechRecognition' in window)) {
            setStatus('Speech recognition not supported. Try Chrome or Edge.', 'error');
            startBtn.disabled = true;
            return;
        }

        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        recognition = new SpeechRecognition();

        recognition.continuous = true;
        recognition.interimResults = true;
        recognition.lang = languageSelect.value;

        recognition.onstart = () => {
            setStatus('<span class="recording-indicator"></span>Listening...', 'listening');
            startBtn.disabled = true;
            stopBtn.disabled = false;
        };

        recognition.onend = () => {
            if (!stopBtn.dataset.manuallyStopped && continuousCheckbox.checked && currentMode === 'browser') {
                // Restart recognition for continuous mode
                setTimeout(() => {
                    if (startBtn.disabled) return;
                    try {
                        recognition.start();
                    } catch (e) {
                        // Already started or other error
                    }
                }, 100);
            } else {
                setStatus('Stopped', 'stopped');
                startBtn.disabled = false;
                stopBtn.disabled = true;
            }
            stopBtn.dataset.manuallyStopped = '';
        };

        recognition.onresult = (event) => {
            let interimTranscript = '';

            for (let i = event.resultIndex; i < event.results.length; i++) {
                const transcript = event.results[i][0].transcript;
                if (event.results[i].isFinal) {
                    finalTranscript += transcript + ' ';
                } else {
                    interimTranscript += transcript;
                }
            }

            output.value = finalTranscript + interimTranscript;
            output.scrollTop = output.scrollHeight;
            updateCounts();
        };

        recognition.onerror = (event) => {
            console.error('Speech recognition error:', event.error);

            let errorMessage = 'Error: ';
            switch (event.error) {
                case 'no-speech':
                    errorMessage += 'No speech detected. Try again.';
                    break;
                case 'audio-capture':
                    errorMessage += 'No microphone found.';
                    break;
                case 'not-allowed':
                    errorMessage += 'Microphone permission denied.';
                    break;
                case 'network':
                    errorMessage += 'Network error occurred.';
                    break;
                default:
                    errorMessage += event.error;
            }

            setStatus(errorMessage, 'error');
            startBtn.disabled = false;
            stopBtn.disabled = true;
        };
    }

    function setupEventListeners() {
        startBtn.addEventListener('click', startRecognition);
        stopBtn.addEventListener('click', () => {
            stopBtn.dataset.manuallyStopped = 'true';
            stopRecognition();
        });
        clearBtn.addEventListener('click', clearOutput);
        copyBtn.addEventListener('click', copyToClipboard);

        languageSelect.addEventListener('change', () => {
            if (recognition) {
                recognition.lang = languageSelect.value;
            }
        });

        output.addEventListener('input', updateCounts);
    }

    function startRecognition() {
        finalTranscript = output.value;

        if (currentMode === 'browser') {
            startBrowserRecognition();
        } else {
            startWhisperRecording();
        }
    }

    function startBrowserRecognition() {
        if (!recognition) {
            setStatus('Speech recognition not available', 'error');
            return;
        }

        recognition.lang = languageSelect.value;
        recognition.continuous = continuousCheckbox.checked;

        try {
            recognition.start();
        } catch (e) {
            console.error('Failed to start recognition:', e);
            setStatus('Failed to start. Please try again.', 'error');
        }
    }

    async function startWhisperRecording() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

            mediaRecorder = new MediaRecorder(stream, {
                mimeType: 'audio/webm;codecs=opus'
            });

            audioChunks = [];
            isRecording = true;

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunks.push(event.data);
                }
            };

            mediaRecorder.onstop = async () => {
                isRecording = false;
                stream.getTracks().forEach(track => track.stop());

                if (audioChunks.length > 0) {
                    await sendAudioToServer();
                }
            };

            mediaRecorder.start(1000); // Collect data every second

            setStatus('<span class="recording-indicator"></span>Recording for Whisper...', 'listening');
            startBtn.disabled = true;
            stopBtn.disabled = false;

        } catch (err) {
            console.error('Microphone access error:', err);
            setStatus('Microphone access denied', 'error');
        }
    }

    async function sendAudioToServer() {
        setStatus('Processing with Whisper...', 'processing');

        const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
        const formData = new FormData();
        formData.append('audio', audioBlob, 'recording.webm');
        formData.append('language', languageSelect.value.split('-')[0]);

        try {
            const response = await fetch('/api/transcribe', {
                method: 'POST',
                body: formData
            });

            if (response.ok) {
                const result = await response.json();
                if (result.text) {
                    finalTranscript += result.text + ' ';
                    output.value = finalTranscript;
                    output.scrollTop = output.scrollHeight;
                    updateCounts();
                }
                setStatus('Transcription complete', 'stopped');
            } else {
                const error = await response.text();
                setStatus('Server error: ' + error, 'error');
            }
        } catch (err) {
            console.error('Server error:', err);
            setStatus('Failed to connect to server', 'error');
        }

        startBtn.disabled = false;
        stopBtn.disabled = true;
    }

    function stopRecognition() {
        if (currentMode === 'browser' && recognition) {
            recognition.stop();
        } else if (currentMode === 'whisper' && mediaRecorder && isRecording) {
            mediaRecorder.stop();
        }
    }

    function clearOutput() {
        output.value = '';
        finalTranscript = '';
        updateCounts();
    }

    async function copyToClipboard() {
        const text = output.value;
        if (!text) return;

        try {
            await navigator.clipboard.writeText(text);
            const originalText = copyBtn.innerHTML;
            copyBtn.innerHTML = 'Copied!';
            setTimeout(() => {
                copyBtn.innerHTML = originalText;
            }, 2000);
        } catch (err) {
            console.error('Failed to copy:', err);
            // Fallback for older browsers
            output.select();
            document.execCommand('copy');
        }
    }

    function setStatus(message, className) {
        status.innerHTML = message;
        status.className = 'status ' + className;
    }

    function updateCounts() {
        const text = output.value.trim();
        const words = text ? text.split(/\s+/).length : 0;
        const chars = text.length;
        wordCountEl.textContent = words;
        charCountEl.textContent = chars;
    }
})();
