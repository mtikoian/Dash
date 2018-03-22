/*!
 * Library for storing widget positions and detecting collisions.
 */
(function(root, factory) {
    // Assume a traditional browser.
    root.Rect = factory(root.$);
})(this, function($) {
    'use strict';

    /**
     * Constructor for a rect.
     * @param {Number} width
     * @param {Number} height
     * @param {Number} x
     * @param {Number} y
     */
    function Rect(width, height, x, y) {
        this.x = $.coalesce(x, 0);
        this.y = $.coalesce(y, 0);
        this.width = $.coalesce(width, 0);
        this.height = $.coalesce(height, 0);
        this.updated = false;
    }

    Rect.prototype = {
        /**
         * Check if rect intersects with this rect.
         * @param {Rect} rect - Rect to check for intersection with.
         * @returns {bool} True if the rectangles collide, else false.
         */
        overlaps: function(rect) {
            var thisRight = this.x + this.width;
            var thisBottom = this.y + this.height;
            var rectRight = rect.x + rect.width;
            var rectBottom = rect.y + rect.height;

            // http://stackoverflow.com/a/306332
            return this.x < rectRight && thisRight > rect.x && this.y < rectBottom && thisBottom > rect.y;
        }
    };

    return Rect;
});
