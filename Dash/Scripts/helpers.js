/*!
 * Fetch wrapper and error logging.
 */
(function($, Alertify) {
    'use strict';

    /**
     * Wrap fetch with success/error handling.
     * @param {Object} options - Options to use for the request.
     * @param {Function} onSuccess - Function to handle success result.
     * @param {Function} onError - Function to handle error result.
     */
    var _ajax = function(options, onSuccess, onError) {
        options = options || {};
        options.headers = $.extend({
            'X-Requested-With': 'XMLHttpRequest'
        }, options.headers);
        if (options.token) {
            options.headers['X-XSRF-TOKEN'] = options.token;
            delete options.token;
        }

        // keep browser from caching requests by tacking milliseconds to end of url
        var url = options.url + (options.url.indexOf('?') > -1 ? '&' : '?') + '_t=' + Date.now();
        delete options.url;

        if (options.data) {
            options.headers['Content-Type'] = 'application/json';
            if (options.method === 'GET') {
                url += '&' + Object.keys(options.data).map(function(x) {
                    return encodeURIComponent(x) + '=' + encodeURIComponent(options.data[x]);
                }).join('&');
            } else {
                options.body = JSON.stringify(options.data);
            }
            delete options.data;
        }
        options.credentials = 'same-origin';

        fetch(url, options)
            .then(_checkStatus)
            .then(_parse)
            .then(function(data) {
                if (data.error) {
                    if ($.isFunction(onError)) {
                        onError(data);
                    }
                    Alertify.error(data.error);
                } else {
                    if ($.isFunction(onSuccess)) {
                        onSuccess(data);
                    }
                    if (data.message) {
                        Alertify.success(data.message);
                    }
                }
            }).catch(function(data) {
                if (url.indexOf('LogJavascriptError') > -1) {
                    return;
                }

                if (data.response && data.response.status && [400, 401, 402, 403].indexOf(data.response.status) > -1) {
                    var locationHeader = data.response.headers && data.response.headers.get('location');
                    if (locationHeader) {
                        window.location.href = locationHeader;
                    } else {
                        window.location.reload(true);
                    }
                    return;
                }

                Alertify.error((data.response && data.response.error) || 'An unhandled error occurred.');
                if ($.isFunction(onError)) {
                    onError(data.response);
                }
            });
    };

    var _requestQueue = [];

    var Request = function(options, onSuccess, onError) {
        this.options = options;
        this.onSuccess = onSuccess;
        this.onError = onError;
        this.status = 0;
    };

    Request.prototype = {
        constructor: Request,
        key: function() {
            return this.options.key;
        },
        abort: function() {
            if (this.isInProcess()) {
                this.promise.reject();
            }
            this.dequeue();
        },
        execute: function() {
            _ajax(this.options, this.success.bind(this), this.error.bind(this));
            this.status = 1;
        },
        success: function(data) {
            this.dequeue();
            if (this.onSuccess) {
                this.onSuccess(data);
            }
        },
        error: function(data) {
            this.dequeue();
            if (this.onError) {
                this.onError(data);
            }
        },
        isInProcess: function() {
            return this.status === 1;
        },
        dequeue: function() {
            // remove this from the queue and start the next request
            var self = this;
            _requestQueue = _requestQueue.filter(function(x) { return x !== self; });
            if (_requestQueue.length) {
                _requestQueue[0].execute();
            }
        }
    };

    /**
     * Queue up an ajax request. Queue prevents one user from hammering the server.
     * @param {Object} options - Options to use for the ajax request.
     * @param {Function} onSuccess - Function to handle success result.
     * @param {Function} onError - Function to handle error result.
     */
    var ajax = function(options, onSuccess, onError) {
        options.key = options.key || options.url;
        var request = new Request(options, onSuccess, onError);

        // remove requests from queue that are for this key and aren't already in process
        _requestQueue = _requestQueue.filter(function(x) {
            return x.key() !== options.key || x.isInProcess();
        });
        _requestQueue.push(request);

        if (_requestQueue.length === 1) {
            // nothing else in the queue, so execute now
            request.execute();
        }
    };


    /**
     * Log JS errors to elmah.
     * @param {string} msg - Error message.
     * @param {string} url - Error source URL.
     * @param {number} line - Line # error occurred on
     * @param {number} columnNo - Column # error occurred on
     * @param {string|string[]|null} stack - Stack trace.
     */
    var logError = function(msg, url, lineNo, columnNo, stack) {
        if ($.isNull(msg)) {
            return;
        }

        var detail = msg + ': at path \'' + (url || document.location) + '\'';
        if (!$.isNull(lineNo)) {
            detail += ' at ' + lineNo + ':' + columnNo;
        }
        if (!$.isNull(stack)) {
            detail += '\n    at ' + ($.isString(stack) ? stack : stack.join('\n    at '));
        }

        // save error message to server
        _ajax({ method: 'POST', url: '/Error/LogJavascriptError', data: { message: detail }, block: false }, null, null);
    };

    var _checkStatus = function(response) {
        if (response.status >= 200 && response.status < 300) {
            return response;
        } else {
            var error = new Error(response.statusText);
            error.response = response;
            throw error;
        }
    };

    /**
     * Deserialize response from fetch.
     * @param {Object} response - Response object
     * @returns {Object} Result object from JSON, or object with a single 'content' property if that fails.
     */
    var _parse = function(response) {
        try {
            var contentType = response && response.headers.has('content-type') ? response.headers.get('content-type') : '';
            return contentType && contentType.indexOf('application/json') > -1 ? response.json() : response.text();
        } catch (ex) {
            return response;
        }
    };

    $.ajax = ajax;
    $.logError = logError;
}(this.$, this.Alertify));

