/*!
 * Contains all the common JS functions Dash needs.
 */
(function(root) {
    'use strict';

    /**
     * Add a class to an element.
     * @param {Node} element - Element to add the class to.
     * @param {string} className - Name of class to add.
     */
    var addClass = function(element, className) {
        var node = get(element);
        if (node) {
            node.classList.add(className);
        }
    };

    /**
     * Get closest parent that matches the selector.
     * @param {string} selector - ID, class name, tag name, or data attribute to find.
     * @param {Node} node - Node to start search from.
     * @returns {Node} Matched node or null.
     */
    var closest = function(selector, node) {
        var firstChar = selector.charAt(0);
        var tSelector = selector.substr(1);
        var lowerSelector = selector.toLowerCase();

        while (node !== document) {
            node = node.parentNode;
            if (!node) {
                return null;
            }

            // If selector is a class
            if (firstChar === '.' && node.classList && node.classList.contains(tSelector)) {
                return node;
            }
            // If selector is an ID
            if (firstChar === '#' && node.id === tSelector) {
                return node;
            }
            // If selector is a data attribute
            if (firstChar === '[' && node.hasAttribute(selector.substr(1, selector.length - 2))) {
                return node;
            }
            // If selector is a tag
            if (node.tagName && node.tagName.toLowerCase() === lowerSelector) {
                return node;
            }
        }
        return null;
    };

    /**
     * Coalesce value and defValue.
     * @param {*} value - First value to check.
     * @param {*} defValue - Default value.
     * @returns {*} Value if it is not null, else defValue.
     */
    var coalesce = function(value, defValue) {
        return isNull(value) ? defValue : value;
    };

    /**
     * Create a dom node from an html string. Expects a single root element.
     * @param {string} html - HTML content for the node.
     * @returns {Node} New DOM node.
     */
    var createNode = function(html) {
        var node = document.createElement('div');
        node.innerHTML = html;
        return html && html.length ? node.children[0] : node;
    };

    /**
     * Create a debounce handler to prevent a function from being called too frequently.
     * @param {Function} fn - Function to debounce.
     * @param {number} wait - Milliseconds to wait between running the function.
     * @returns {Function} A closure wrapping the passed in function.
     */
    var debounce = function(fn, wait) {
        var timeout, args, context, timestamp;

        return function() {
            context = this;
            args = [].slice.call(arguments, 0);
            timestamp = new Date();

            var later = function() {
                var last = new Date() - timestamp;
                if (last < wait) {
                    // if the latest call was less that the wait period ago then we reset the timeout to wait for the difference
                    timeout = setTimeout(later, wait - last);
                } else {
                    // if not we can null out the timer and run the latest
                    timeout = null;
                    fn.apply(context, args);
                }
            };

            // we only need to set the timer now if one isn't already running
            if (!timeout) {
                timeout = setTimeout(later, wait);
            }
        };
    };

    /**
     * Destroy an object.
     * @param {Object} obj - Object to destroy.
     */
    var destroy = function(obj) {
        if (isNull(obj)) {
            return;
        }
        if (obj.destroy) {
            obj.destroy();
        }
        obj = null;
    };

    /**
     * Conditionally disable a input.
     * @param {Node} node - Input to enable/disable.
     * @param {bool} disable - True to disable, false to enable.
     */
    var disableIf = function(node, disable) {
        if (!node) {
            return;
        }
        if (disable) {
            node.setAttribute('disabled', true);
        } else {
            node.removeAttribute('disabled');
        }
    };

    /**
     * Recursively merge multiple objects, combining values of arguments into first argument. Rightmost values take precedence.
     * @returns {*} Updated first argument.
     */
    var extend = function() {
        var l = arguments.length, key, i;
        var result = l > 0 ? arguments[0] : {};
        if (isNull(result)) {
            result = {};
        }
        for (i = 1; i < l; i++) {
            for (key in arguments[i]) {
                if (arguments[i].hasOwnProperty(key)) {
                    result[key] = arguments[i][key];
                }
            }
        }
        return result;
    };

    /**
     * Get an object from an array where the obj[key]===value.
     * @param {*[]} arr - Array to search in.
     * @param {string} key - Property name to check.
     * @param {*} value - Value to find.
     * @returns {*} Array value that matches or null.
     */
    var findByKey = function(arr, key, value) {
        if (!arr || isNull(key)) {
            return;
        }
        var i = arr.length - 1;
        while (i > -1) {
            if (arr[i][key] === value) {
                arr[i]._i = i;
                return arr[i];
            }
            i--;
        }
        return null;
    };

    /**
     * Get an element matching selector.
     * @param {string} selector - ID, class name, or any valid query selector.
     * @param {Node} container - Only search within this node.
     * @returns {Node} Matched node.
     */
    var get = function(selector, container) {
        if (typeof selector !== 'string') {
            return selector;
        }
        if (container) {
            return container.querySelector(selector);
        }
        var simple = selector.indexOf(' ', 1) === -1 && selector.indexOf('.', 1) === -1;
        if (!simple) {
            return document.querySelector(selector);
        }
        var sel = selector.charAt(0);
        if (sel === '#') {
            return document.getElementById(selector.substr(1));
        } else if (sel === '.') {
            var res = document.getElementsByClassName(selector.substr(1));
            return res.length ? res[0] : null;
        } else {
            return document.querySelector(selector);
        }
    };

    /**
     * Get all elements matching selector.
     * @param {string} selector - ID, class name, or any valid query selector.
     * @param {Node} container - Only search within this node.
     * @param {bool} includeContainer - If true check if container matches selector and add it to resultset.
     * @returns {Node[]} Non-live array of matched nodes.
     */
    var getAll = function(selector, container, includeContainer) {
        var list;
        if (selector.charAt(0) === '.' && selector.indexOf(',') === -1 && selector.indexOf('>') === -1) {
            list = (container || document).getElementsByClassName(selector.substr(1));
        } else {
            list = (container || document).querySelectorAll(selector);
        }
        var res = Array.prototype.slice.call(list);
        if (includeContainer && container && _matches(container, selector)) {
            res.unshift(container);
        }
        return res;
    };

    /**
     * Check if an element has a class assigned to it.
     * @param {Node} element - Element to check.
     * @param {string} className - Name of class to look for.
     * @returns {bool} True if the element has the class.
     */
    var hasClass = function(element, className) {
        var node = get(element);
        return node && node.classList && node.classList.contains(className);
    };

    /**
     * Hide an element.
     * @param {Node} element - Element to hide.
     * @param {bool} maintainLayout - Maintain the spacing of the element if true, default to false.
     */
    var hide = function(element, maintainLayout) {
        var node = get(element);
        if (node) {
            if (coalesce(maintainLayout, false)) {
                addClass(node, 'd-invisible');
            } else {
                addClass(node, 'd-none');
            }
        }
    };

    /**
     * Check if a variable is an array.
     * @param {*} x - Variable to check the type of.
     * @returns {bool} True if x is an array.
     */
    var isArray = function(x) {
        return !isNull(x) && x.constructor === Array;
    };

    /**
     * Check if a variable is a function.
     * @param {*} x - Variable to check the type of.
     * @returns {bool} True if x is a function.
     */
    var isFunction = function(x) {
        return typeof x === 'function';
    };

    /**
     * Check if a variable is undefined or null.
     * @param {*} x - Variable to check the value of.
     * @returns {bool} True if x is undefined or null.
     */
    var isNull = function(x) {
        return typeof x === 'undefined' || x === null;
    };

    /**
     * Check if a variable is a string.
     * @param {*} x - Variable to check the type of.
     * @returns {bool} True if x is a string.
     */
    var isString = function(x) {
        return typeof x === 'string';
    };

    /**
     * Check if an element should be visible.
     * @param {Node} element - Node to check.
     * @returns {Bool} True if visible else false.
     */
    var isVisible = function(element) {
        var node = get(element);
        return node && node.offsetParent !== null;
    };

    /**
     * Remove an event from an element.
     * @param {Node} element - Element to remove the event from.
     * @param {string} event - Event name to remove.
     * @param {Function} fn - Function to remove.
     * @param {bool} useCapture - Dispatch to this listener before any before it.
     */
    var off = function(element, event, fn, useCapture) {
        var node = get(element);
        if (node) {
            node.removeEventListener(event, fn, useCapture);
        }
    };

    /**
     * Attach an event to an element.
     * @param {Node} element - Element to attach the event to.
     * @param {string} event - Event name to attach.
     * @param {Function} fn - Function to run when the event fires.
     * @param {bool} useCapture - Dispatch to this listener before any before it.
     */
    var on = function(element, event, fn, useCapture) {
        var node = get(element);
        if (node) {
            node.addEventListener(event, fn, useCapture);
        }
    };

    /**
     * Remove a class from an element.
     * @param {Node} element - Element to remove the class from.
     * @param {string} className - Name of class to remove.
     */
    var removeClass = function(element, className) {
        var node = get(element);
        if (node) {
            node.classList.remove(className);
        }
    };

    /**
     * Show a hidden element.
     * @param {Node} element - Element to show.
     */
    var show = function(element) {
        var node = get(element);
        if (node) {
            removeClass(node, 'd-invisible');
            removeClass(node, 'd-none');
        }
    };

    /**
     * Set the text content of a node.
     * @param {Node} element - Element to update.
     * @param {string} text - New text content.
     */
    var text = function(element, text) {
        var node = get(element);
        if (node) {
            node.textContent = text;
        }
    };

    /**
     * Add/remove a class from an element based on the value of toggle.
     * @param {Node|string} element - Element to modify, or selector to get element.
     * @param {string} className - Name of class to add/remove.
     * @param {bool|undefined} toggle - If true add class, if false remove class, if null toggle based on current status.
     * @returns {undefined}
     */
    var toggleClass = function(element, className, toggle) {
        var node = get(element);
        if (isNull(toggle)) {
            toggle = !hasClass(node, className);
        }
        toggle ? addClass(node, className) : removeClass(node, className);
        return;
    };

    /**
    * Trigger an event on a node.
    * @param {Node} element - Element to attach the event to.
    * @param {string} eventName - Name of event to trigger.
    */
    var trigger = function(element, eventName) {
        var node = element ? get(element) : window;
        if (node) {
            var event;
            if (typeof (Event) === 'function') {
                event = new Event(eventName);
            } else {
                event = document.createEvent('Event');
                event.initEvent(eventName, true, true);
            }
            node.dispatchEvent(event);
        }
    };

    /**
     * Verify if an element would be matched by a selector.
     * @param {Node} element - Node to compare the selector to.
     * @param {string} selector - Valid CSS selector.
     * @returns {bool} True if the element matches the selector.
     */
    var _matches = function(element, selector) {
        var p = Element.prototype;
        var f = p.matches || p.webkitMatchesSelector || p.mozMatchesSelector || p.msMatchesSelector || function(s) {
            return [].indexOf.call(getAll(s), this) !== -1;
        };
        return f.call(element, selector);
    };

    root.$ = {
        addClass: addClass,
        closest: closest,
        coalesce: coalesce,
        createNode: createNode,
        debounce: debounce,
        destroy: destroy,
        disableIf: disableIf,
        extend: extend,
        get: get,
        getAll: getAll,
        findByKey: findByKey,
        hasClass: hasClass,
        hide: hide,
        isArray: isArray,
        isFunction: isFunction,
        isNull: isNull,
        isString: isString,
        isVisible: isVisible,
        off: off,
        on: on,
        removeClass: removeClass,
        show: show,
        text: text,
        toggleClass: toggleClass,
        trigger: trigger
    };
}(this));
