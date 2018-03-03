/*!
 * Contains resource handling code.
 */
(function($) {
    'use strict';

    /**
     * Variable to store resource strings.
     * {Object}
     */
    var _resx = {};

    /**
     * Get/set i18n resource strings.
     * @param {string|Object} key - Key of the resource to get/set, or an object of resource strings.
     * @param {string} value - Set key to this value if provided
     * @returns {string} Returns the value of key if value is not defined.
     */
    var resx = function(key, value) {
        if (!$.isString(key)) {
            $.extend(_resx, key);
        } else if ($.isNull(value)) {
            if (_resx.hasOwnProperty(key)) {
                return _resx[key];
            } else {
                // @todo remove once i'm done migrating to the new translation set up
                console.log('Couldn\'t find translation for key `' + key + '`.');
                console.trace();
                // lets try to make the key into something cleaner
                var result = key.split('.');
                return separateWords(capitalizeFirstLetter(result[result.length - 1])).trim();
            }
        } else {
            _resx[key] = value;
        }
    };

    /**
     * Capitalize the first letter of a string.
     * @param {string} value - String to capitalize.
     * @returns {string} Capitalized string.
     */
    var capitalizeFirstLetter = function(value) {
        return value.charAt(0).toUpperCase() + value.slice(1);
    };

    /**
     * Split a camelcase string into words.
     * @param {string} value - Strint to convert to words.
     * @param {Object} options - Options object with split/separator properties.
     * @returns {string} String split into words.
     */
    var separateWords = function(value, options) {
        options = options || {};
        return value.split(options.split || /(?=[A-Z])/).join(options.separator || ' ');
    };

    /**
     * Load all resources from the server.
     */
    var body = $.get('body');
    if (body && body.hasAttribute('data-resx')) {
        $.ajax({
            method: 'GET',
            url: body.getAttribute('data-resx')
        }, function(data) {
            // parse the results into the resx object
            if (data) {
                _resx = data;
            }
            document.dispatchEvent($.events.resxLoaded);
            $.resxLoaded = true;
        }, function() {
            document.dispatchEvent($.events.resxLoaded);
            $.resxLoaded = true;
        });
    }

    $.resx = resx;
})(this.$);
