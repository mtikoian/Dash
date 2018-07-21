/*!
 * Custom events.
 */
(function($) {
    'use strict';

    var events = {
        chartLoad: new CustomEvent('chartLoad'),
        chartShareLoad: new CustomEvent('chartShareLoad'),
        chartShareUnload: new CustomEvent('chartShareUnload'),
        chartUnload: new CustomEvent('chartUnload'),
        columnSelectorLoad: new CustomEvent('columnSelectorLoad'),
        dashboardLoad: new CustomEvent('dashboardLoad'),
        dashboardUnload: new CustomEvent('dashboardUnload'),
        datasetFormLoad: new CustomEvent('datasetFormLoad'),
        datasetFormUnload: new CustomEvent('datasetFormUnload'),
        layoutUpdate: new CustomEvent('layoutUpdate'),
        reportLoad: new CustomEvent('reportLoad'),
        reportUnload: new CustomEvent('reportUnload'),
        reportShareLoad: new CustomEvent('reportShareLoad'),
        reportShareUnload: new CustomEvent('reportShareUnload'),
        resxLoaded: new CustomEvent('resxLoaded'),
        tableLoad: new CustomEvent('tableLoad'),
        tableUnload: new CustomEvent('tableUnload'),
        tableDestroy: new CustomEvent('tableDestroy'),
        tableRefresh: new CustomEvent('tableRefresh')
    };

    $.events = events;
}(this.$));

/*!
 * Ajax wrapper and error logging.
 */
