// Platform detection and Tauri interop module for Tauri 2.x
// Requires "withGlobalTauri": true in tauri.conf.json

window.aesirPlatform = {
    /**
     * Detect the current platform environment
     * @returns {Object} Platform information
     */
    detect: function () {
        // With withGlobalTauri: true, window.__TAURI__ is available
        const isTauri = typeof window.__TAURI__ !== 'undefined';
        const userAgent = navigator.userAgent.toLowerCase();

        // Detect mobile devices
        const isMobile = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent);

        // Detect operating system from user agent
        let os = 'unknown';
        if (userAgent.includes('win')) os = 'windows';
        else if (userAgent.includes('mac')) os = 'macos';
        else if (userAgent.includes('linux')) os = 'linux';
        else if (userAgent.includes('android')) os = 'android';
        else if (userAgent.includes('iphone') || userAgent.includes('ipad')) os = 'ios';

        return {
            isTauri: isTauri,
            isDesktop: !isMobile,
            isMobile: isMobile,
            operatingSystem: os
        };
    },

    /**
     * Set the Tauri window theme to match the application theme
     * @param {boolean} isDarkMode - True for dark theme, false for light theme
     * @returns {Promise<boolean>} True if successful, false if not in Tauri or failed
     */
    setTauriTheme: async function (isDarkMode) {
        if (typeof window.__TAURI__ === 'undefined') {
            return false;
        }

        try {
            // With withGlobalTauri: true, window API is at window.__TAURI__.window
            const currentWindow = window.__TAURI__.window.getCurrentWindow();
            await currentWindow.setTheme(isDarkMode ? 'dark' : 'light');
            return true;
        } catch (error) {
            console.warn('Failed to set Tauri window theme:', error);
            return false;
        }
    }
};

// Tauri native file operations (Tauri 2.x with withGlobalTauri: true)
window.aesirNativeFile = {
    /**
     * Check if running in Tauri environment
     * @returns {boolean} True if in Tauri
     */
    isTauri: function () {
        return typeof window.__TAURI__ !== 'undefined';
    },

    /**
     * Open a file or URL with the system's default application
     * Uses the shell plugin's open function
     * @param {string} path - Path or URL to open
     * @returns {Promise<boolean>} True if successful
     */
    openFileNative: async function (path) {
        if (!this.isTauri()) {
            console.warn('Not running in Tauri, falling back to browser');
            window.open(path, '_blank');
            return true;
        }

        try {
            // With withGlobalTauri: true, shell plugin is at window.__TAURI__.shell
            const { open } = window.__TAURI__.shell;
            await open(path);
            return true;
        } catch (error) {
            console.error('Failed to open file natively:', error);
            // Fall back to browser
            window.open(path, '_blank');
            return true;
        }
    },

    /**
     * Open a URL in the system's default browser
     * @param {string} url - URL to open
     * @returns {Promise<boolean>} True if successful
     */
    openUrlNative: async function (url) {
        return this.openFileNative(url);
    },

    /**
     * Save a file with a native save dialog
     * @param {string} filename - Suggested filename
     * @param {Uint8Array} content - File content as byte array
     * @returns {Promise<string|null>} Saved file path or null if cancelled/failed
     */
    saveToDownloads: async function (filename, content) {
        if (!this.isTauri()) {
            return this.browserDownload(filename, content);
        }

        try {
            // With withGlobalTauri: true, dialog plugin is at window.__TAURI__.dialog
            const { save } = window.__TAURI__.dialog;
            const filePath = await save({
                defaultPath: filename,
                filters: [{
                    name: 'All Files',
                    extensions: ['*']
                }]
            });

            if (filePath) {
                // With withGlobalTauri: true, fs plugin is at window.__TAURI__.fs
                const { writeFile } = window.__TAURI__.fs;
                await writeFile(filePath, content);
                return filePath;
            }
            return null;
        } catch (error) {
            console.error('Failed to save file:', error);
            return this.browserDownload(filename, content);
        }
    },

    /**
     * Browser fallback for file download
     * @param {string} filename - Filename for download
     * @param {Uint8Array} content - File content
     * @returns {string} The filename (browser doesn't return path)
     */
    browserDownload: function (filename, content) {
        const blob = new Blob([content]);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();

        setTimeout(() => {
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        }, 100);

        return filename;
    },

    /**
     * Download a file from URL and save it using native save dialog
     * @param {string} url - URL of the file
     * @param {string} filename - Suggested filename
     * @returns {Promise<string|null>} Saved file path or null if cancelled
     */
    saveFileWithDialog: async function (url, filename) {
        if (!this.isTauri()) {
            return this.browserDownload(filename, new Uint8Array());
        }

        try {
            // Step 1: Fetch the file content
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`Failed to fetch file: ${response.status} ${response.statusText}`);
            }
            const arrayBuffer = await response.arrayBuffer();
            const content = new Uint8Array(arrayBuffer);

            // Step 2: Show native save dialog
            const { save } = window.__TAURI__.dialog;
            const { writeFile } = window.__TAURI__.fs;

            const filePath = await save({
                defaultPath: filename,
                filters: [{
                    name: 'All Files',
                    extensions: ['*']
                }]
            });

            if (filePath) {
                // Step 3: Write file to chosen location
                await writeFile(filePath, content);
                return filePath;
            }
            return null;
        } catch (error) {
            console.error('Failed to save file with dialog:', error);
            // Fall back to browser download
            return this.browserDownload(filename, new Uint8Array());
        }
    },

    /**
     * Download a file from URL and open it natively
     * Downloads to temp directory first, then opens with system default app
     * @param {string} url - URL of the file
     * @param {string} filename - Filename to use
     * @returns {Promise<boolean>} True if successful
     */
    downloadAndOpenNative: async function (url, filename) {
        if (!this.isTauri()) {
            // In browser, just open in new tab
            window.open(url, '_blank');
            return true;
        }

        try {
            // Step 1: Fetch the file content
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error(`Failed to fetch file: ${response.status} ${response.statusText}`);
            }
            const arrayBuffer = await response.arrayBuffer();
            const content = new Uint8Array(arrayBuffer);

            // Step 2: Get the temp directory path and write the file
            const { tempDir } = window.__TAURI__.path;
            const { writeFile, mkdir, exists } = window.__TAURI__.fs;
            // Use opener plugin for local file paths (shell.open only works with URLs)
            const { openPath } = window.__TAURI__.opener;

            // Get temp directory
            const tempPath = await tempDir();
            const aesirTempDir = `${tempPath}aesir-citations`;

            // Create our temp subdirectory if it doesn't exist
            try {
                const dirExists = await exists(aesirTempDir);
                if (!dirExists) {
                    await mkdir(aesirTempDir, { recursive: true });
                }
            } catch (e) {
                await mkdir(aesirTempDir, { recursive: true });
            }

            // Step 3: Write file to temp directory
            const filePath = `${aesirTempDir}/${filename}`;
            await writeFile(filePath, content);

            // Step 4: Open the local file with system default app using opener plugin
            await openPath(filePath);

            return true;
        } catch (error) {
            console.error('Failed to download and open natively:', error);
            // Fall back to browser
            window.open(url, '_blank');
            return true;
        }
    }
};

