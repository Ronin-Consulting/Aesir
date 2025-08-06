export function openNewWindow(url) {
    const win = window.open(url, "_blank");

    if (win === null) {
        console.error("Popup was blocked.");
        return false;
    }

    return true;
}