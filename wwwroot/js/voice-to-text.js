// Voice to Text - JavaScript Module

(function() {
    'use strict';

    // Technical terms dictionary for auto-correction
    // Maps common misrecognitions to correct technical terms
    const techTermsCorrections = {
        // JavaScript/TypeScript
        'use state': 'useState',
        'use effect': 'useEffect',
        'use memo': 'useMemo',
        'use ref': 'useRef',
        'use callback': 'useCallback',
        'use context': 'useContext',
        'use reducer': 'useReducer',
        'type script': 'TypeScript',
        'java script': 'JavaScript',
        'node js': 'Node.js',
        'next js': 'Next.js',
        'react js': 'React.js',
        'vue js': 'Vue.js',
        'nuxt js': 'Nuxt.js',
        'express js': 'Express.js',
        'nest js': 'NestJS',
        'j query': 'jQuery',
        'json': 'JSON',
        'ajax': 'AJAX',
        'a sync': 'async',
        'a wait': 'await',
        'const': 'const',
        'let': 'let',
        'var': 'var',

        // .NET / C#
        'c sharp': 'C#',
        'dot net': '.NET',
        'asp dot net': 'ASP.NET',
        'asp net': 'ASP.NET',
        'entity framework': 'Entity Framework',
        'ef core': 'EF Core',
        'link': 'LINQ',
        'new get': 'NuGet',
        'nu get': 'NuGet',
        'eye enumerable': 'IEnumerable',
        'i enumerable': 'IEnumerable',
        'i list': 'IList',
        'i collection': 'ICollection',
        'i queryable': 'IQueryable',
        'action result': 'ActionResult',
        'i action result': 'IActionResult',
        'db context': 'DbContext',
        'db set': 'DbSet',

        // Python
        'pie thon': 'Python',
        'python': 'Python',
        'pip': 'pip',
        'pi pi': 'PyPI',
        'num pie': 'NumPy',
        'num pi': 'NumPy',
        'pandas': 'pandas',
        'psycho pg': 'psycopg',
        'django': 'Django',
        'flask': 'Flask',
        'fast api': 'FastAPI',
        'fast a p i': 'FastAPI',

        // DevOps / Cloud
        'cubectl': 'kubectl',
        'cube c t l': 'kubectl',
        'kubectl': 'kubectl',
        'docker': 'Docker',
        'kubernetes': 'Kubernetes',
        'k 8 s': 'K8s',
        'k8s': 'K8s',
        'aws': 'AWS',
        'azure': 'Azure',
        'gcp': 'GCP',
        'terraform': 'Terraform',
        'ansible': 'Ansible',
        'jenkins': 'Jenkins',
        'ci cd': 'CI/CD',
        'c i c d': 'CI/CD',
        'get hub': 'GitHub',
        'git hub': 'GitHub',
        'get lab': 'GitLab',
        'git lab': 'GitLab',
        'bit bucket': 'Bitbucket',

        // Databases
        'my sequel': 'MySQL',
        'my sql': 'MySQL',
        'post gress': 'PostgreSQL',
        'postgres': 'PostgreSQL',
        'post gres q l': 'PostgreSQL',
        'mongo db': 'MongoDB',
        'redis': 'Redis',
        'elastic search': 'Elasticsearch',
        'dynamo db': 'DynamoDB',
        'cosmos db': 'CosmosDB',
        'fire base': 'Firebase',
        'supabase': 'Supabase',
        'sequel': 'SQL',
        's q l': 'SQL',
        'no sequel': 'NoSQL',

        // Package managers / CLI
        'npm': 'npm',
        'n p m': 'npm',
        'yarn': 'yarn',
        'p npm': 'pnpm',
        'pnpm': 'pnpm',
        'homebrew': 'Homebrew',
        'brew': 'brew',
        'apt get': 'apt-get',
        'apt': 'apt',
        'yum': 'yum',

        // APIs / Protocols
        'rest api': 'REST API',
        'rest a p i': 'REST API',
        'graph ql': 'GraphQL',
        'graph q l': 'GraphQL',
        'grpc': 'gRPC',
        'g r p c': 'gRPC',
        'http': 'HTTP',
        'https': 'HTTPS',
        'web socket': 'WebSocket',
        'web sockets': 'WebSockets',
        'o auth': 'OAuth',
        'oauth': 'OAuth',
        'jwt': 'JWT',
        'j w t': 'JWT',

        // Common programming terms
        'api': 'API',
        'a p i': 'API',
        'sdk': 'SDK',
        's d k': 'SDK',
        'cli': 'CLI',
        'c l i': 'CLI',
        'gui': 'GUI',
        'g u i': 'GUI',
        'ui': 'UI',
        'u i': 'UI',
        'ux': 'UX',
        'u x': 'UX',
        'html': 'HTML',
        'h t m l': 'HTML',
        'css': 'CSS',
        'c s s': 'CSS',
        'sass': 'Sass',
        'scss': 'SCSS',
        'regex': 'regex',
        'reg ex': 'regex',
        'localhost': 'localhost',
        'local host': 'localhost',
        'dev ops': 'DevOps',
        'dev tools': 'DevTools',
        'vs code': 'VS Code',
        'v s code': 'VS Code',
        'visual studio': 'Visual Studio',
        'intellij': 'IntelliJ',

        // File extensions / formats
        'dot json': '.json',
        'dot yaml': '.yaml',
        'dot yml': '.yml',
        'dot xml': '.xml',
        'dot cs': '.cs',
        'dot js': '.js',
        'dot ts': '.ts',
        'dot tsx': '.tsx',
        'dot jsx': '.jsx',
        'dot py': '.py',
        'dot env': '.env',
        'dot git ignore': '.gitignore',
        'git ignore': '.gitignore',

        // Common commands
        'cd': 'cd',
        'ls': 'ls',
        'mkdir': 'mkdir',
        'rm': 'rm',
        'sudo': 'sudo',
        'chmod': 'chmod',
        'chown': 'chown',
        'grep': 'grep',
        'cat': 'cat',
        'echo': 'echo',
        'curl': 'curl',
        'wget': 'wget',

        // AI/ML
        'open ai': 'OpenAI',
        'open a i': 'OpenAI',
        'chat gpt': 'ChatGPT',
        'gpt': 'GPT',
        'g p t': 'GPT',
        'llm': 'LLM',
        'l l m': 'LLM',
        'whisper': 'Whisper',
        'tensor flow': 'TensorFlow',
        'pie torch': 'PyTorch',
        'pi torch': 'PyTorch',
    };

    // Build a case-insensitive regex pattern
    let techTermsEnabled = true;

    function correctTechnicalTerms(text) {
        if (!techTermsEnabled) return text;

        let corrected = text;
        for (const [wrong, correct] of Object.entries(techTermsCorrections)) {
            // Case-insensitive replacement with word boundaries
            const regex = new RegExp(`\\b${wrong}\\b`, 'gi');
            corrected = corrected.replace(regex, correct);
        }
        return corrected;
    }

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
    const techTermsCheckbox = document.getElementById('techTerms');

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
                let transcript = event.results[i][0].transcript;
                if (event.results[i].isFinal) {
                    // Apply technical term corrections to final results
                    transcript = correctTechnicalTerms(transcript);
                    finalTranscript += transcript + ' ';
                } else {
                    interimTranscript += transcript;
                }
            }

            output.value = finalTranscript + correctTechnicalTerms(interimTranscript);
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

        // Toggle tech terms auto-correction
        if (techTermsCheckbox) {
            techTermsCheckbox.addEventListener('change', () => {
                techTermsEnabled = techTermsCheckbox.checked;
            });
        }

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
                    // Apply technical term corrections
                    const correctedText = correctTechnicalTerms(result.text);
                    finalTranscript += correctedText + ' ';
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
