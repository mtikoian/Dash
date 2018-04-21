/*!
 * Draggabilly PACKAGED v2.1.1
 * Make that shiz draggable
 * http://draggabilly.desandro.com
 * MIT license
 *
 * Modified to remove jQuery completely.
 */

/*!
 * getSize v2.0.2
 * measure size of elements
 * MIT license
 */
(function(root, factory) {
    root.getSize = factory(root.$);
})(this, function factory($) {
    /**
     * Get a number from a string, not a percentage.
     * @param {string} value - String to parse.
     * @returns {Number} Returns the number.
     */
    function getStyleSize(value) {
        var num = parseFloat(value);
        // not a percent like '100%', and a number
        return value.indexOf('%') === -1 && !isNaN(num) && num;
    }

    var measurements = [
        'paddingLeft',
        'paddingRight',
        'paddingTop',
        'paddingBottom',
        'marginLeft',
        'marginRight',
        'marginTop',
        'marginBottom',
        'borderLeftWidth',
        'borderRightWidth',
        'borderTopWidth',
        'borderBottomWidth'
    ];

    var measurementsLength = measurements.length;

    /**
     * Build the size measurements for a hidden element.
     * @returns {Object} Measurements for the element.
     */
    function getZeroSize() {
        var size = {
            width: 0,
            height: 0,
            innerWidth: 0,
            innerHeight: 0,
            outerWidth: 0,
            outerHeight: 0
        };
        for (var i = 0; i < measurementsLength; i++) {
            size[measurements[i]] = 0;
        }
        return size;
    }

    var isSetup = false;
    var isBoxSizeOuter;

    /**
     * Setup the instance.
     */
    function setup() {
        // setup once
        if (isSetup) {
            return;
        }
        isSetup = true;

        /**
         * WebKit measures the outer-width on style.width on border-box elems
         * IE & Firefox<29 measures the inner-width
         */
        var div = document.createElement('div');
        $.style(div, { width: '200px', padding: '1px 2px 3px 4px', borderStyle: 'solid', borderWidth: '1px 2px 3px 4px', boxSizing: 'border-box' });

        var body = document.body || document.documentElement;
        body.appendChild(div);
        var style = getComputedStyle(div);

        getSize.isBoxSizeOuter = isBoxSizeOuter = getStyleSize(style.width) === 200;
        body.removeChild(div);
    }

    /**
     * Get the dimensions of a node.
     * @param {Node|string} elem - Node object or query selector.
     * @returns {Object} Dimensions for node.
     */
    function getSize(elem) {
        setup();

        elem = $.get(elem);
        if (!$.isNode(elem)) {
            return;
        }

        var style = getComputedStyle(elem);

        // if hidden, everything is 0
        if (style.display === 'none') {
            return getZeroSize();
        }

        var size = { width: elem.offsetWidth, height: elem.offsetHeight };
        var isBorderBox = size.isBorderBox = style.boxSizing === 'border-box';

        // get all measurements
        for (var i = 0; i < measurementsLength; i++) {
            var measurement = measurements[i];
            var value = style[measurement];
            var num = parseFloat(value);
            // any 'auto', 'medium' value will be 0
            size[measurement] = !isNaN(num) ? num : 0;
        }

        var paddingWidth = size.paddingLeft + size.paddingRight;
        var paddingHeight = size.paddingTop + size.paddingBottom;
        var marginWidth = size.marginLeft + size.marginRight;
        var marginHeight = size.marginTop + size.marginBottom;
        var borderWidth = size.borderLeftWidth + size.borderRightWidth;
        var borderHeight = size.borderTopWidth + size.borderBottomWidth;
        var isBorderBoxSizeOuter = isBorderBox && isBoxSizeOuter;

        // overwrite width and height if we can get it from style
        var styleWidth = getStyleSize(style.width);
        if (styleWidth !== false) {
            // add padding and border unless it's already including it
            size.width = styleWidth + (isBorderBoxSizeOuter ? 0 : paddingWidth + borderWidth);
        }

        var styleHeight = getStyleSize(style.height);
        if (styleHeight !== false) {
            // add padding and border unless it's already including it
            size.height = styleHeight + (isBorderBoxSizeOuter ? 0 : paddingHeight + borderHeight);
        }

        size.innerWidth = size.width - (paddingWidth + borderWidth);
        size.innerHeight = size.height - (paddingHeight + borderHeight);
        size.outerWidth = size.width + marginWidth;
        size.outerHeight = size.height + marginHeight;

        return size;
    }

    return getSize;
});

