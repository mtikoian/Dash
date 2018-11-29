/*!
 * ColorPicker - pure JavaScript color picker without using images, external CSS or 1px divs.
 * Copyright © 2011 David Durman, All rights reserved.
 * https://github.com/DavidDurman/FlexiColorPicker
 */
(function(factory) {
    // Assume a traditional browser.
    window.ColorPicker = factory(this.$, this.Draggabilly);
})(function($, Draggabilly) {
    'use strict';

    var picker,
        slide,
        svgNS = 'http://www.w3.org/2000/svg',
        uniqID = 0;

    /**
     * This HTML snippet is inserted into the innerHTML property of the passed color picker element when the no-hassle call to ColorPicker() is used,
     * i.e. ColorPicker(function(hex, hsv, rgb) { ... });.
     */
    var colorpickerHTMLSnippet = '<div class="cp-picker-wrapper"><div class="cp-picker"></div><div class="cp-drag-handle cp-picker-indicator"></div></div><div class="cp-slide-wrapper"><div class="cp-slide"></div><div class="cp-drag-handle cp-slide-indicator"></div></div>';

    /**
     * Create SVG element.
     * @param {string} el - Node name.
     * @param {Object} attrs - Attributes for the node.
     * @param {Object[]} children - Child nodes.
     * @return {Node} New DOM node.
     */
    function c(el, attrs, children) {
        var node = document.createElementNS(svgNS, el);
        for (var key in attrs) {
            node.setAttribute(key, attrs[key]);
        }
        if (!$.isNull(children)) {
            if (!$.isArray(children)) {
                children = [children];
            }
            children.forEach(function(x) { node.appendChild(x); });
        }
        return node;
    }

    /**
     * Create slide and picker markup.
     */
    slide = c('svg', { xmlns: 'http://www.w3.org/2000/svg', version: '1.1', width: '100%', height: '100%' }, [
        c('defs', {},
            c('linearGradient', { id: 'gradient-hsv', x1: '0%', y1: '100%', x2: '0%', y2: '0%' }, [
                c('stop', { offset: '0%', 'stop-color': '#FF0000', 'stop-opacity': '1' }),
                c('stop', { offset: '13%', 'stop-color': '#FF00FF', 'stop-opacity': '1' }),
                c('stop', { offset: '25%', 'stop-color': '#8000FF', 'stop-opacity': '1' }),
                c('stop', { offset: '38%', 'stop-color': '#0040FF', 'stop-opacity': '1' }),
                c('stop', { offset: '50%', 'stop-color': '#00FFFF', 'stop-opacity': '1' }),
                c('stop', { offset: '63%', 'stop-color': '#00FF40', 'stop-opacity': '1' }),
                c('stop', { offset: '75%', 'stop-color': '#0BED00', 'stop-opacity': '1' }),
                c('stop', { offset: '88%', 'stop-color': '#FFFF00', 'stop-opacity': '1' }),
                c('stop', { offset: '100%', 'stop-color': '#FF0000', 'stop-opacity': '1' })
            ])
        ),
        c('rect', { x: '0', y: '0', width: '100%', height: '100%', fill: 'url(#gradient-hsv)' })
    ]);

    picker = c('svg', { xmlns: 'http://www.w3.org/2000/svg', version: '1.1', width: '100%', height: '100%' }, [
        c('defs', {}, [
            c('linearGradient', { id: 'gradient-black', x1: '0%', y1: '100%', x2: '0%', y2: '0%' }, [
                c('stop', { offset: '0%', 'stop-color': '#000000', 'stop-opacity': '1' }),
                c('stop', { offset: '100%', 'stop-color': '#CC9A81', 'stop-opacity': '0' })
            ]),
            c('linearGradient', { id: 'gradient-white', x1: '0%', y1: '100%', x2: '100%', y2: '100%' }, [
                c('stop', { offset: '0%', 'stop-color': '#FFFFFF', 'stop-opacity': '1' }),
                c('stop', { offset: '100%', 'stop-color': '#CC9A81', 'stop-opacity': '0' })
            ])
        ]),
        c('rect', { x: '0', y: '0', width: '100%', height: '100%', fill: 'url(#gradient-white)' }),
        c('rect', { x: '0', y: '0', width: '100%', height: '100%', fill: 'url(#gradient-black)' })
    ]);

    /**
     * ColorPicker constructor.
     * @param {Node} container  - Picker parent element.
     * @param {Function} callback - Called whenever the color is changed provided chosen color in RGB HEX format as the only argument.
     */
    function ColorPicker(container, callback) {
        this.h = 0;
        this.s = 1;
        this.v = 1;
        this.draggie = [];

        container.innerHTML = colorpickerHTMLSnippet;
        this.slideElement = container.getElementsByClassName('cp-slide')[0];
        this.pickerElement = container.getElementsByClassName('cp-picker')[0];
        this.slideIndicator = container.getElementsByClassName('cp-slide-indicator')[0];
        this.pickerIndicator = container.getElementsByClassName('cp-picker-indicator')[0];
        this.callback = callback;

        // Generate uniq IDs for linearGradients so that we don't have the same IDs within one document.
        // Then reference those gradients in the associated rectangles.

        var slideClone = slide.cloneNode(true);
        var pickerClone = picker.cloneNode(true);
        var hsvGradient = $.get('#gradient-hsv', slideClone);
        var hsvRect = $.get('rect', slideClone);

        hsvGradient.id = 'gradient-hsv-' + uniqID;
        hsvRect.setAttribute('fill', 'url(#' + hsvGradient.id + ')');

        var blackAndWhiteGradients = [$.get('#gradient-black', pickerClone), $.get('#gradient-white', pickerClone)];
        var whiteAndBlackRects = $.getAll('rect', pickerClone);

        blackAndWhiteGradients[0].id = 'gradient-black-' + uniqID;
        blackAndWhiteGradients[1].id = 'gradient-white-' + uniqID;

        whiteAndBlackRects[0].setAttribute('fill', 'url(#' + blackAndWhiteGradients[1].id + ')');
        whiteAndBlackRects[1].setAttribute('fill', 'url(#' + blackAndWhiteGradients[0].id + ')');

        this.slideElement.appendChild(slideClone);
        this.pickerElement.appendChild(pickerClone);

        uniqID++;

        this.slideListener = this.slideHandler.bind(this);
        this.pickerListener = this.pickerHandler.bind(this);
        this.addEvents(this.slideElement, this.slideListener);
        this.addEvents(this.pickerElement, this.pickerListener);
    }

    ColorPicker.prototype = {
        /**
         * Convert HSV representation to RGB HEX string. Credits to http://www.raphaeljs.com.
         * @param {Object} hsv - Object with h, s, and v properties.
         * @returns {Object} Object with RGB and hex value.
         */
        hsv2rgb: function(hsv) {
            hsv = $.coalesce(hsv, this);
            var R, G, B, X, C;
            var h = (hsv.h % 360) / 60;

            C = hsv.v * hsv.s;
            X = C * (1 - Math.abs(h % 2 - 1));
            R = G = B = hsv.v - C;

            h = ~~h;
            R += [C, X, 0, 0, X, C][h];
            G += [X, C, C, X, 0, 0][h];
            B += [0, 0, X, C, C, X][h];

            var r = Math.floor(R * 255);
            var g = Math.floor(G * 255);
            var b = Math.floor(B * 255);
            return { r: r, g: g, b: b, hex: '#' + (16777216 | b | (g << 8) | (r << 16)).toString(16).slice(1) };
        },

        /**
         * Convert RGB representation to HSV. r, g, b can be either in <0,1> range or <0,255> range. Credits to http://www.raphaeljs.com.
         * @param {Object} rgb - Object with r, g, and b properties.
         * @returns {Object} Object wth HSV values.
         */
        rgb2hsv: function(rgb) {
            var r = rgb.r;
            var g = rgb.g;
            var b = rgb.b;

            if (rgb.r > 1 || rgb.g > 1 || rgb.b > 1) {
                r /= 255;
                g /= 255;
                b /= 255;
            }

            var H, S, V, C;
            V = Math.max(r, g, b);
            C = V - Math.min(r, g, b);
            H = (C === 0 ? null :
                V === r ? (g - b) / C + (g < b ? 6 : 0) :
                    V === g ? (b - r) / C + 2 :
                        (r - g) / C + 4);
            H = (H % 6) * 60;
            S = C === 0 ? 0 : C / V;
            return { h: H, s: S, v: V };
        },

        /**
         * Convert hex string to RGB.
         * @param {string} hex - Hex string.
         * @returns {Object} Object with RGB properties.
         */
        hex2rgb: function(hex) {
            return { r: parseInt(hex.substr(1, 2), 16), g: parseInt(hex.substr(3, 2), 16), b: parseInt(hex.substr(5, 2), 16) };
        },

        /**
         * Convert hex string to hsv values.
         * @param {string} hex - Hex string.
         * @returns {Object} Object with HSV values.
         */
        hex2hsv: function(hex) {
            return this.rgb2hsv(this.hex2rgb(hex));
        },

        /**
         * Click event handler for the slider. Sets picker background color and calls callback if provided.
         * @param {Event} event - Original mouseup or touchend event.
         * @param {MouseEvent|Touch} pointer - Event object that has .pageX and .pageY.
         * @param {Object} moveVector - Move distance as x/y properties.
         */
        slideHandler: function(event, pointer, moveVector) {
            var rect = this.slideElement.getBoundingClientRect();
            if (!this.contains(rect, event)) {
                return;
            }

            this.h = (event.y - rect.top) / this.slideElement.offsetHeight * 360;
            $.style(this.pickerElement, { 'background-color': this.hsv2rgb({ h: this.h, s: 1, v: 1 }).hex });
            if (!moveVector) {
                this.positionIndicators();
            }
            if (this.callback) {
                this.callback(this.hsv2rgb({ h: this.h, s: this.s, v: this.v }).hex);
            }
        },

        /**
         * Click event handler for the picker. Calls callback if provided.
         * @param {Event} event - Original mouseup or touchend event.
         * @param {MouseEvent|Touch} pointer - Event object that has .pageX and .pageY.
         * @param {Object} moveVector - Move distance as x/y properties.
         */
        pickerHandler: function(event, pointer, moveVector) {
            var rect = this.pickerElement.getBoundingClientRect();
            if (!this.contains(rect, event)) {
                return;
            }

            this.s = (event.x - rect.left) / this.pickerElement.offsetWidth;
            var height = this.pickerElement.offsetHeight;
            this.v = (height - (event.y - rect.top) - this.pickerElement.offsetTop) / height;

            if (!moveVector) {
                this.positionIndicators();
            }
            if (this.callback) {
                this.callback(this.hsv2rgb(this).hex);
            }
        },

        /**
         * Check if the event is inside the rect.
         * @param {Object} rect - Bounding rectangle to check against.
         * @param {MouseEvent} event - Mouse location to check.
         * @returns {bool} True if event is inside rect.
         */
        contains: function(rect, event) {
            return event.clientX >= rect.left && event.clientX <= rect.right && event.clientY >= rect.top && event.clientY <= rect.bottom;
        },

        /**
         * Enable click and drag&drop color selection.
         * @param {DOMElement} element - HSV slide element or HSV picker element.
         * @param {Function} listener - Function that will be called whenever mouse is dragged over the element with event object as argument.
         */
        addEvents: function(element, listener) {
            $.on(element, 'click', listener, false);
            this.draggie.push(new Draggabilly($.get('.cp-drag-handle', element.parentNode), { containment: true }).on('dragMove', listener).on('dragEnd', this.positionIndicators.bind(this)));
        },

        /**
         * Sets color of the picker in hsv/hex format.
         * @param {Object} hsv - Object of the form: { h: <hue>, s: <saturation>, v: <value> }.
         * @param {string} hex - String of the form: #RRGGBB.
         */
        setColor: function(hsv, hex) {
            this.h = hsv.h % 360;
            this.s = hsv.s;
            this.v = hsv.v;

            var c = this.hsv2rgb();
            this.pickerElement.style.backgroundColor = this.hsv2rgb({ h: this.h, s: 1, v: 1 }).hex;
            if (this.callback) {
                this.callback(hex || c.hex);
            }
            this.positionIndicators();
        },

        /**
         * Sets color of the picker in hex format.
         * @param {string} hex - Hex color format #RRGGBB.
         */
        setHex: function(hex) {
            this.setColor(this.hex2hsv(hex), hex);
        },

        /**
         * Helper to position indicators.
         */
        positionIndicators: function() {
            $.style(this.slideIndicator, { top: (((this.h * this.slideElement.offsetHeight) / 360) - this.slideIndicator.offsetHeight / 2) + 'px' });
            var pickerHeight = this.pickerElement.offsetHeight;
            $.style(this.pickerIndicator, {
                top: ((pickerHeight - this.v * pickerHeight) - this.pickerIndicator.offsetHeight / 2) + 'px',
                left: ((this.s * this.pickerElement.offsetWidth) - this.pickerIndicator.offsetWidth / 2) + 'px'
            });
        },

        /**
         * Destroy this object.
         */
        destroy: function() {
            $.off(this.slideElement, 'click', this.slideListener, false);
            $.off(this.pickerElement, 'click', this.pickerListener, false);
            this.draggie.forEach(function(x) { $.destroy(x); });

            this.slideElement.parentNode.removeChild(this.slideElement);
            this.pickerElement.parentNode.removeChild(this.pickerElement);
        }
    };

    return ColorPicker;
});