// External link handler - opens URLs in system browser for Tauri
// Used by markdown-rendered external links in chat messages
window.aesirExternalLink = {
    /**
     * Handle external link click
     * In Tauri: opens URL in system default browser
     * In browser: lets target="_blank" handle it naturally
     * @param {Event} event - The click event
     * @param {string} url - The URL to open
     */
    open: async function (event, url) {
        // Only intercept in Tauri - browsers handle target="_blank" natively
        if (typeof window.__TAURI__ !== 'undefined') {
            event.preventDefault();
            try {
                // Use shell plugin to open URL in system default browser
                const { open } = window.__TAURI__.shell;
                await open(url);
            } catch (error) {
                console.error('Failed to open URL in system browser:', error);
                // Fallback: open in new window (will stay in webview but better than nothing)
                window.open(url, '_blank');
            }
        }
        // For browser: do nothing, let target="_blank" work naturally
    }
};

// Text input utilities for enhanced editing functionality
window.aesirTextInput = {
    /**
     * Insert a tab character at the current cursor position in a textarea
     * @param {HTMLElement} element - The textarea or input element
     * @returns {string} The new value of the element
     */
    insertTab: function (element) {
        if (!element) return '';

        // Find the actual textarea inside MudTextField
        const textarea = element.querySelector('textarea') || element.querySelector('input');
        if (!textarea) return '';

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const value = textarea.value;

        // Insert tab character at cursor position
        const newValue = value.substring(0, start) + '\t' + value.substring(end);
        textarea.value = newValue;

        // Move cursor after the inserted tab
        textarea.selectionStart = textarea.selectionEnd = start + 1;

        // Trigger input event so Blazor picks up the change
        textarea.dispatchEvent(new Event('input', { bubbles: true }));

        return newValue;
    },

    /**
     * Set up tab key handling on a text element
     * @param {HTMLElement} element - The element to attach the handler to
     */
    enableTabInsertion: function (element) {
        if (!element) return;

        const textarea = element.querySelector('textarea') || element.querySelector('input');
        if (!textarea) return;

        // Remove any existing handler first
        textarea.removeEventListener('keydown', this._tabHandler);

        // Create and store the handler
        this._tabHandler = (e) => {
            if (e.key === 'Tab') {
                e.preventDefault();
                this.insertTab(element);
            }
        };

        textarea.addEventListener('keydown', this._tabHandler);
    },

    /**
     * Remove tab key handling from a text element
     * @param {HTMLElement} element - The element to remove the handler from
     */
    disableTabInsertion: function (element) {
        if (!element) return;

        const textarea = element.querySelector('textarea') || element.querySelector('input');
        if (!textarea || !this._tabHandler) return;

        textarea.removeEventListener('keydown', this._tabHandler);
    }
};