/**
 * EvEmitter v1.0.3
 * Lil' event emitter
 * MIT License
 */

/* jshint unused: true, undef: true, strict: true */
(function(root, factory) {
    // Assume a traditional browser.
    root.EvEmitter = factory();
}(this, function() {
    /**
     * Constructor for the event emitter.
     */
    function EvEmitter() { }

    EvEmitter.prototype = {
        /**
         * Add an event.
         * @param {string} eventName - New event name.
         * @param {Function} listener - Callback function
         * @returns {Undefined|Object} Returns undefined if the params aren't valid, else a reference to this for chaining.
         */
        on: function(eventName, listener) {
            if (!eventName || !listener) {
                return;
            }
            // set events hash
            var events = this._events = this._events || {};
            // set listeners array
            var listeners = events[eventName] = events[eventName] || [];
            // only add once
            if (listeners.indexOf(listener) === -1) {
                listeners.push(listener);
            }

            return this;
        },

        /**
         * Remove an event.
         * @param {string} eventName - New event name.
         * @param {Function} listener - Callback function
         * @returns {Undefined|Object} Returns undefined if the params aren't valid, else a reference to this for chaining.
         */
        off: function(eventName, listener) {
            var listeners = this._events && this._events[eventName];
            if (!listeners || !listeners.length) {
                return;
            }
            var index = listeners.indexOf(listener);
            if (index !== -1) {
                listeners.splice(index, 1);
            }

            return this;
        },

        /**
         * Dispatch an event.
         * @param {string} eventName - New event name.
         * @param {Array} args - Arguments for the callback.
         * @returns {Undefined|Object} Returns undefined if the params aren't valid, else a reference to this for chaining.
         */
        emitEvent: function(eventName, args) {
            var listeners = this._events && this._events[eventName];
            if (!listeners || !listeners.length) {
                return;
            }
            args = args || [];
            listeners.forEach(function(x) {
                x.apply(this, args);
            });
            return this;
        }
    };

    return EvEmitter;
}));

/*!
 * Unipointer v2.1.0
 * base class for doing one thing with pointer event
 * MIT license
 */