/**
 * Add a application wide error handler to log errors.
 * Outside of strict mode to prevent errors.
 * @param {string} msg - Error message.
 * @param {string} url - Error source URL.
 * @param {number} line - Line # error occurred on
 * @param {number} columnNo - Column # error occurred on
 * @param {Error|null} error - Error object if browser supports it
 */
window.onerror = function(msg, url, lineNo, columnNo, error) {
    this.$.logError(msg, url, lineNo, columnNo, error && error.stack ? error.stack : null);
};

/*!
 * Color functions.
 */
(function($) {
    'use strict';

    /**
     * Convert hex string to RGB.
     * @param {string} hex - Hex string.
     * @returns {Object} Object with RGB properties.
     */
    var hex2rgb = function(hex) {
        return { r: parseInt(hex.substr(1, 2), 16), g: parseInt(hex.substr(3, 2), 16), b: parseInt(hex.substr(5, 2), 16) };
    };

    /**
     * Convert RGB to hex string.
     * @param {Object} color Object with r/g/b properties.
     * @returns {string} hex - Hex string.
     */
    var rgb2hex = function(color) {
        var hex = [
            (color.r * 1).toString(16),
            (color.g * 1).toString(16),
            (color.b * 1).toString(16)
        ];
        return '#' + hex.map(function(x) {
            return ('00' + x.toString()).slice(-2);
        }).join('').toUpperCase();
    };

    /**
     * Converts an RGB color value to HSL. Conversion formula
     * adapted from http://en.wikipedia.org/wiki/HSL_color_space.
     * Assumes r, g, and b are contained in the set [0, 255] and
     * returns h, s, and l in the set [0, 1].
     * @param {Object} rgb - Object with r, g, and b properties.
     * @return {Number[]} The HSL representation.
     */
    var rgb2hsl = function(rgb) {
        var r = rgb.r, g = rgb.g, b = rgb.b;
        r /= 255, g /= 255, b /= 255;
        var max = Math.max(r, g, b), min = Math.min(r, g, b);
        var h, s, l = (max + min) / 2;

        if (max === min) {
            h = s = 0; // achromatic
        } else {
            var d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            switch (max) {
                case r: h = (g - b) / d + (g < b ? 6 : 0); break;
                case g: h = (b - r) / d + 2; break;
                case b: h = (r - g) / d + 4; break;
            }
            h /= 6;
        }

        return [h, s, l];
    };

    $.colors = {
        hex2rgb: hex2rgb,
        rgb2hex: rgb2hex,
        rgb2hsl: rgb2hsl,
    };
})(this.$);

/*!
 * accounting.js v0.4.2
 * Copyright 2014 Open Exchange Rates
 *
 * Freely distributable under the MIT license.
 * Portions of accounting.js are inspired or borrowed from underscore.js
 *
 * Full details and documentation:
 * http://openexchangerates.github.io/accounting.js/
 */
