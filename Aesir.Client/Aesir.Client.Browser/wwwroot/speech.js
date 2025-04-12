export function speakText(text) {
    var element = document.querySelector('#player-element');

    element.setAttribute('text', text);
    
    element.speak();
}