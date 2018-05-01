/*!
 * Color helper library.
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