(function($) {
    var lib = {
        settings: {
            currency: {
                symbol: '$',		// default currency symbol is '$'
                format: '%s%v',	    // controls output: %s = symbol, %v = value (can be object, see docs)
                decimal: '.',		// decimal point separator
                thousand: ',',		// thousands separator
                precision: 2,		// decimal places
                grouping: 3		    // digit grouping (not implemented yet)
            },
            number: {
                precision: 0,		// default precision on numbers is 0
                grouping: 3,		// digit grouping (not implemented yet)
                thousand: ',',
                decimal: '.'
            }
        }
    };

    var tokens = {
        symbol: /\{s:(.?)\}/i,
        decimal: /\[d:(.?)\]/i,
        thousand: /\[t:(.?)\]/i,
        precision: /\[p:(.?)\]/i,
        value: /\{#+\}/i
    };

    /**
     * Check and normalise the value of precision (must be positive integer).
     * @param {number} val - Value of precision to validate
     * @returns {number} Positive integer value.
     */
    var checkPrecision = function(val) {
        val = Math.round(Math.abs(val));
        return isNaN(val) ? lib.settings.number.precision : val;
    };

    /**
     * Parses a format string or object and returns format obj for use in rendering.
     * @param {string|Object} format - Default (positive) format, or object containing `pos` (required), `neg` and `zero` values (or a function returning either a string or object)
     * @returns {Object} Format object with pos, neg, and zero properties.
     */
    var checkCurrencyFormat = function(format) {
        // Format can be a string, in which case `value` ("%v") must be present:
        if ($.isString(format) && format.match('%v')) {
            // Create and return positive, negative and zero formats:
            return {
                pos: format,
                neg: format.replace('-', '').replace('%v', '-%v'),
                zero: format
            };
        }
        if (!format || !format.pos || !format.pos.match('%v')) {
            // If no format, or object is missing valid positive value, use default.
            // If default is a string, casts it to an object for faster checking next time.
            var x = lib.settings.currency.format;
            if ($.isString(x)) {
                lib.settings.currency.format = x = { pos: x, neg: x.replace('%v', '-%v'), zero: x };
            }
            return x;
        }
        return format;
    };

    /**
     * Takes a format string and parses it into an object.
     * @param {string} format - Format string`
     * @returns {Object} Object with format settings.
     */
    var parseFormat = function(format) {
        if (!$.isString(format)) {
            return format || {};
        }

        var res = {}, x;
        var newFormat = format;
        if ((x = tokens.symbol.exec(newFormat)) !== null && x.length > 1) {
            res.symbol = x[1];
            newFormat = newFormat.replace(x[0], '%s');
        }
        if ((x = tokens.decimal.exec(newFormat)) !== null && x.length > 1) {
            res.decimal = x[1];
            newFormat = newFormat.replace(x[0], '#');
        }
        if ((x = tokens.thousand.exec(newFormat)) !== null && x.length > 1) {
            res.thousand = x[1];
            newFormat = newFormat.replace(x[0], '#');
        }
        if ((x = tokens.precision.exec(newFormat)) !== null && x.length > 1) {
            res.precision = x[1] * 1;
            newFormat = newFormat.replace(x[0], '#');
        }
        if ((x = tokens.value.exec(newFormat)) !== null) {
            res.format = newFormat.replace(x[0], '%v');
        } else {
            res.format = newFormat + ' %v';
        }
        return res;
    };

    /**
     * Takes a string/array of strings, removes all formatting/cruft and returns the raw float value.
     * @param {string|number} value - Value to remove formatting from.
     * @returns {number} Number with no formatting.
     */
    var unformat = function(value) {
        value = value || 0;
        if ($.isNumber(value)) {
            return value;
        }

        // Build regex to strip out everything except digits, decimal point and minus sign:
        var regex = new RegExp('[^0-9-' + lib.settings.number.decimal + ']', ['g']);
        var unformatted = parseFloat(('' + value)
            .replace(/\((.*)\)/, '-$1') // replace bracketed values with negatives
            .replace(regex, '')         // strip out any cruft
            .replace(lib.settings.number.decimal, '.')      // make sure decimal point is standard
        );

        // This will fail silently which may cause trouble, let's wait and see:
        return !isNaN(unformatted) ? unformatted : 0;
    };

    /**
     * Implementation of toFixed() that treats floats more like decimals.
     * Fixes binary rounding issues (eg. (0.615).toFixed(2) === "0.61") that present problems for accounting- and finance-related software.
     * @param {number|string} value - Number to convert
     * @param {number} precision - Number of digits after the decimal.
     * @returns {number} Formatted value.
     */
    var toFixed = function(value, precision) {
        precision = checkPrecision(precision);
        var power = Math.pow(10, precision);
        // Multiply up by precision, round accurately, then divide and use native toFixed():
        return (Math.round(unformat(value) * power) / power).toFixed(precision);
    };

    /**
     * Format a number, with comma-separated thousands and custom precision/decimal places.
     * @param {number} number - Number to format.
     * @param {string} format - Tokenized string format.
     * @returns {string} Formatted number.
     */
    var formatNumber = function(number, format) {
        number = unformat(number);
        var opts = $.extend({}, lib.settings.number, parseFormat(format));
        var usePrecision = checkPrecision(opts.precision);
        var base = parseInt(toFixed(Math.abs(number || 0), usePrecision), 10) + '';
        var mod = base.length > 3 ? base.length % 3 : 0;
        return (number < 0 ? '-' : '') + (mod ? base.substr(0, mod) + opts.thousand : '') + base.substr(mod).replace(/(\d{3})(?=\d)/g, '$1' + opts.thousand) +
            (usePrecision ? opts.decimal + toFixed(Math.abs(number), usePrecision).split('.')[1] : '');
    };

    /**
     * Format a number as currency, with comma-separated thousands and custom precision/decimal places.
     * @param {number} number - Number to format.
     * @param {string} format - Tokenized string format.
     * @returns {string} Formatted currency.
     */
    var formatMoney = function(number, format) {
        number = unformat(number);
        var opts = $.extend({}, lib.settings.currency, parseFormat(format));
        var formats = checkCurrencyFormat(opts.format);
        return (number > 0 ? formats.pos : number < 0 ? formats.neg : formats.zero)
            .replace('%s', opts.symbol).replace('%v', formatNumber(Math.abs(number), format));
    };

    $.accounting = {
        formatMoney: formatMoney,
        parseFormat: parseFormat,
        unformat: unformat
    };
}(this.$));
