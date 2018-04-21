/*!
 * Contains resource handling code.
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
            document.dispatchEvent($.events.resxLoaded);
            $.resxLoaded = true;
        }, function() {
            document.dispatchEvent($.events.resxLoaded);
            $.resxLoaded = true;
        });
    }

    $.resx = resx;
})(this.$);
