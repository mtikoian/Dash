/*!
 * Polyfill for IE CustomEvent
 * https://developer.mozilla.org/en-US/docs/Web/API/CustomEvent/CustomEvent#Polyfill
 */
(function(root) {
    if (typeof root.CustomEvent === 'function') {
        return false;
    }

    /**
     * Custom event constructor.
     * @param {string} event - Name of the event.
     * @param {Object} params - Parameters for the event.
     * @returns {CustomEvent} New CustomEvent instance.
     */
    function CustomEvent(event, params) {
        params = params || { bubbles: false, cancelable: false, detail: undefined };
        var evt = document.createEvent('CustomEvent');
        evt.initCustomEvent(event, params.bubbles, params.cancelable, params.detail);
        return evt;
    }

    CustomEvent.prototype = root.Event.prototype;
    root.CustomEvent = CustomEvent;
})(this);