/*jshint browser: true, undef: true, unused: true, strict: true */
(function(root, factory) {
    // Assume a traditional browser.
    root.Unipointer = factory(root.$, root, root.EvEmitter);
}(this, function factory($, root, EvEmitter) {
    /**
     * Constructor for the Unipointer
     */
    function Unipointer() { }

    // inherit EvEmitter
    var proto = Unipointer.prototype = Object.create(EvEmitter.prototype);

    /**
     * Add an event to start dragging.
     * @param {Node} elem - Node to add the event to.
     */
    proto.bindStartEvent = function(elem) {
        this._bindStartEvent(elem, true);
    };

    /**
     * Remove an event to start dragging.
     * @param {Node} elem - Node to remove the event from.
     */
    proto.unbindStartEvent = function(elem) {
        this._bindStartEvent(elem, false);
    };

    /**
     * Add/remove the event to start dragging.
     * @param {Node} elem - Node to add/remove the event to.
     * @param {Boolean} isBind - will unbind if falsey.
     * @private
     */
    proto._bindStartEvent = function(elem, isBind) {
        var bindMethod = $.coalesce(isBind, true) ? 'addEventListener' : 'removeEventListener';

        if (window.navigator.pointerEnabled) {
            // W3C Pointer Events, IE11. See https://coderwall.com/p/mfreca
            elem[bindMethod]('pointerdown', this);
        } else if (window.navigator.msPointerEnabled) {
            // IE10 Pointer Events
            elem[bindMethod]('MSPointerDown', this);
        } else {
            // listen for both, for devices like Chrome Pixel
            elem[bindMethod]('mousedown', this);
            elem[bindMethod]('touchstart', this);
        }
    };

    /**
     * Trigger handler methods for events
     * @param {Event} event - Event to trigger.
     */
    proto.handleEvent = function(event) {
        var method = 'on' + event.type;
        if (this[method]) {
            this[method](event);
        }
    };

    /**
     * Returns the touch that we're keeping track of.
     * @param {Touch[]} touches - Touch history
     * @returns {Touch} Touch event.
     */
    proto.getTouch = function(touches) {
        return $.findByKey(touches, 'identifier', this.pointerIdentifier);
    };

    /**
     * Handle a mouse click.
     * @param {Event} event - Click event.
     */
    proto.onmousedown = function(event) {
        // dismiss clicks from right or middle buttons
        var button = event.button;
        if (button && (button !== 0 && button !== 1)) {
            return;
        }
        this._pointerDown(event, event);
    };

    /**
     * Handle a touch.
     * @param {Touch} event - Touch event.
     */
    proto.ontouchstart = function(event) {
        this._pointerDown(event, event.changedTouches[0]);
    };

    /**
     * Handle a mouse click for IE.
     * @param {Event} event - Click event.
     */
    proto.onMSPointerDown = proto.onpointerdown = function(event) {
        this._pointerDown(event, event);
    };

    /**
     * Start a click/touch event.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @private
     */
    proto._pointerDown = function(event, pointer) {
        // dismiss other pointers
        if (this.isPointerDown) {
            return;
        }

        this.isPointerDown = true;
        // save pointer identifier to match up touch events
        // pointerId for pointer events, touch.indentifier for touch events
        this.pointerIdentifier = pointer.pointerId !== undefined ? pointer.pointerId : pointer.identifier;
        this.pointerDown(event, pointer);
    };

    /**
     * Bind events after the start of a click/touch event.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerDown = function(event, pointer) {
        this._bindPostStartEvents(event);
        this.emitEvent('pointerDown', [event, pointer]);
    };

    // hash of events to be bound after start event
    var postStartEvents = {
        mousedown: ['mousemove', 'mouseup'],
        touchstart: ['touchmove', 'touchend', 'touchcancel'],
        pointerdown: ['pointermove', 'pointerup', 'pointercancel'],
        MSPointerDown: ['MSPointerMove', 'MSPointerUp', 'MSPointerCancel']
    };

    /**
     * Add all of the events that are needed after start dragging.
     * @param {Event} event - Original click/touch event.
     * @private
     */
    proto._bindPostStartEvents = function(event) {
        if (!event) {
            return;
        }
        // get proper events to match start event
        var events = postStartEvents[event.type];
        // bind events to node
        events.forEach(function(eventName) {
            window.addEventListener(eventName, this);
        }, this);
        // save these arguments
        this._boundPointerEvents = events;
    };

    /**
     * Remove all of the events that are needed after start dragging.
     * @param {Event} event - Original click/touch event.
     * @private
     */
    proto._unbindPostStartEvents = function() {
        // check for _boundEvents, in case dragEnd triggered twice (old IE8 bug)
        if (!this._boundPointerEvents) {
            return;
        }
        this._boundPointerEvents.forEach(function(eventName) {
            window.removeEventListener(eventName, this);
        }, this);

        delete this._boundPointerEvents;
    };

    /**
     * Event handler when the mouse moves.
     * @param {Event} event - Original click/touch event.
     */
    proto.onmousemove = function(event) {
        this._pointerMove(event, event);
    };

    /**
     * Event handler when the mouse moves for IE.
     * @param {Event} event - Original click/touch event.
     */
    proto.onMSPointerMove = proto.onpointermove = function(event) {
        if (event.pointerId === this.pointerIdentifier) {
            this._pointerMove(event, event);
        }
    };

    /**
     * Event handler when the touch moves.
     * @param {Event} event - Original click/touch event.
     */
    proto.ontouchmove = function(event) {
        var touch = this.getTouch(event.changedTouches);
        if (touch) {
            this._pointerMove(event, touch);
        }
    };

    /**
     * Handle pointer move from all the different possible inputs.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @private
     */
    proto._pointerMove = function(event, pointer) {
        this.pointerMove(event, pointer);
    };

    /**
     * Trigger the pointer move event for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerMove = function(event, pointer) {
        this.emitEvent('pointerMove', [event, pointer]);
    };

    /**
     * Event handler when the mouse button is released.
     * @param {Event} event - Original click/touch event.
     */
    proto.onmouseup = function(event) {
        this._pointerUp(event, event);
    };

    /**
     * Event handler when the mouse button is released for IE.
     * @param {Event} event - Original click/touch event.
     */
    proto.onMSPointerUp = proto.onpointerup = function(event) {
        if (event.pointerId === this.pointerIdentifier) {
            this._pointerUp(event, event);
        }
    };

    /**
     * Event handler when the touch is released.
     * @param {Event} event - Original click/touch event.
     */
    proto.ontouchend = function(event) {
        var touch = this.getTouch(event.changedTouches);
        if (touch) {
            this._pointerUp(event, touch);
        }
    };

    /**
     * Handle pointer up from all the different possible inputs.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @private
     */
    proto._pointerUp = function(event, pointer) {
        this._pointerDone();
        this.pointerUp(event, pointer);
    };

    /**
     * Trigger the pointer up event for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerUp = function(event, pointer) {
        this.emitEvent('pointerUp', [event, pointer]);
    };

    /**
     * Event handler for when the dragging stops.
     * @private
     */
    proto._pointerDone = function() {
        // reset properties
        this.isPointerDown = false;
        delete this.pointerIdentifier;
        // remove events
        this._unbindPostStartEvents();
        this.pointerDone();
    };

    proto.pointerDone = $.noop;

    /**
     * Event handler when the mouse click is canceled for IE.
     * @param {Event} event - Original click/touch event.
     */
    proto.onMSPointerCancel = proto.onpointercancel = function(event) {
        if (event.pointerId === this.pointerIdentifier) {
            this._pointerCancel(event, event);
        }
    };

    /**
     * Event handler when the touch is canceled.
     * @param {Event} event - Original click/touch event.
     */
    proto.ontouchcancel = function(event) {
        var touch = this.getTouch(event.changedTouches);
        if (touch) {
            this._pointerCancel(event, touch);
        }
    };

    /**
     * Handle pointer cancel from all of the different possible inputs.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @private
     */
    proto._pointerCancel = function(event, pointer) {
        this._pointerDone();
        this.pointerCancel(event, pointer);
    };

    /**
     * Trigger the pointer cancel event for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerCancel = function(event, pointer) {
        this.emitEvent('pointerCancel', [event, pointer]);
    };

    /**
     * Utility function for getting x/y coords from event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @returns {Object} Object with x & y coordinates.
     */
    Unipointer.getPointerPoint = function(pointer) {
        return {
            x: pointer.pageX,
            y: pointer.pageY
        };
    };

    return Unipointer;
}));

