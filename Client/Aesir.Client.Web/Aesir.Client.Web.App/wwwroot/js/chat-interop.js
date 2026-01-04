// Chat interop functions for Blazor
window.scrollToBottom = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.scrollToBottomSmooth = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTo({
            top: element.scrollHeight,
            behavior: 'smooth'
        });
    }
};

window.isScrolledToBottom = function (elementId, threshold = 50) {
    const element = document.getElementById(elementId);
    if (element) {
        return element.scrollHeight - element.scrollTop - element.clientHeight < threshold;
    }
    return true;
};

// File drop handling
// Store reference to the hidden file input element ID
window.dropZoneInputId = null;

window.initializeDropZone = function (chatPageId, fileInputId) {
    window.dropZoneInputId = fileInputId;
    const chatPage = document.getElementById(chatPageId) || document.querySelector('.' + chatPageId);

    if (!chatPage) {
        console.warn('Chat page element not found:', chatPageId);
        return;
    }

    // Override the native drop handler to redirect files to our hidden input
    chatPage.addEventListener('drop', function(e) {
        e.preventDefault();
        e.stopPropagation();

        const files = e.dataTransfer?.files;
        if (files && files.length > 0 && window.dropZoneInputId) {
            const fileInput = document.getElementById(window.dropZoneInputId);
            if (fileInput) {
                // Create a DataTransfer to set files on the input
                const dt = new DataTransfer();
                for (let i = 0; i < files.length; i++) {
                    dt.items.add(files[i]);
                }
                fileInput.files = dt.files;

                // Trigger the change event so Blazor picks it up
                const event = new Event('change', { bubbles: true });
                fileInput.dispatchEvent(event);
            }
        }
    }, true); // Use capture phase
};

// Trigger file input click programmatically
window.triggerFileInput = function (inputElement) {
    if (inputElement) {
        inputElement.click();
    }
};

// Download text content as a file
window.downloadText = function (content, filename, mimeType) {
    const bytes = new TextEncoder().encode(content);
    const blob = new Blob([bytes], { type: mimeType || 'text/plain' });
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
};

// Download file with proper filename
window.downloadFile = async function (url, filename) {
    try {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const blob = await response.blob();
        const blobUrl = window.URL.createObjectURL(blob);

        const link = document.createElement('a');
        link.href = blobUrl;
        link.download = filename;
        link.style.display = 'none';
        document.body.appendChild(link);
        link.click();

        // Cleanup
        setTimeout(() => {
            document.body.removeChild(link);
            window.URL.revokeObjectURL(blobUrl);
        }, 100);
    } catch (error) {
        console.error('Failed to download file:', error);
        throw error;
    }
};

// Copy code block to clipboard
window.copyCodeBlock = function (codeId) {
    const codeElement = document.getElementById('code-' + codeId);
    if (codeElement) {
        const text = codeElement.textContent || codeElement.innerText;
        navigator.clipboard.writeText(text).then(() => {
            // Find the button and update its text temporarily
            const wrapper = codeElement.closest('.code-block-wrapper');
            if (wrapper) {
                const btn = wrapper.querySelector('.code-copy-btn');
                const copyText = btn.querySelector('.copy-text');
                if (copyText) {
                    const originalText = copyText.textContent;
                    copyText.textContent = 'Copied!';
                    btn.classList.add('copied');
                    setTimeout(() => {
                        copyText.textContent = originalText;
                        btn.classList.remove('copied');
                    }, 2000);
                }
            }
        }).catch(err => {
            console.error('Failed to copy code:', err);
        });
    }
};

// Citation Handler - Manages citation link clicks and callbacks to Blazor
window.aesirCitationHandler = {
    // Reference to the .NET object that will handle citations
    dotNetReference: null,

    // Initialize the citation handler with a .NET object reference
    initialize: function (dotNetRef) {
        this.dotNetReference = dotNetRef;
        console.log('Citation handler initialized');
    },

    // Dispose the citation handler
    dispose: function () {
        this.dotNetReference = null;
    },

    // Called when a citation link is clicked
    openCitation: function (element) {
        if (!element) {
            console.warn('Citation element is null');
            return;
        }

        // Extract data attributes from the clicked element
        const conversationId = element.getAttribute('data-conversation-id');
        const filename = element.getAttribute('data-filename');
        const extension = element.getAttribute('data-extension');
        const pageAttr = element.getAttribute('data-page');
        const page = pageAttr ? parseInt(pageAttr, 10) : null;

        if (!conversationId || !filename) {
            console.warn('Missing required citation data attributes');
            return;
        }

        // Create citation info object
        const citationInfo = {
            conversationId: conversationId,
            fileName: filename,
            fileExtension: extension || '',
            pageNumber: page
        };

        // Call the .NET handler if available
        if (this.dotNetReference) {
            this.dotNetReference.invokeMethodAsync('HandleCitationClick', citationInfo)
                .catch(err => console.error('Failed to invoke citation handler:', err));
        } else {
            console.warn('Citation handler not initialized. Citation:', citationInfo);
            // Fallback: log the citation info for debugging
        }
    }
};

// Keyboard shortcut handler for citation viewer (Escape to close)
window.initializeCitationKeyboardShortcuts = function (dotNetRef) {
    const handler = function (e) {
        if (e.key === 'Escape') {
            dotNetRef.invokeMethodAsync('HandleEscapeKey')
                .catch(err => console.error('Failed to handle escape key:', err));
        }
    };

    document.addEventListener('keydown', handler);

    // Return a function to dispose the handler
    return {
        dispose: function () {
            document.removeEventListener('keydown', handler);
        }
    };
};
