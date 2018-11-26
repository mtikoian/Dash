/*!
 * Wraps functionality needed to display a dashboard widget.
 */
(function(root, factory) {
    root.Widget = factory(root.$, root.DashChart, root.Draggabilly, root.Rect);
})(this, function($, DashChart, Draggabilly, Rect) {
    'use strict';

    var _columns = 20;
    var _rows = 20;

    /**
     * Use object properties to make class list for a widget container.
     * @param {Object} obj - Widget properties.
     * @returns {string} CSS class list.
     */
    var buildClassList = function(obj) {
        return 'grid-item grid-item-width-' + obj.width + ' grid-item-height-' + obj.height +
            ' grid-item-x-' + obj.x + ' grid-item-y-' + obj.y;
    };

    /**
     * Sort widgets from top left to bottom right.
     * @param {Widget} a - First widget to compare.
     * @param {Widget} b - Second widget to compare.
     * @returns {number} Negative number if a comes first, positive number if b, zero if equal.
     */
    var rectSort = function(a, b) {
        if (a.rect.y === b.rect.y && a.rect.x === b.rect.x) {
            return a.rect.updated ? -1 : b.rect.updated ? 1 : 0;
        }
        if (a.rect.x === b.rect.x) {
            return a.rect.y - b.rect.y;
        }
        return a.rect.x - b.rect.x;
    };

    /**
     * Declare Widget class.
     * @param {Node} widgetNode - DOM node that contains the widget.
     */
    var Widget = function(widgetNode) {
        this.init(widgetNode);
    };

    Widget.prototype = {
        /**
         * Initialize the widget.
         * @param {Node} widgetNode - DOM node that contains the widget.
         */
        init: function(widgetNode) {
            // attach this to the container for reference in the dashboard
            widgetNode.widget = this;

            this.opts = {
                id: widgetNode.id,
                dashboardId: 'bodyContent'
            };
            this.chart = null;
            this.interval = null;
            this.isFullscreen = false;
            this.dragMargin = 0;

            $.on($.get('.btn-refresh', widgetNode), 'click', this.forceRefresh.bind(this));
            $.on($.get('.btn-fullscreen', widgetNode), 'click', this.toggleFullScreen.bind(this));
            this.rect = new Rect(widgetNode);
            this.setupDraggie(widgetNode);

            var chartNode = $.get('.widget-chart', widgetNode);
            if (chartNode) {
                this.chart = new DashChart(chartNode, false);
            }
            var refreshSeconds = this.getRefreshRate();
            if (refreshSeconds > 0) {
                this.interval = setInterval(this.refresh.bind(this), refreshSeconds * 1000);
            }
        },

        /**
         * Get the container element for the widget.
         * @returns {Node} DOM node that contains the widget content.
         */
        getContainer: function() {
            return $.get('#' + this.opts.id);
        },

        /**
         * Get the container element for the dashboard.
         * @returns {Node} DOM node that contains the widget content.
         */
        getDashboardContainer: function() {
            return $.get('#' + this.opts.dashboardId);
        },

        /**
         * Get the refresh rate in seconds for the widget.
         * @returns {Number} Refresh rate in seconds. Zero means no refresh.
         */
        getRefreshRate: function() {
            var container = this.getContainer();
            return container.hasAttribute('data-refresh') ? container.getAttribute('data-refresh') * 1 : 0;
        },

        makeGrid: function() {
            var dashboardNode = this.getDashboardContainer();
            return { columns: _columns, rows: _rows, columnWidth: dashboardNode.parentNode.offsetWidth / _columns, rowHeight: dashboardNode.parentNode.offsetHeight / _rows };
        },

        /**
         * Get the widget objects for the dashboard.
         * @returns {Widget[]} Array of widgets.
         */
        getWidgets: function() {
            return $.getAll('.grid-item').map(function(x) { return x.widget; });
        },

        /**
         * Update widget position to avoid collisions after a resize or drag.
         */
        updatePosition: function() {
            var sorted = this.getWidgets();
            sorted.sort(rectSort);

            var l = sorted.length, aWidget, bWidget;
            for (var i = 0; i < l; i++) {
                aWidget = sorted[i];
                aWidget.rect.updated = false;

                for (var j = 0; j < l; j++) {
                    if (i === j) {
                        continue;
                    }

                    bWidget = sorted[j];
                    if (bWidget.rect.overlaps(aWidget.rect)) {
                        if (bWidget.rect.y > aWidget.rect.y) {
                            // need to move down
                            bWidget.setLocation(bWidget.rect.x, aWidget.rect.y + aWidget.rect.height);
                        } else if (aWidget.rect.x + aWidget.rect.width + bWidget.rect.width > _columns) {
                            // need to move down
                            bWidget.setLocation(bWidget.rect.x, aWidget.rect.y + aWidget.rect.height);
                        } else {
                            // safe to move right
                            bWidget.setLocation(aWidget.rect.x + aWidget.rect.width, bWidget.rect.y);
                        }
                    }
                }
            }
            this.savePosition();
        },

        /**
         * Save position settings back to server.
         */
        savePosition: function() {
            var positions = this.getWidgets().map(function(w) {
                return {
                    Id: w.opts.id || 0,
                    Width: w.rect.width || 1,
                    Height: w.rect.height || 1,
                    X: w.rect.x || 0,
                    Y: w.rect.y || 0
                };
            });

            var dash = this.getDashboardContainer();
            $.ajax({
                method: 'POST',
                url: dash.getAttribute('data-save-url'),
                data: { Widgets: positions },
                block: false
            }, null);
        },

        /**
         * Add the draggabilly handlers.
         * @param {Node} container - DOM node that contains the widget content.
         * @param {Object} grid - Object that contains the grid columnWidth and rowHeight.
         */
        setupDraggie: function(container, grid) {
            container = $.coalesce(container, this.getContainer());
            var g = this.opts.grid = $.coalesce(grid, this.makeGrid());

            $.destroy(this.moveDraggie);
            this.moveDraggie = new Draggabilly(container, { handle: '.drag-handle', grid: [g.columnWidth, g.rowHeight], minZero: true }).on('dragEnd', this.stopDrag.bind(this));

            $.destroy(this.resizeDraggie);
            var handle = $.get('.resizable-handle', container);
            this.resizeDraggie = new Draggabilly(handle, { grid: [g.columnWidth, g.rowHeight] }).on('dragStart', this.initResize.bind(this)).on('dragEnd', this.stopResize.bind(this));

            var style = handle.currentStyle || window.getComputedStyle(handle);
            this.dragMargin = style.marginRight.replace('px', '') * 1;
        },

        /**
         * Stop dragging the widget and updates its location.
         */
        stopDrag: function() {
            var x = Math.max(Math.round(this.moveDraggie.position.x / this.opts.grid.columnWidth), 0);
            var y = Math.max(Math.round(this.moveDraggie.position.y / this.opts.grid.rowHeight), 0);
            if (x + this.rect.width > this.opts.grid.columns) {
                x = this.opts.grid.columns - this.rect.width;
            }
            if (y + this.rect.height > this.opts.grid.rows) {
                y = this.opts.grid.rows - this.rect.height;
            }
            this.setLocation(x, y);
            this.rect.updated = true;
            this.updatePosition();
        },

        /**
         * Start resizing a widget
         * @param {Event} event - Original mousedown or touchstart event
         */
        initResize: function(event) {
            // clear any selection so browser doesn't think we are dragging the selection
            window.getSelection().removeAllRanges();

            var container = this.getContainer();
            container.style['z-index'] = 9999;
            var pos = event.changedTouches ? event.changedTouches[0] : event;
            this.x = pos.clientX;
            this.y = pos.clientY;

            var styles = document.defaultView.getComputedStyle(container);
            this.width = styles.width.replace('px', '') * 1;
            this.height = styles.height.replace('px', '') * 1;

            if (event.target !== event.currentTarget) {
                event.stopPropagation();
            }
        },

        /**
         * Update the widget after the user finishes resizing.
         */
        stopResize: function() {
            var container = this.getContainer();
            var w = Math.max(this.width + this.resizeDraggie.position.x + this.dragMargin, this.opts.grid.columnWidth * 4);
            var h = Math.max(this.height + this.resizeDraggie.position.y, this.opts.grid.rowHeight * 4);

            this.setSize(Math.min(Math.round(w / this.opts.grid.columnWidth), this.opts.grid.columns), Math.min(Math.round(h / this.opts.grid.rowHeight), this.opts.grid.rows));

            var handle = $.get('.resizable-handle', container);
            if (handle) {
                handle.removeAttribute('style');
            }

            this.savePosition();
            this.updateLayout();
            this.rect.updated = true;
        },

        refresh: function() {
            var container = this.getContainer();
            var table = $.get('[data-toggle="dotable"]', container);
            if (table && table.doTable) {
                table.doTable.refresh();
            } else {
                this.chart.run();
            }
            $.setText($.get('.grid-updated-time', container), new Date().toLocaleTimeString());
        },

        updateLayout: function() {
            // @todo de-duplicate this code
            var event;
            if (typeof (Event) === 'function') {
                event = new Event('resize');
            } else {
                event = document.createEvent('Event');
                event.initEvent('resize', true, true);
            }
            $.dispatch(window, event);
        },

        forceRefresh: function() {
            var refreshSeconds = this.getRefreshRate();
            if (refreshSeconds > 0) {
                clearInterval(this.interval);
                this.interval = setInterval(this.refresh.bind(this), refreshSeconds * 1000);
            }
            this.refresh();
        },

        /**
         * Refresh the style based on the dimensions.
         * @param {number} width - New width of the widget.
         * @param {number} height - New height of the widget.
         */
        setSize: function(width, height) {
            this.rect.width = width;
            this.rect.height = height;
            this.updateStyle();
        },

        /**
         * Refresh the style based on the dimensions.
         * @param {number} x - New x coordinate of the widget.
         * @param {number} y - New y coordinate of the widget.
         */
        setLocation: function(x, y) {
            this.rect.x = x;
            this.rect.y = y;
            this.updateStyle();
        },

        /**
        * Refresh the style based on the dimensions.
        */
        updateStyle: function() {
            var container = this.getContainer();
            container.className = buildClassList(this.rect);
            container.removeAttribute('style');
        },

        /**
         * Destroy the widget.
         * @param {bool} totalDestruction - Remove the container node and null out the widget object if true.
         */
        destroy: function(totalDestruction) {
            var table = $.get('[data-toggle="dotable"]', this.getContainer());
            if (table && table.doTable) {
                table.doTable.destroy();
            } else {
                $.destroy(this.chart);
            }

            $.destroy(this.moveDraggie);
            $.destroy(this.resizeDraggie);

            if (this.interval) {
                clearInterval(this.interval);
                this.interval = null;
            }

            if ($.coalesce(totalDestruction, true)) {
                var container = this.getContainer();
                container.widget = null;
                container.parentNode.removeChild(container);
            }
        },

        toggleFullScreen: function() {
            var container = this.getContainer();
            var fullScreenIcon = $.get('.btn-fullscreen i', container);
            $.toggleClass(container, 'full-screen', !this.isFullscreen);
            $.toggleClass(fullScreenIcon, 'dash-min', !this.isFullscreen);
            $.toggleClass(fullScreenIcon, 'dash-max', this.isFullscreen);
            var isFullscreen = this.isFullscreen;
            $.getAll('.fs-disabled', container).forEach(function(x) { $.toggleClass(x, 'disabled', !isFullscreen); });
            this.isFullscreen = !this.isFullscreen;
            this.updateLayout();
        }
    };

    return Widget;
});