/*!
 * Unidragger v2.1.0
 * Draggable base class
 * MIT license
 */
(function(root, factory) {
    // Assume a traditional browser.
    root.Unidragger = factory(root.$, root, root.Unipointer);
}(this, function factory($, root, Unipointer) {
    /**
     * Unidragger constructor.
     */
    function Unidragger() { }

    /**
     * Inherit Unipointer & EvEmitter
     */
    var proto = Unidragger.prototype = Object.create(Unipointer.prototype);

    /**
     * Add events for drag handle.
     */
    proto.bindHandles = function() {
        this._bindHandles(true);
    };

    /**
     * Remove events for drag handle.
     */
    proto.unbindHandles = function() {
        this._bindHandles(false);
    };

    /**
     * Handle adding/removing events for the drag handle.
     * @param {Bool} isBind - Unbind if falsey.
     */
    proto._bindHandles = function(isBind) {
        isBind = $.coalesce(isBind, true);
        // extra bind logic
        var binderExtra;
        var navigator = window.navigator;
        if (navigator.pointerEnabled || navigator.msPointerEnabled) {
            var prop = navigator.pointerEnabled ? 'touchAction' : 'msTouchAction';
            binderExtra = function(handle) {
                // disable scrolling on the element
                handle.style[prop] = isBind ? 'none' : '';
            };
        } else {
            binderExtra = $.noop;
        }
        // bind each handle
        var bindMethod = isBind ? 'addEventListener' : 'removeEventListener';
        for (var i = 0; i < this.handles.length; i++) {
            var handle = this.handles[i];
            this._bindStartEvent(handle, isBind);
            binderExtra(handle);
            handle[bindMethod]('click', this);
        }
    };

    /**
     * Pointer start
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerDown = function(event, pointer) {
        // dismiss range sliders
        if (event.target.nodeName === 'INPUT' && event.target.type === 'range') {
            // reset pointerDown logic
            this.isPointerDown = false;
            delete this.pointerIdentifier;
            return;
        }

        this._dragPointerDown(event, pointer);
        // kludge to blur focused inputs in dragger
        var focused = document.activeElement;
        if (focused && focused.blur) {
            focused.blur();
        }
        // bind move and end events
        this._bindPostStartEvents(event);
        this.emitEvent('pointerDown', [event, pointer]);
    };

    /**
     * Base pointer down logic.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto._dragPointerDown = function(event, pointer) {
        // track to see when dragging starts
        this.pointerDownPoint = Unipointer.getPointerPoint(pointer);
        if (this.canPreventDefaultOnPointerDown(event, pointer)) {
            event.preventDefault();
        }
    };

    /**
     * Overwriteable method so Flickity can prevent for scrolling
     * @param {Event} event - Original mousedown or touchstart event.
     * @returns {Bool} True if allowed to prevent default event, else false.
     */
    proto.canPreventDefaultOnPointerDown = function(event) {
        // prevent default, unless touchstart or <select>
        return event.target.nodeName !== 'SELECT';
    };

    /**
     * Drag move.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerMove = function(event, pointer) {
        var moveVector = this._dragPointerMove(event, pointer);
        this.emitEvent('pointerMove', [event, pointer, moveVector]);
        this._dragMove(event, pointer, moveVector);
    };

    /**
     * Base pointer move logic.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @returns {Object} Object with x & y coordinates.
     */
    proto._dragPointerMove = function(event, pointer) {
        var movePoint = Unipointer.getPointerPoint(pointer);
        var moveVector = {
            x: movePoint.x - this.pointerDownPoint.x,
            y: movePoint.y - this.pointerDownPoint.y
        };
        // start drag if pointer has moved far enough to start drag
        if (!this.isDragging && this.hasDragStarted(moveVector)) {
            this._dragStart(event, pointer);
        }
        return moveVector;
    };

    /**
     * Check if pointer has moved far enough to start drag.
     * @param {Object} moveVector - Object with x & y coordinates.
     * @returns {Bool} True if moved far enough to count as move.
     */
    proto.hasDragStarted = function(moveVector) {
        return Math.abs(moveVector.x) > 3 || Math.abs(moveVector.y) > 3;
    };

    /**
     * Pointer up.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerUp = function(event, pointer) {
        this.emitEvent('pointerUp', [event, pointer]);
        this._dragPointerUp(event, pointer);
    };

    /**
     * Handle mouse/touch up.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto._dragPointerUp = function(event, pointer) {
        if (this.isDragging) {
            this._dragEnd(event, pointer);
        } else {
            // pointer didn't move enough for drag to start
            this._staticClick(event, pointer);
        }
    };

    /**
     * Base drag start logic.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @private
     */
    proto._dragStart = function(event, pointer) {
        this.isDragging = true;
        this.dragStartPoint = Unipointer.getPointerPoint(pointer);
        this.isPreventingClicks = true;
        this.dragStart(event, pointer);
    };

    /**
     * Dispatch drag start event for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.dragStart = function(event, pointer) {
        this.emitEvent('dragStart', [event, pointer]);
    };

    /**
     * Base drag move logic.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @param {Object} moveVector - Object with x & y coordinates.
     * @private
     */
    proto._dragMove = function(event, pointer, moveVector) {
        // do not drag if not dragging yet
        if (!this.isDragging) {
            return;
        }
        this.dragMove(event, pointer, moveVector);
    };

    /**
     * Dispatch drag move event for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @param {Object} moveVector - Object with x & y coordinates.
     */
    proto.dragMove = function(event, pointer, moveVector) {
        event.preventDefault();
        this.emitEvent('dragMove', [event, pointer, moveVector]);
    };

    /**
     * Base logic for drag end.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto._dragEnd = function(event, pointer) {
        this.isDragging = false;
        // re-enable clicking async
        setTimeout(function() {
            delete this.isPreventingClicks;
        }.bind(this));
        this.dragEnd(event, pointer);
    };

    /**
     * Dispatch drag end event for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.dragEnd = function(event, pointer) {
        this.emitEvent('dragEnd', [event, pointer]);
    };

    /**
     * Handle all clicks and prevent clicks when dragging
     * @param {Event} event - Original mousedown or touchstart event.
     */
    proto.onclick = function(event) {
        if (this.isPreventingClicks) {
            event.preventDefault();
        }
    };

    /**
     * Triggered after pointer down & up with no/tiny movement.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto._staticClick = function(event, pointer) {
        // ignore emulated mouse up clicks
        if (this.isIgnoringMouseUp && event.type === 'mouseup') {
            return;
        }

        // allow click in <input>s and <textarea>s
        var nodeName = event.target.nodeName;
        if (nodeName === 'INPUT' || nodeName === 'TEXTAREA') {
            event.target.focus();
        }
        this.staticClick(event, pointer);

        // set flag for emulated clicks 300ms after touchend
        if (event.type !== 'mouseup') {
            this.isIgnoringMouseUp = true;
            // reset flag after 300ms
            setTimeout(function() {
                delete this.isIgnoringMouseUp;
            }.bind(this), 400);
        }
    };

    /**
     * Dispatch event for staticClick for callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.staticClick = function(event, pointer) {
        this.emitEvent('staticClick', [event, pointer]);
    };

    return Unidragger;
}));

/*!
 * Draggabilly v2.1.1
 * Make that shiz draggable
 * http://draggabilly.desandro.com
 * MIT license
 */
(function(root, factory) {
    // Assume a traditional browser.
    root.Draggabilly = factory(root.$, root, root.getSize, root.Unidragger);
}(this, function factory($, root, getSize, Unidragger) {
    var lastTime = 0;
    // get rAF, prefixed, if present. fallback to setTimeout
    var requestAnimationFrame = window.requestAnimationFrame || window.webkitRequestAnimationFrame || window.mozRequestAnimationFrame || function(callback) {
        var currTime = new Date().getTime();
        var timeToCall = Math.max(0, 16 - (currTime - lastTime));
        var id = setTimeout(callback, timeToCall);
        lastTime = currTime + timeToCall;
        return id;
    };
    var transformProperty = $.isString(document.documentElement.style.transform) ? 'transform' : 'WebkitTransform';

    /**
     * Draggabilly constructor.
     * @param {Node|string} element - Node, or querySelector string of node, to make draggable.
     * @param {Object} options - Configuration options.
     */
    function Draggabilly(element, options) {
        this.element = $.get(element);
        this.options = options || {};
        this._create();
    }

    /**
     * Inherit Unidragger methods.
     */
    var proto = Draggabilly.prototype = Object.create(Unidragger.prototype);

    /**
     * CSS position values that don't need to be set.
     */
    var positionValues = { relative: true, absolute: true, fixed: true };

    /**
     * Initialize draggabilly.
     */
    proto._create = function() {
        // properties
        this.position = {};
        this._getPosition();

        this.startPoint = { x: 0, y: 0 };
        this.dragPoint = { x: 0, y: 0 };
        this.startPosition = {};

        // set relative positioning
        var style = getComputedStyle(this.element);
        if (!positionValues[style.position]) {
            this.element.style.position = 'relative';
        }

        this.enable();
        this.setHandles();
    };

    /**
     * Set this.handles and bind start events to the handles.
     */
    proto.setHandles = function() {
        this.handles = this.options.handle ? $.getAll(this.options.handle, this.element) : [this.element];
        this.bindHandles();
    };

    /**
     * Emits events via EvEmitter.
     * @param {String} type - Name of event.
     * @param {Event} event - Original event.
     * @param {Array} args - Extra arguments.
     */
    proto.dispatchEvent = function(type, event, args) {
        this.emitEvent(type, [event].concat(args));
    };

    /**
     * Get x/y position from style.
     */
    proto._getPosition = function() {
        var style = getComputedStyle(this.element);
        var x = this._getPositionCoord(style.left, 'width');
        var y = this._getPositionCoord(style.top, 'height');
        // clean up 'auto' or other non-integer values
        this.position.x = isNaN(x) ? 0 : x;
        this.position.y = isNaN(y) ? 0 : y;

        this._addTransformPosition(style);
    };

    /**
     * Get a numeric coordinate from a style.
     * @param {String} styleSide - CSS style to parse.
     * @param {String} measure - Property name to get the size for.
     * @returns {Number} Resulting size in pixels.
     */
    proto._getPositionCoord = function(styleSide, measure) {
        if (styleSide.indexOf('%') !== -1) {
            // convert percent into pixel for Safari, #75
            var parentSize = getSize(this.element.parentNode);
            // prevent not-in-DOM element throwing bug, #131
            return !parentSize ? 0 : (parseFloat(styleSide) / 100) * parentSize[measure];
        }
        return parseInt(styleSide, 10);
    };

    /**
     * Add `transform: translate( x, y )` to position.
     * @param {CSSStyleDeclaration} style - Element styles to add to.
     */
    proto._addTransformPosition = function(style) {
        var transform = style[transformProperty];
        // bail out if value is 'none'
        if (transform.indexOf('matrix') !== 0) {
            return;
        }
        // split matrix(1, 0, 0, 1, x, y)
        var matrixValues = transform.split(',');
        // translate X value is in 12th or 4th position
        var xIndex = transform.indexOf('matrix3d') === 0 ? 12 : 4;
        var translateX = parseInt(matrixValues[xIndex], 10);
        // translate Y value is in 13th or 5th position
        var translateY = parseInt(matrixValues[xIndex + 1], 10);
        this.position.x += translateX;
        this.position.y += translateY;
    };

    /**
     * Pointer start.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerDown = function(event, pointer) {
        this._dragPointerDown(event, pointer);
        // kludge to blur focused inputs in dragger
        var focused = document.activeElement;
        // do not blur body for IE10, metafizzy/flickity#117
        if (focused && focused.blur && focused !== document.body) {
            focused.blur();
        }
        // bind move and end events
        this._bindPostStartEvents(event);
        $.addClass(this.element, 'is-pointer-down');
        this.dispatchEvent('pointerDown', event, [pointer]);
    };

    /**
     * Drag move.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerMove = function(event, pointer) {
        var moveVector = this._dragPointerMove(event, pointer);
        this.dispatchEvent('pointerMove', event, [pointer, moveVector]);
        this._dragMove(event, pointer, moveVector);
    };

    /**
     * Drag start.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.dragStart = function(event, pointer) {
        if (!this.isEnabled) {
            return;
        }
        this._getPosition();
        this.measureContainment();
        // position _when_ drag began
        this.startPosition.x = this.position.x;
        this.startPosition.y = this.position.y;
        // reset left/top style
        this.setLeftTop();

        this.dragPoint.x = 0;
        this.dragPoint.y = 0;

        $.addClass(this.element, 'is-dragging');
        this.dispatchEvent('dragStart', event, [pointer]);
        // start animation
        this.animate();
    };

    /**
     * Calculate the correct position of the element within its container.
     */
    proto.measureContainment = function() {
        var containment = this.options.containment;
        if (!containment) {
            return;
        }

        // use element if element, otherwise just `true`, use the parent
        var container = $.isNode(containment) ? containment : $.isString(containment) ? $.get(containment) : this.element.parentNode;
        var elemSize = getSize(this.element);
        var containerSize = getSize(container);
        var elemRect = this.element.getBoundingClientRect();
        var containerRect = container.getBoundingClientRect();
        var borderSizeX = containerSize.borderLeftWidth + containerSize.borderRightWidth;
        var borderSizeY = containerSize.borderTopWidth + containerSize.borderBottomWidth;

        var position = this.relativeStartPosition = {
            x: elemRect.left - (containerRect.left + containerSize.borderLeftWidth),
            y: elemRect.top - (containerRect.top + containerSize.borderTopWidth)
        };

        this.containSize = {
            width: (containerSize.width - borderSizeX) - position.x - elemSize.width,
            height: (containerSize.height - borderSizeY) - position.y - elemSize.height
        };
    };

    /**
     * Drag move.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     * @param {Object} moveVector - Object with x and y coordinates of current position.
     */
    proto.dragMove = function(event, pointer, moveVector) {
        if (!this.isEnabled) {
            return;
        }

        var dragX = moveVector.x;
        var dragY = moveVector.y;
        var grid = this.options.grid;
        var gridX = grid && grid[0];
        var gridY = grid && grid[1];

        dragX = applyGrid(dragX, gridX);
        dragY = applyGrid(dragY, gridY);

        dragX = this.containDrag('x', dragX, gridX);
        dragY = this.containDrag('y', dragY, gridY);

        // constrain to axis
        dragX = this.options.axis === 'y' ? 0 : dragX;
        dragY = this.options.axis === 'x' ? 0 : dragY;

        if (this.options.minZero) {
            dragX = applyMinZero(this.startPosition.x, dragX);
            dragY = applyMinZero(this.startPosition.y, dragY);
        }

        this.position.x = this.startPosition.x + dragX;
        this.position.y = this.startPosition.y + dragY;
        // set dragPoint properties
        this.dragPoint.x = dragX;
        this.dragPoint.y = dragY;

        this.dispatchEvent('dragMove', event, [pointer, moveVector]);
    };

    /**
     * Adjust a coordinate to snap to the grid.
     * @param {Number} value - Coordinate to snap to grid.
     * @param {Number} grid - Grid width/height.
     * @param {String} method - Math method to apply.
     * @returns {Number} Closes number that snaps to grid.
     */
    function applyGrid(value, grid, method) {
        return grid ? Math[method || 'round'](value / grid) * grid : value;
    }

    /**
     * Adjust the drag position to prevent the item from moving out of the container to the left or top.
     * @param {Number} start - Element starting coordinate.
     * @param {Number} drag - Distance element was dragged.
     * @returns {Number} Corrected drag coordinate.
     */
    function applyMinZero(start, drag) {
        return start + drag < 0 ? -start : drag;
    }

    /**
     * Limit dragging within boundaries.
     * @param {string} axis - X or Y Axis
     * @param {number} drag - Position coordinate on axis
     * @param {number} grid - Boundary coordinate on axis.
     * @returns {number} Coordinate contained with drag.
     */
    proto.containDrag = function(axis, drag, grid) {
        if (!this.options.containment) {
            return drag;
        }
        var measure = axis === 'x' ? 'width' : 'height';

        var rel = this.relativeStartPosition[axis];
        var min = applyGrid(-rel, grid, 'ceil');
        var max = this.containSize[measure];
        max = applyGrid(max, grid, 'floor');
        return Math.min(max, Math.max(min, drag));
    };

    /**
     * pointer up
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.pointerUp = function(event, pointer) {
        $.removeClass(this.element, 'is-pointer-down');
        this.dispatchEvent('pointerUp', event, [pointer]);
        this._dragPointerUp(event, pointer);
    };

    /**
     * Drag end.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.dragEnd = function(event, pointer) {
        if (!this.isEnabled) {
            return;
        }
        // use top left position when complete
        if (transformProperty) {
            this.element.style[transformProperty] = '';
            this.setLeftTop();
        }
        $.removeClass(this.element, 'is-dragging');
        this.dispatchEvent('dragEnd', event, [pointer]);
    };

    /**
     * Animate while dragging.
     */
    proto.animate = function() {
        if (!this.isDragging) {
            return;
        }
        this.positionDrag();
        requestAnimationFrame(this.animate.bind(this));
    };

    /**
     * Set left/top positioning.
     */
    proto.setLeftTop = function() {
        $.style(this.element, { left: this.position.x + 'px', top: this.position.y + 'px' });
    };

    /**
     * Set position transform based on drag.
     */
    proto.positionDrag = function() {
        this.element.style[transformProperty] = 'translate3d( ' + this.dragPoint.x + 'px, ' + this.dragPoint.y + 'px, 0)';
    };

    /**
     * Dispatch the event for static click callbacks.
     * @param {Event} event - Original mousedown or touchstart event.
     * @param {Event|Touch} pointer - Event object that has .pageX and .pageY.
     */
    proto.staticClick = function(event, pointer) {
        this.dispatchEvent('staticClick', event, [pointer]);
    };

    /**
     * Enable dragging.
     */
    proto.enable = function() {
        this.isEnabled = true;
    };

    /**
     * Disable dragging.
     */
    proto.disable = function() {
        this.isEnabled = false;
        if (this.isDragging) {
            this.dragEnd();
        }
    };

    /**
     * Destroy this instance.
     */
    proto.destroy = function() {
        this.disable();
        this.element.removeAttribute('style');
        this.unbindHandles();
    };

    return Draggabilly;
}));
