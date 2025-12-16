// Audio interop functions for Blazor - Hands-Free Voice Interaction
// Handles microphone capture (push-to-talk) and audio playback (TTS output)

window.aesirAudio = {
    // Internal state
    _state: {
        audioContext: null,
        mediaStream: null,
        mediaRecorder: null,
        captureDotNetRef: null,   // Reference for capture callbacks (OnAudioChunk, OnCaptureError)
        playbackDotNetRef: null,  // Reference for playback callbacks (OnPlaybackComplete)
        isCapturing: false,
        isPlaying: false,
        playbackQueue: [],
        currentSource: null,
        nextPlaybackTime: 0,
        sampleRate: 16000, // Default for STT
        playbackSampleRate: 22050, // Default for TTS
        jitterBufferSize: 3, // Number of chunks to buffer before playing
        initialized: false
    },

    /**
     * Initialize the audio system with a .NET reference for callbacks
     * @param {object} dotNetRef - .NET object reference for callbacks
     * @param {number} captureSampleRate - Sample rate for capture (default 16000)
     * @param {number} playbackSampleRate - Sample rate for playback (default 22050)
     * @param {string} serviceType - 'capture' or 'playback' to identify which service is initializing
     * @returns {Promise<boolean>} True if initialization successful
     */
    initialize: async function (dotNetRef, captureSampleRate, playbackSampleRate, serviceType) {
        try {
            // Store the reference based on service type
            if (serviceType === 'playback') {
                this._state.playbackDotNetRef = dotNetRef;
            } else {
                // Default to capture for backwards compatibility
                this._state.captureDotNetRef = dotNetRef;
            }

            // Only initialize AudioContext once
            if (this._state.initialized) {
                console.log(`Audio system already initialized, registered ${serviceType || 'capture'} callbacks`);
                return true;
            }

            this._state.sampleRate = captureSampleRate || 16000;
            this._state.playbackSampleRate = playbackSampleRate || 22050;

            // Create AudioContext (may need user interaction to resume)
            this._state.audioContext = new (window.AudioContext || window.webkitAudioContext)({
                sampleRate: this._state.playbackSampleRate
            });

            // If context is suspended, it will be resumed on first user interaction
            if (this._state.audioContext.state === 'suspended') {
                console.log('AudioContext suspended, will resume on user interaction');
            }

            this._state.initialized = true;
            console.log('Audio system initialized successfully');
            return true;
        } catch (error) {
            console.error('Failed to initialize audio system:', error);
            return false;
        }
    },

    /**
     * Dispose of all audio resources
     */
    dispose: function () {
        this.capture.release();  // Full cleanup, not just stop
        this.playback.stop();

        if (this._state.captureContext) {
            this._state.captureContext.close();
            this._state.captureContext = null;
        }

        if (this._state.audioContext) {
            this._state.audioContext.close();
            this._state.audioContext = null;
        }

        this._state.captureDotNetRef = null;
        this._state.playbackDotNetRef = null;
        this._state.initialized = false;
        console.log('Audio system disposed');
    },

    // Microphone capture functions (push-to-talk)
    capture: {
        /**
         * Pre-initialize the audio capture pipeline to eliminate first-recording latency.
         * Call this when entering hands-free mode to warm up the microphone.
         * @returns {Promise<boolean>} True if warm-up successful
         */
        warmUp: async function () {
            const state = window.aesirAudio._state;

            if (state.captureWarmedUp) {
                console.log('Audio capture already warmed up');
                return true;
            }

            try {
                console.log('Warming up audio capture...');

                // Create a dedicated AudioContext for capture at 16kHz (STT sample rate)
                if (!state.captureContext || state.captureContext.state === 'closed') {
                    state.captureContext = new (window.AudioContext || window.webkitAudioContext)({
                        sampleRate: state.sampleRate // 16kHz for STT
                    });
                }

                // Resume AudioContext if suspended (requires user interaction)
                if (state.captureContext.state === 'suspended') {
                    await state.captureContext.resume();
                }

                // Request microphone access - this is the slow part that causes first-word loss
                state.mediaStream = await navigator.mediaDevices.getUserMedia({
                    audio: {
                        channelCount: 1,
                        sampleRate: state.sampleRate,
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    },
                    video: false
                });

                // Create media stream source
                state.captureSource = state.captureContext.createMediaStreamSource(state.mediaStream);

                // Use ScriptProcessorNode for PCM capture (works in all browsers)
                // Buffer size of 2048 samples at 16kHz = 128ms chunks (reduced for lower latency)
                const bufferSize = 2048;
                state.scriptProcessor = state.captureContext.createScriptProcessor(bufferSize, 1, 1);

                state.scriptProcessor.onaudioprocess = async (event) => {
                    if (!state.isCapturing || !state.captureDotNetRef) return;

                    const inputData = event.inputBuffer.getChannelData(0);

                    // Convert Float32 samples to Int16 PCM bytes
                    const pcmBuffer = new ArrayBuffer(inputData.length * 2);
                    const pcmView = new DataView(pcmBuffer);

                    for (let i = 0; i < inputData.length; i++) {
                        // Clamp to [-1, 1] and convert to 16-bit signed integer
                        const sample = Math.max(-1, Math.min(1, inputData[i]));
                        const int16 = sample < 0 ? sample * 0x8000 : sample * 0x7FFF;
                        pcmView.setInt16(i * 2, int16, true); // Little-endian
                    }

                    try {
                        const base64 = this._arrayBufferToBase64(pcmBuffer);
                        await state.captureDotNetRef.invokeMethodAsync('OnAudioChunk', base64);
                    } catch (error) {
                        console.error('Failed to send PCM chunk:', error);
                    }
                };

                // Connect: source -> scriptProcessor -> destination (required for processing)
                state.captureSource.connect(state.scriptProcessor);
                state.scriptProcessor.connect(state.captureContext.destination);

                state.captureWarmedUp = true;
                console.log('Audio capture warmed up successfully (raw PCM at', state.sampleRate, 'Hz)');
                return true;
            } catch (error) {
                console.error('Failed to warm up audio capture:', error);
                return false;
            }
        },

        /**
         * Start capturing audio from the microphone
         * Sends raw 16-bit PCM chunks to .NET via OnAudioChunk callback
         * Uses Web Audio API for direct PCM access (no container format)
         * @returns {Promise<boolean>} True if capture started successfully
         */
        start: async function () {
            const state = window.aesirAudio._state;

            if (state.isCapturing) {
                console.warn('Already capturing audio');
                return true;
            }

            try {
                // Warm up if not already done
                if (!state.captureWarmedUp) {
                    const warmedUp = await this.warmUp();
                    if (!warmedUp) {
                        throw new Error('Failed to warm up audio capture');
                    }
                }

                // Resume AudioContext if suspended
                if (state.captureContext.state === 'suspended') {
                    await state.captureContext.resume();
                }

                state.isCapturing = true;
                console.log('Audio capture started (raw PCM at', state.sampleRate, 'Hz)');
                return true;
            } catch (error) {
                console.error('Failed to start audio capture:', error);
                if (state.captureDotNetRef) {
                    state.captureDotNetRef.invokeMethodAsync('OnCaptureError', error.message || 'Failed to access microphone');
                }
                return false;
            }
        },

        /**
         * Stop capturing audio (keeps pipeline alive if warmed up for quick restart)
         */
        stop: function () {
            const state = window.aesirAudio._state;

            state.isCapturing = false;
            console.log('Audio capture stopped (pipeline preserved for next recording)');
        },

        /**
         * Fully release microphone and audio resources
         */
        release: function () {
            const state = window.aesirAudio._state;

            state.isCapturing = false;
            state.captureWarmedUp = false;

            if (state.scriptProcessor) {
                state.scriptProcessor.disconnect();
                state.scriptProcessor = null;
            }

            if (state.captureSource) {
                state.captureSource.disconnect();
                state.captureSource = null;
            }

            if (state.mediaStream) {
                state.mediaStream.getTracks().forEach(track => track.stop());
                state.mediaStream = null;
            }

            console.log('Audio capture released');
        },

        /**
         * Check if currently capturing
         * @returns {boolean} True if capturing
         */
        isActive: function () {
            return window.aesirAudio._state.isCapturing;
        },

        /**
         * Convert ArrayBuffer to base64 string
         * @param {ArrayBuffer} buffer - The buffer to convert
         * @returns {string} Base64 encoded string
         */
        _arrayBufferToBase64: function (buffer) {
            const bytes = new Uint8Array(buffer);
            let binary = '';
            for (let i = 0; i < bytes.byteLength; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            return window.btoa(binary);
        }
    },

    // Audio playback functions (TTS output)
    playback: {
        /**
         * Queue an audio chunk for playback
         * @param {string} base64Data - Base64 encoded audio data
         * @param {string} format - Audio format ('wav', 'opus', 'pcm')
         * @returns {Promise<boolean>} True if chunk queued successfully
         */
        queueChunk: async function (base64Data, format) {
            const state = window.aesirAudio._state;

            if (!state.audioContext) {
                console.error('AudioContext not initialized');
                return false;
            }

            try {
                // Decode base64 to ArrayBuffer
                const arrayBuffer = this._base64ToArrayBuffer(base64Data);

                // Decode audio data based on format
                let audioBuffer;
                if (format === 'pcm' || format === 'raw') {
                    // PCM data - create buffer manually
                    audioBuffer = this._decodePcm(arrayBuffer, state.audioContext);
                } else {
                    // WAV or other encoded format - use Web Audio API decoder
                    audioBuffer = await state.audioContext.decodeAudioData(arrayBuffer.slice(0));
                }

                // Add to queue
                state.playbackQueue.push(audioBuffer);

                // Start playback if we have enough buffered chunks and not already playing
                if (!state.isPlaying && state.playbackQueue.length >= state.jitterBufferSize) {
                    this._startPlayback();
                }

                return true;
            } catch (error) {
                console.error('Failed to queue audio chunk:', error);
                return false;
            }
        },

        /**
         * Start playback from the queue
         */
        play: function () {
            const state = window.aesirAudio._state;

            if (state.playbackQueue.length > 0 && !state.isPlaying) {
                this._startPlayback();
            }
        },

        /**
         * Stop playback and clear the queue
         */
        stop: function () {
            const state = window.aesirAudio._state;

            if (state.currentSource) {
                try {
                    state.currentSource.stop();
                } catch (e) {
                    // Ignore - source may already be stopped
                }
                state.currentSource = null;
            }

            state.playbackQueue = [];
            state.isPlaying = false;
            state.nextPlaybackTime = 0;
            console.log('Audio playback stopped');
        },

        /**
         * Check if currently playing
         * @returns {boolean} True if playing
         */
        isActive: function () {
            return window.aesirAudio._state.isPlaying;
        },

        /**
         * Internal: Start playback from queue
         */
        _startPlayback: function () {
            const state = window.aesirAudio._state;

            if (state.playbackQueue.length === 0) {
                state.isPlaying = false;
                if (state.playbackDotNetRef) {
                    state.playbackDotNetRef.invokeMethodAsync('OnPlaybackComplete');
                }
                return;
            }

            // Resume context if suspended
            if (state.audioContext.state === 'suspended') {
                state.audioContext.resume();
            }

            state.isPlaying = true;

            // Initialize playback time if needed
            if (state.nextPlaybackTime === 0 || state.nextPlaybackTime < state.audioContext.currentTime) {
                state.nextPlaybackTime = state.audioContext.currentTime;
            }

            // Schedule all buffered chunks
            while (state.playbackQueue.length > 0) {
                const audioBuffer = state.playbackQueue.shift();
                this._scheduleBuffer(audioBuffer);
            }
        },

        /**
         * Internal: Schedule an audio buffer for playback
         * @param {AudioBuffer} audioBuffer - The buffer to play
         */
        _scheduleBuffer: function (audioBuffer) {
            const state = window.aesirAudio._state;

            const source = state.audioContext.createBufferSource();
            source.buffer = audioBuffer;
            source.connect(state.audioContext.destination);

            // Schedule at the next available time
            source.start(state.nextPlaybackTime);

            // Update next playback time
            state.nextPlaybackTime += audioBuffer.duration;

            // Track current source for stopping
            state.currentSource = source;

            // Handle playback end
            source.onended = () => {
                // Check if there's more to play
                if (state.playbackQueue.length > 0) {
                    this._startPlayback();
                } else if (state.audioContext.currentTime >= state.nextPlaybackTime - 0.1) {
                    // Playback complete
                    state.isPlaying = false;
                    state.nextPlaybackTime = 0;
                    if (state.playbackDotNetRef) {
                        state.playbackDotNetRef.invokeMethodAsync('OnPlaybackComplete');
                    }
                }
            };
        },

        /**
         * Convert base64 string to ArrayBuffer
         * @param {string} base64 - Base64 encoded string
         * @returns {ArrayBuffer} Decoded buffer
         */
        _base64ToArrayBuffer: function (base64) {
            const binaryString = window.atob(base64);
            const len = binaryString.length;
            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes.buffer;
        },

        /**
         * Decode raw PCM data (16-bit signed, mono) to AudioBuffer
         * @param {ArrayBuffer} pcmData - Raw PCM data
         * @param {AudioContext} audioContext - The audio context
         * @returns {AudioBuffer} Decoded audio buffer
         */
        _decodePcm: function (pcmData, audioContext) {
            const state = window.aesirAudio._state;
            const int16Array = new Int16Array(pcmData);
            const float32Array = new Float32Array(int16Array.length);

            // Convert 16-bit PCM to float32 (-1.0 to 1.0)
            for (let i = 0; i < int16Array.length; i++) {
                float32Array[i] = int16Array[i] / 32768.0;
            }

            // Create AudioBuffer
            const audioBuffer = audioContext.createBuffer(
                1, // mono
                float32Array.length,
                state.playbackSampleRate
            );

            audioBuffer.getChannelData(0).set(float32Array);
            return audioBuffer;
        }
    },

    // Feature detection utilities
    capabilities: {
        /**
         * Check if Web Audio API is supported
         * @returns {boolean} True if supported
         */
        isSupported: function () {
            return !!(window.AudioContext || window.webkitAudioContext);
        },

        /**
         * Check if MediaRecorder with Opus is supported
         * @returns {boolean} True if Opus capture is supported
         */
        hasOpusSupport: function () {
            if (!window.MediaRecorder) {
                return false;
            }

            const types = [
                'audio/webm;codecs=opus',
                'audio/ogg;codecs=opus'
            ];

            return types.some(type => MediaRecorder.isTypeSupported(type));
        },

        /**
         * Check if running in Tauri desktop environment
         * @returns {boolean} True if in Tauri
         */
        isTauri: function () {
            return !!(window.__TAURI__ || window.__TAURI_INTERNALS__);
        },

        /**
         * Check if microphone permission is granted
         * @returns {Promise<string>} 'granted', 'denied', or 'prompt'
         */
        getMicrophonePermission: async function () {
            try {
                if (navigator.permissions) {
                    const result = await navigator.permissions.query({ name: 'microphone' });
                    return result.state;
                }
                // Fallback: try to get user media and see if it succeeds
                return 'prompt';
            } catch (error) {
                console.warn('Could not query microphone permission:', error);
                return 'prompt';
            }
        },

        /**
         * Get all capabilities as an object
         * @returns {Promise<object>} Capabilities object
         */
        getAll: async function () {
            const micPermission = await this.getMicrophonePermission();
            return {
                webAudioSupported: this.isSupported(),
                opusSupported: this.hasOpusSupport(),
                isTauri: this.isTauri(),
                microphonePermission: micPermission,
                // Browser info
                userAgent: navigator.userAgent,
                platform: navigator.platform
            };
        }
    }
};
