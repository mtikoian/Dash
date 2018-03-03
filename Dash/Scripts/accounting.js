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
    /**
     * Create the local library object, to be exported or referenced globally later
     */
    var lib = {
        version: '0.4.2',
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
        // Allow function as format parameter (should return string or object):
        if ($.isFunction(format)) {
            format = format();
        }

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
        // Fails silently (need decent errors):
        value = value || 0;

        // Return the value as-is if it's already a number:
        if (typeof value === 'number') {
            return value;
        }

        // Build regex to strip out everything except digits, decimal point and minus sign:
        var regex = new RegExp('[^0-9-' + lib.settings.number.decimal + ']', ['g']),
            unformatted = parseFloat(
                ('' + value)
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
        // Clean up number:
        number = unformat(number);

        // Build options object from second param (if object) or all params, extending defaults:
        var opts = $.extend({}, lib.settings.number, parseFormat(format));

        // Clean up precision
        var usePrecision = checkPrecision(opts.precision);

        // Do some calc:
        var negative = number < 0 ? '-' : '';
        var base = parseInt(toFixed(Math.abs(number || 0), usePrecision), 10) + '';
        var mod = base.length > 3 ? base.length % 3 : 0;

        // Format the number:
        return negative + (mod ? base.substr(0, mod) + opts.thousand : '') + base.substr(mod).replace(/(\d{3})(?=\d)/g, '$1' + opts.thousand) +
            (usePrecision ? opts.decimal + toFixed(Math.abs(number), usePrecision).split('.')[1] : '');
    };

    /**
     * Format a number as currency, with comma-separated thousands and custom precision/decimal places.
     * @param {number} number - Number to format.
     * @param {string} format - Tokenized string format.
     * @returns {string} Formatted currency.
     */
    var formatMoney = function(number, format) {
        // Clean up number:
        number = unformat(number);

        // Build options object from second param (if object) or all params, extending defaults:
        var opts = $.extend({}, lib.settings.currency, parseFormat(format));

        // Check format (returns object with pos, neg and zero):
        var formats = checkCurrencyFormat(opts.format);

        // Choose which format to use for this value:
        var useFormat = number > 0 ? formats.pos : number < 0 ? formats.neg : formats.zero;

        // Return with currency symbol added:
        return useFormat.replace('%s', opts.symbol).replace('%v', formatNumber(Math.abs(number), format));
    };

    $.accounting = {
        formatMoney: formatMoney,
        formatNumber: formatNumber,
        parseFormat: parseFormat,
        unformat: unformat
    };
}(this.$));