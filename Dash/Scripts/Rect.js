/*!
 * Library for storing widget positions and detecting collisions.
 */
(function(root, factory) {
    root.Rect = factory(root.$);
})(this, function($) {
    'use strict';

    /**
     * Constructor for a rect.
     * @param {Node} node - Node to build the rect for.
     */
    function Rect(node) {
        var classList = [].slice.call(node.classList);
        var opts = {};
        classList.forEach(function(x) {
            var lx = x.toLowerCase();
            if (lx.indexOf('grid-item-x-') === 0)
                this.x = x.replace('grid-item-x-', '') * 1;
            else if (lx.indexOf('grid-item-y-') === 0)
                this.y = x.replace('grid-item-y-', '') * 1;
            else if (lx.indexOf('grid-item-width-') === 0)
                this.width = x.replace('grid-item-width-', '') * 1;
            else if (lx.indexOf('grid-item-height-') === 0)
                this.height = x.replace('grid-item-height-', '') * 1;
        }, opts);

        this.x = $.coalesce(opts.x, 0);
        this.y = $.coalesce(opts.y, 0);
        this.width = $.coalesce(opts.width, 0);
        this.height = $.coalesce(opts.height, 0);
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