(function(m, $, Alertify) {
    'use strict';

    /**
     * Wrap Mithril Ajax request with success/error handling.
     * @param {Object} options - Options to use for the ajax request.
     * @param {Function} onSuccess - Function to handle success result.
     * @param {Function} onError - Function to handle error result.
     */
    var _ajax = function(options, onSuccess, onError) {
        options.headers = {
            'Content-Type': 'application/jil; charset=utf-8',
            'Accept': 'application/jil',
            'X-Requested-With': 'XMLHttpRequest'
        };
        if (options.token) {
            options.headers['X-XSRF-Token'] = options.token;
            delete options.token;
        }
        options.config = function(xhr) {
            xhr.timeout = 60000;
        };
        options.extract = function(xhr) {
            return { status: xhr.status, data: _deserialize(xhr.responseText) };
        };

        var canBlock = $.coalesce(options.block, true);
        if (canBlock) {
            //$.show(_loadingDiv);
        }

        // keep IE from caching requests by tacking milliseconds to end of url
        options.url += (options.url.indexOf('?') > -1 ? '&' : '?') + '_t=' + Date.now();

        m.request(options).then(function(response) {
            if (response.data.reload) {
                location.reload();
                return;
            }
            if (response.data.error) {
                if (canBlock) {
                    //$.hide(_loadingDiv);
                }
                if ($.isFunction(onError)) {
                    onError(response.data);
                }
                Alertify.error(response.data.error);
            } else {
                if (canBlock) {
                    //$.hide(_loadingDiv);
                }
                if ($.isFunction(onSuccess)) {
                    onSuccess(response.data);
                }
                if (response.data.message) {
                    Alertify.success(response.data.message);
                }
            }
        }).catch(function(response) {
            if (canBlock) {
                //$.hide(_loadingDiv);
            }
            if (options.url.indexOf('LogJavascriptError') > -1) {
                return;
            }
            logError(response.data);
            if ([400, 401, 402, 403].indexOf(response.status) > -1) {
                Alertify.error(($.resx && $.resx('errorAuthorization')) || 'You do not have permission to access the requested resource.');
            } else {
                Alertify.error(($.resx && $.resx('errorGeneric')) || 'An unhandled error occurred.');
            }
            if ($.isFunction(onError)) {
                onError(response.data);
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

    /**
     * Deserialize response from ajax request.
     * @param {string} data - String of data to deserialize.
     * @returns {Object} Result object from JSON, or object with a single 'content' property if that fails.
     */
    var _deserialize = function(data) {
        if ($.isNull(data) || data.length === 0) {
            return null;
        }
        try {
            return JSON.parse(data);
        } catch (e) {
            return { content: data };
        }
    };

    $.ajax = ajax;
    $.logError = logError;
}(this.m, this.$, this.Alertify));

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
 * Resource handling.
 */
(function($) {
    'use strict';

    var _resx = {};

    /**
     * Get/set i18n resource strings.
     * @param {string|Object} key - Key of the resource to get/set, or an object of resource strings.
     * @param {string} value - Set key to this value if provided
     * @returns {string} Returns the value of key, or null if key is not defined.
     */
    var resx = function(key, value) {
        if (!$.isString(key)) {
            $.extend(_resx, key);
        } else if ($.isNull(value)) {
            if (_resx.hasOwnProperty(key)) {
                return _resx[key];
            } else {
                return null;
            }
        } else {
            _resx[key] = value;
        }
    };

    var body = $.get('body');
    if (body && body.hasAttribute('data-resx')) {
        $.ajax({
            method: 'GET',
            url: body.getAttribute('data-resx')
        }, function(data) {
            if (data) {
                _resx = data;
            }
            $.dispatch(document, $.events.resxLoaded);
            $.resxLoaded = true;
        }, function() {
            $.dispatch(document, $.events.resxLoaded);
            $.resxLoaded = true;
        });
    }

    $.resx = resx;
})(this.$);

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
        formatNumber: formatNumber,
        parseFormat: parseFormat,
        unformat: unformat
    };
}(this.$));

/*!
 * Lightweight date library
 * https://github.com/taylorhakes/fecha
 */
(function($) {
    'use strict';

    var fecha = {};
    var token = /d{1,4}|M{1,4}|YY(?:YY)?|S{1,3}|Do|ZZ|([HhMsDm])\1?|[aA]|"[^"]*"|'[^']*'/g;
    var twoDigits = /\d\d?/;
    var threeDigits = /\d{3}/;
    var fourDigits = /\d{4}/;
    var word = /[0-9]*['a-z\u00A0-\u05FF\u0700-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]+|[\u0600-\u06FF/]+(\s*?[\u0600-\u06FF]+){1,2}/i;
    var literal = /\[([^]*?)\]/gm;
    var noop = function() { };

    /**
     * Abbreviate a string.
     * @param {string[]} arr - Array of strings to shorten.
     * @param {number} sLen - Max length of new strings.
     * @returns {string[]} New array of strings.
     */
    function shorten(arr, sLen) {
        var newArr = [];
        for (var i = 0, len = arr.length; i < len; i++) {
            newArr.push(arr[i].substr(0, sLen));
        }
        return newArr;
    }

    /**
     * Update months names based on i18n resource.
     * @param {string[]} arrName - Array of month names.
     * @returns {string[]} Updated array of month names.
     */
    function monthUpdate(arrName) {
        return function(d, v, i18n) {
            var index = i18n[arrName].indexOf(v.charAt(0).toUpperCase() + v.substr(1).toLowerCase());
            if (~index) {
                d.month = index;
            }
        };
    }

    /**
     * Left pad a number to length len using zeros.
     * @param {number|string} val - Value to pad.
     * @param {number} len - Length to pad number to.
     * @returns {string} Zero padded string.
     */
    function pad(val, len) {
        val = String(val);
        len = len || 2;
        while (val.length < len) {
            val = '0' + val;
        }
        return val;
    }

    var dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    var monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    var monthNamesShort = shorten(monthNames, 3);
    var dayNamesShort = shorten(dayNames, 3);
    fecha.i18n = {
        dayNamesShort: dayNamesShort,
        dayNames: dayNames,
        monthNamesShort: monthNamesShort,
        monthNames: monthNames,
        amPm: ['am', 'pm'],
        DoFn: function DoFn(D) {
            return D + ['th', 'st', 'nd', 'rd'][D % 10 > 3 ? 0 : (D - D % 10 !== 10) * D % 10];
        }
    };

    var formatFlags = {
        D: function(dateObj) {
            return dateObj.getDate();
        },
        DD: function(dateObj) {
            return pad(dateObj.getDate());
        },
        Do: function(dateObj, i18n) {
            return i18n.DoFn(dateObj.getDate());
        },
        d: function(dateObj) {
            return dateObj.getDay();
        },
        dd: function(dateObj) {
            return pad(dateObj.getDay());
        },
        ddd: function(dateObj, i18n) {
            return i18n.dayNamesShort[dateObj.getDay()];
        },
        dddd: function(dateObj, i18n) {
            return i18n.dayNames[dateObj.getDay()];
        },
        M: function(dateObj) {
            return dateObj.getMonth() + 1;
        },
        MM: function(dateObj) {
            return pad(dateObj.getMonth() + 1);
        },
        MMM: function(dateObj, i18n) {
            return i18n.monthNamesShort[dateObj.getMonth()];
        },
        MMMM: function(dateObj, i18n) {
            return i18n.monthNames[dateObj.getMonth()];
        },
        YY: function(dateObj) {
            return String(dateObj.getFullYear()).substr(2);
        },
        YYYY: function(dateObj) {
            return dateObj.getFullYear();
        },
        h: function(dateObj) {
            return dateObj.getHours() % 12 || 12;
        },
        hh: function(dateObj) {
            return pad(dateObj.getHours() % 12 || 12);
        },
        H: function(dateObj) {
            return dateObj.getHours();
        },
        HH: function(dateObj) {
            return pad(dateObj.getHours());
        },
        m: function(dateObj) {
            return dateObj.getMinutes();
        },
        mm: function(dateObj) {
            return pad(dateObj.getMinutes());
        },
        s: function(dateObj) {
            return dateObj.getSeconds();
        },
        ss: function(dateObj) {
            return pad(dateObj.getSeconds());
        },
        S: function(dateObj) {
            return Math.round(dateObj.getMilliseconds() / 100);
        },
        SS: function(dateObj) {
            return pad(Math.round(dateObj.getMilliseconds() / 10), 2);
        },
        SSS: function(dateObj) {
            return pad(dateObj.getMilliseconds(), 3);
        },
        a: function(dateObj, i18n) {
            return dateObj.getHours() < 12 ? i18n.amPm[0] : i18n.amPm[1];
        },
        A: function(dateObj, i18n) {
            return dateObj.getHours() < 12 ? i18n.amPm[0].toUpperCase() : i18n.amPm[1].toUpperCase();
        },
        ZZ: function(dateObj) {
            var o = dateObj.getTimezoneOffset();
            return (o > 0 ? '-' : '+') + pad(Math.floor(Math.abs(o) / 60) * 100 + Math.abs(o) % 60, 4);
        },
        l: function(dateObj) {
            var onejan = new Date(dateObj.getFullYear(), 0, 1);
            return Math.ceil((((dateObj - onejan) / 86400000) + onejan.getDay() + 1) / 7);
        },
        ll: function(dateObj) {
            var onejan = new Date(dateObj.getFullYear(), 0, 1);
            return pad(Math.ceil((((dateObj - onejan) / 86400000) + onejan.getDay() + 1) / 7), 2);
        },
        q: function(dateObj) {
            return (Math.ceil(dateObj.getMonth() + 1 / 3));
        }
    };

    var parseFlags = {
        D: [twoDigits, function(d, v) {
            d.day = v;
        }],
        Do: [new RegExp(twoDigits.source + word.source), function(d, v) {
            d.day = parseInt(v, 10);
        }],
        M: [twoDigits, function(d, v) {
            d.month = v - 1;
        }],
        YY: [twoDigits, function(d, v) {
            var da = new Date(), cent = +('' + da.getFullYear()).substr(0, 2);
            d.year = '' + (v > 68 ? cent - 1 : cent) + v;
        }],
        h: [twoDigits, function(d, v) {
            d.hour = v;
        }],
        m: [twoDigits, function(d, v) {
            d.minute = v;
        }],
        s: [twoDigits, function(d, v) {
            d.second = v;
        }],
        YYYY: [fourDigits, function(d, v) {
            d.year = v;
        }],
        S: [/\d/, function(d, v) {
            d.millisecond = v * 100;
        }],
        SS: [/\d{2}/, function(d, v) {
            d.millisecond = v * 10;
        }],
        SSS: [threeDigits, function(d, v) {
            d.millisecond = v;
        }],
        d: [twoDigits, noop],
        ddd: [word, noop],
        MMM: [word, monthUpdate('monthNamesShort')],
        MMMM: [word, monthUpdate('monthNames')],
        a: [word, function(d, v, i18n) {
            var val = v.toLowerCase();
            if (val === i18n.amPm[0]) {
                d.isPm = false;
            } else if (val === i18n.amPm[1]) {
                d.isPm = true;
            }
        }],
        ZZ: [/[+-]\d\d:?\d\d/, function(d, v) {
            var parts = (v + '').match(/([+-]|\d\d)/gi), minutes;

            if (parts) {
                minutes = +(parts[1] * 60) + parseInt(parts[2], 10);
                d.timezoneOffset = parts[0] === '+' ? minutes : -minutes;
            }
        }]
    };
    parseFlags.dd = parseFlags.d;
    parseFlags.dddd = parseFlags.ddd;
    parseFlags.DD = parseFlags.D;
    parseFlags.mm = parseFlags.m;
    parseFlags.hh = parseFlags.H = parseFlags.HH = parseFlags.h;
    parseFlags.MM = parseFlags.M;
    parseFlags.ss = parseFlags.s;
    parseFlags.A = parseFlags.a;

    // Some common format strings
    fecha.masks = {
        'default': 'ddd MMM DD YYYY HH:mm:ss',
        shortDate: 'M/D/YY',
        mediumDate: 'MMM D, YYYY',
        longDate: 'MMMM D, YYYY',
        fullDate: 'dddd, MMMM D, YYYY',
        shortTime: 'HH:mm',
        mediumTime: 'HH:mm:ss',
        longTime: 'HH:mm:ss.SSS'
    };

    /***
     * Format a date.
     * @method format
     * @param {Date|number} dateObj - JS date to format.
     * @param {string} mask - New format for the date, i.e. 'mm-dd-yy' or 'shortDate'.
     * @param {Object} i18nSettings - i18n resources.
     * @return {string} Formatted date string.
     */
    fecha.format = function(dateObj, mask, i18nSettings) {
        var i18n = i18nSettings || fecha.i18n;

        if (typeof dateObj === 'number') {
            dateObj = new Date(dateObj);
        }

        if (!dateObj.getMonth || isNaN(dateObj.getTime())) {
            return '';
            // throw new Error('Invalid Date in fecha.format');
        }

        mask = fecha.masks[mask] || mask || fecha.masks['default'];

        var literals = [];

        // Make literals inactive by replacing them with ??
        mask = mask.replace(literal, function($0, $1) {
            literals.push($1);
            return '??';
        });
        // Apply formatting rules
        mask = mask.replace(token, function($0) {
            return $0 in formatFlags ? formatFlags[$0](dateObj, i18n) : $0.slice(1, $0.length - 1);
        });
        // Inline literal values back into the formatted value
        return mask.replace(/\?\?/g, function() {
            return literals.shift();
        });
    };

    /**
     * Parse a date string into an object.
     * @method parse
     * @param {string} dateStr - Date string
     * @param {string} format - Date parse format
     * @param {Object} i18nSettings - i18n resources.
     * @returns {Date|boolean} JS date object or false.
     */
    fecha.parse = function(dateStr, format, i18nSettings) {
        var i18n = i18nSettings || fecha.i18n;

        if (typeof format !== 'string') {
            throw new Error('Invalid format in fecha.parse');
        }

        format = fecha.masks[format] || format;

        // Avoid regular expression denial of service, fail early for really long strings
        // https://www.owasp.org/index.php/Regular_expression_Denial_of_Service_-_ReDoS
        if (!dateStr || dateStr.length > 1000) {
            return false;
        }

        var isValid = true;
        var dateInfo = {};
        var isUtc = false;

        // Special handler for UTC. String will end in a Z but with no offset specified (ie '-0400')
        if (dateStr.indexOf('Z') === dateStr.length - 1 && dateStr.indexOf('ZZ') === -1) {
            dateStr = dateStr.substr(0, dateStr.length - 2);
            dateInfo.timezoneOffset = new Date().getTimezoneOffset();
            isUtc = true;
        }

        format.replace(token, function($0) {
            if (parseFlags[$0]) {
                var info = parseFlags[$0];
                var index = dateStr.search(info[0]);
                if (!~index) {
                    isValid = false;
                } else {
                    dateStr.replace(info[0], function(result) {
                        info[1](dateInfo, result, i18n);
                        dateStr = dateStr.substr(index + result.length);
                        return result;
                    });
                }
            }

            return parseFlags[$0] ? '' : $0.slice(1, $0.length - 1);
        });

        if (!isValid) {
            return false;
        }

        var today = new Date();
        if (dateInfo.isPm === true && typeof dateInfo.hour !== 'undefined' && +dateInfo.hour !== 12) {
            dateInfo.hour = +dateInfo.hour + 12;
        } else if (dateInfo.isPm === false && +dateInfo.hour === 12) {
            dateInfo.hour = 0;
        }

        var date;
        if (typeof dateInfo.timezoneOffset !== 'undefined') {
            if (!isUtc) {
                dateInfo.minute = +(dateInfo.minute || 0) - +dateInfo.timezoneOffset;
            }
            date = new Date(Date.UTC(dateInfo.year || today.getFullYear(), dateInfo.month || 0, dateInfo.day || 1,
                dateInfo.hour || 0, dateInfo.minute || 0, dateInfo.second || 0, dateInfo.millisecond || 0));
        } else {
            date = new Date(dateInfo.year || today.getFullYear(), dateInfo.month || 0, dateInfo.day || 1,
                dateInfo.hour || 0, dateInfo.minute || 0, dateInfo.second || 0, dateInfo.millisecond || 0);
        }
        return date;
    };

    $.fecha = fecha;
})(this.$);

/*!
 * Library for evaluating logic passed as json.
 * https://github.com/jwadhams/json-logic-js
 */
(function(root, factory) {
    root.jsonLogic = factory();
}(this.$, function() {
    'use strict';
    /* globals console:false */

    /**
     * Return an array that contains no duplicates (original not modified)
     * @param  {array} array   Original reference array
     * @return {array}         New array with no duplicates
     */
    function arrayUnique(array) {
        var a = [];
        for (var i = 0, l = array.length; i < l; i++) {
            if (a.indexOf(array[i]) === -1) {
                a.push(array[i]);
            }
        }
        return a;
    }

    var jsonLogic = {};
    var operations = {
        '==': function(a, b) {
            return a == b;
        },
        '===': function(a, b) {
            return a === b;
        },
        '!=': function(a, b) {
            return a != b;
        },
        '!==': function(a, b) {
            return a !== b;
        },
        '>': function(a, b) {
            return a > b;
        },
        '>=': function(a, b) {
            return a >= b;
        },
        '<': function(a, b, c) {
            return (c === undefined) ? a < b : (a < b) && (b < c);
        },
        '<=': function(a, b, c) {
            return (c === undefined) ? a <= b : (a <= b) && (b <= c);
        },
        '!!': function(a) {
            return jsonLogic.truthy(a);
        },
        '!': function(a) {
            return !jsonLogic.truthy(a);
        },
        '%': function(a, b) {
            return a % b;
        },
        'in': function(a, b) {
            if (!b || typeof b.indexOf === 'undefined') return false;
            return (b.indexOf(a) !== -1);
        },
        'cat': function() {
            return Array.prototype.join.call(arguments, '');
        },
        'substr': function(source, start, end) {
            if (end < 0) {
                // JavaScript doesn't support negative end, this emulates PHP behavior
                var temp = String(source).substr(start);
                return temp.substr(0, temp.length + end);
            }
            return String(source).substr(start, end);
        },
        '+': function() {
            return Array.prototype.reduce.call(arguments, function(a, b) {
                return parseFloat(a, 10) + parseFloat(b, 10);
            }, 0);
        },
        '*': function() {
            return Array.prototype.reduce.call(arguments, function(a, b) {
                return parseFloat(a, 10) * parseFloat(b, 10);
            });
        },
        '-': function(a, b) {
            if (b === undefined) {
                return -a;
            } else {
                return a - b;
            }
        },
        '/': function(a, b) {
            return a / b;
        },
        'min': function() {
            return Math.min.apply(this, arguments);
        },
        'max': function() {
            return Math.max.apply(this, arguments);
        },
        'merge': function() {
            return Array.prototype.reduce.call(arguments, function(a, b) {
                return a.concat(b);
            }, []);
        },
        'var': function(a, b) {
            var not_found = (b === undefined) ? null : b;
            var data = this;
            if (typeof a === 'undefined' || a === '' || a === null) {
                return data;
            }
            var sub_props = String(a).split('.');
            for (var i = 0; i < sub_props.length; i++) {
                if (data === null) {
                    return not_found;
                }
                // Descending into data
                data = data[sub_props[i]];
                if (data === undefined) {
                    return not_found;
                }
            }
            return data;
        },
        'missing': function() {
            /*
            Missing can receive many keys as many arguments, like {"missing:[1,2]}
            Missing can also receive *one* argument that is an array of keys,
            which typically happens if it's actually acting on the output of another command
            (like 'if' or 'merge')
            */

            var missing = [];
            var keys = Array.isArray(arguments[0]) ? arguments[0] : arguments;

            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                var value = jsonLogic.apply({ 'var': key }, this);
                if (value === null || value === '') {
                    missing.push(key);
                }
            }

            return missing;
        },
        'missing_some': function(need_count, options) {
            // missing_some takes two arguments, how many (minimum) items must be present, and an array of keys (just like 'missing') to check for presence.
            var are_missing = jsonLogic.apply({ 'missing': options }, this);

            if (options.length - are_missing.length >= need_count) {
                return [];
            } else {
                return are_missing;
            }
        },
        'method': function(obj, method, args) {
            return obj[method].apply(obj, args);
        },

    };

    jsonLogic.is_logic = function(logic) {
        return (
            typeof logic === 'object' && // An object
            logic !== null && // but not null
            !Array.isArray(logic) && // and not an array
            Object.keys(logic).length === 1 // with exactly one key
        );
    };

    /*
    This helper will defer to the JsonLogic spec as a tie-breaker when different language interpreters define different behavior for the truthiness of primitives.  E.g., PHP considers empty arrays to be falsy, but Javascript considers them to be truthy. JsonLogic, as an ecosystem, needs one consistent answer.
    Spec and rationale here: http://jsonlogic.com/truthy
    */
    jsonLogic.truthy = function(value) {
        if (Array.isArray(value) && value.length === 0) {
            return false;
        }
        return !!value;
    };


    jsonLogic.get_operator = function(logic) {
        return Object.keys(logic)[0];
    };

    jsonLogic.get_values = function(logic) {
        return logic[jsonLogic.get_operator(logic)];
    };

    jsonLogic.apply = function(logic, data) {
        // Does this array contain logic? Only one way to find out.
        if (Array.isArray(logic)) {
            return logic.map(function(l) {
                return jsonLogic.apply(l, data);
            });
        }
        // You've recursed to a primitive, stop!
        if (!jsonLogic.is_logic(logic)) {
            return logic;
        }

        data = data || {};

        var op = jsonLogic.get_operator(logic);
        var values = logic[op];
        var i;
        var current;
        var scopedLogic, scopedData, filtered, initial;

        // easy syntax for unary operators, like {"var" : "x"} instead of strict {"var" : ["x"]}
        if (!Array.isArray(values)) {
            values = [values];
        }

        // 'if', 'and', and 'or' violate the normal rule of depth-first calculating consequents, let each manage recursion as needed.
        if (op === 'if' || op == '?:') {
            /* 'if' should be called with a odd number of parameters, 3 or greater
            This works on the pattern:
            if( 0 ){ 1 }else{ 2 };
            if( 0 ){ 1 }else if( 2 ){ 3 }else{ 4 };
            if( 0 ){ 1 }else if( 2 ){ 3 }else if( 4 ){ 5 }else{ 6 };
            The implementation is:
            For pairs of values (0,1 then 2,3 then 4,5 etc)
            If the first evaluates truthy, evaluate and return the second
            If the first evaluates falsy, jump to the next pair (e.g, 0,1 to 2,3)
            given one parameter, evaluate and return it. (it's an Else and all the If/ElseIf were false)
            given 0 parameters, return NULL (not great practice, but there was no Else)
            */
            for (i = 0; i < values.length - 1; i += 2) {
                if (jsonLogic.truthy(jsonLogic.apply(values[i], data))) {
                    return jsonLogic.apply(values[i + 1], data);
                }
            }
            if (values.length === i + 1) return jsonLogic.apply(values[i], data);
            return null;
        } else if (op === 'and') { // Return first falsy, or last
            for (i = 0; i < values.length; i += 1) {
                current = jsonLogic.apply(values[i], data);
                if (!jsonLogic.truthy(current)) {
                    return current;
                }
            }
            return current; // Last
        } else if (op === 'or') {// Return first truthy, or last
            for (i = 0; i < values.length; i += 1) {
                current = jsonLogic.apply(values[i], data);
                if (jsonLogic.truthy(current)) {
                    return current;
                }
            }
            return current; // Last
        } else if (op === 'filter') {
            scopedData = jsonLogic.apply(values[0], data);
            scopedLogic = values[1];

            if (!Array.isArray(scopedData)) {
                return [];
            }
            // Return only the elements from the array in the first argument,
            // that return truthy when passed to the logic in the second argument.
            // For parity with JavaScript, reindex the returned array
            return scopedData.filter(function(datum) {
                return jsonLogic.truthy(jsonLogic.apply(scopedLogic, datum));
            });
        } else if (op === 'map') {
            scopedData = jsonLogic.apply(values[0], data);
            scopedLogic = values[1];

            if (!Array.isArray(scopedData)) {
                return [];
            }

            return scopedData.map(function(datum) {
                return jsonLogic.apply(scopedLogic, datum);
            });

        } else if (op === 'reduce') {
            scopedData = jsonLogic.apply(values[0], data);
            scopedLogic = values[1];
            initial = typeof values[2] !== 'undefined' ? values[2] : null;

            if (!Array.isArray(scopedData)) {
                return initial;
            }

            return scopedData.reduce(
                function(accumulator, current) {
                    return jsonLogic.apply(
                        scopedLogic,
                        { 'current': current, 'accumulator': accumulator }
                    );
                },
                initial
            );

        } else if (op === 'all') {
            scopedData = jsonLogic.apply(values[0], data);
            scopedLogic = values[1];
            // All of an empty set is false. Note, some and none have correct fallback after the for loop
            if (!scopedData.length) {
                return false;
            }
            for (i = 0; i < scopedData.length; i += 1) {
                if (!jsonLogic.truthy(jsonLogic.apply(scopedLogic, scopedData[i]))) {
                    return false; // First falsy, short circuit
                }
            }
            return true; // All were truthy
        } else if (op === 'none') {
            filtered = jsonLogic.apply({ 'filter': values }, data);
            return filtered.length === 0;

        } else if (op === 'some') {
            filtered = jsonLogic.apply({ 'filter': values }, data);
            return filtered.length > 0;
        }

        // Everyone else gets immediate depth-first recursion
        values = values.map(function(val) {
            return jsonLogic.apply(val, data);
        });

        // The operation is called with "data" bound to its "this" and "values" passed as arguments.
        // Structured commands like % or > can name formal arguments while flexible commands (like missing or merge) can operate on the pseudo-array arguments
        // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Functions/arguments
        if (typeof operations[op] === 'function') {
            return operations[op].apply(data, values);
        } else if (op.indexOf('.') > 0) { // Contains a dot, and not in the 0th position
            var sub_ops = String(op).split('.');
            var operation = operations;
            for (i = 0; i < sub_ops.length; i++) {
                // Descending into operations
                operation = operation[sub_ops[i]];
                if (operation === undefined) {
                    throw new Error('Unrecognized operation ' + op + ' (failed at ' + sub_ops.slice(0, i + 1).join('.') + ')');
                }
            }

            return operation.apply(data, values);
        }

        throw new Error('Unrecognized operation ' + op);
    };

    jsonLogic.uses_data = function(logic) {
        var collection = [];

        if (jsonLogic.is_logic(logic)) {
            var op = jsonLogic.get_operator(logic);
            var values = logic[op];

            if (!Array.isArray(values)) {
                values = [values];
            }

            if (op === 'var') {
                // This doesn't cover the case where the arg to var is itself a rule.
                collection.push(values[0]);
            } else {
                // Recursion!
                values.map(function(val) {
                    collection.push.apply(collection, jsonLogic.uses_data(val));
                });
            }
        }

        return arrayUnique(collection);
    };

    jsonLogic.add_operation = function(name, code) {
        operations[name] = code;
    };

    jsonLogic.rm_operation = function(name) {
        delete operations[name];
    };

    jsonLogic.rule_like = function(rule, pattern) {
        if (pattern === rule) {
            return true;
        }
        if (pattern === '@') {
            return true;
        } // Wildcard!
        if (pattern === 'number') {
            return (typeof rule === 'number');
        }
        if (pattern === 'string') {
            return (typeof rule === 'string');
        }
        if (pattern === 'array') {
            // !logic test might be superfluous in JavaScript
            return Array.isArray(rule) && !jsonLogic.is_logic(rule);
        }

        if (jsonLogic.is_logic(pattern)) {
            if (jsonLogic.is_logic(rule)) {
                var pattern_op = jsonLogic.get_operator(pattern);
                var rule_op = jsonLogic.get_operator(rule);

                if (pattern_op === '@' || pattern_op === rule_op) {
                    // echo "\nOperators match, go deeper\n";
                    return jsonLogic.rule_like(
                        jsonLogic.get_values(rule, false),
                        jsonLogic.get_values(pattern, false)
                    );
                }
            }
            return false; // pattern is logic, rule isn't, can't be eq
        }

        if (Array.isArray(pattern)) {
            if (Array.isArray(rule)) {
                if (pattern.length !== rule.length) {
                    return false;
                }
                /*
                  Note, array order MATTERS, because we're using this array test logic to consider arguments, where order can matter. (e.g., + is commutative, but '-' or 'if' or 'var' are NOT)
                */
                for (var i = 0; i < pattern.length; i += 1) {
                    // If any fail, we fail
                    if (!jsonLogic.rule_like(rule[i], pattern[i])) {
                        return false;
                    }
                }
                return true; // If they *all* passed, we pass
            } else {
                return false; // Pattern is array, rule isn't
            }
        }

        // Not logic, not array, not a === match for rule.
        return false;
    };

    return jsonLogic;
}));
