/*!
 * Wraps functionality needed to display a dashboard widget.
 */
(function(root, factory) {
    root.Widget = factory(root.$, root.Alertify, root.DashChart, root.Draggabilly, root.Rect);
})(this, function($, Alertify, DashChart, Draggabilly, Rect) {
    'use strict';

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
     * Declare Widget class.
     * @param {Object} opts - Widget settings
     */
    var Widget = function(opts) {
        this.init(opts);

        // attach this to the container for reference in the dashboard
        var container = this.getContainer();
        container.widget = this;
    };

    Widget.prototype = {
        /**
         * Initialize the widget.
         * @param {Object} opts - Widget settings
         */
        init: function(opts) {
            opts.isData = $.coalesce(opts.isData, true);
            opts.refreshSeconds = $.coalesce(opts.refreshSeconds, 0);
            opts.baseUrl = $.get('body').getAttribute('data-base-url');
            this.opts = opts;

            this.id = opts.id;
            this.chart = null;
            this.interval = null;
            this.isFullscreen = false;
            this.initDate = new Date();
            this.dragMargin = 0;

            this.render();

            var container = this.getContainer();
            this.rect = new Rect(opts.width, opts.height, opts.x, opts.y);
            this.setupDraggie(container);

            if (!opts.isData) {
                this.chart = new DashChart($.get('.widget-chart', container), false, this.processJson.bind(this), this.onError.bind(this));
            }
            if (opts.refreshSeconds > 0) {
                this.interval = setInterval(this.refresh.bind(this), opts.refreshSeconds * 1000);
            }

            if (opts.title) {
                $.setText($.get('.grid-title', container), opts.title);
            }
        },

        render: function() {
            var parentNode = $.get('#widget_' + this.opts.id);

            $.on($.get('.btn-refresh', parentNode), 'click', this.forceRefresh.bind(this));
            $.on($.get('.btn-fullscreen', parentNode), 'click', this.toggleFullScreen.bind(this));
        },

        /**
         * Get the container element for the widget.
         * @returns {Node} DOM node that contains the widget content.
         */
        getContainer: function() {
            return $.get('#widget_' + this.opts.id);
        },

        /**
         * Add the draggabilly handlers.
         * @param {Node} container - DOM node that contains the widget content.
         * @param {Object} grid - Object that contains the grid columnWidth and rowHeight.
         */
        setupDraggie: function(container, grid) {
            container = $.coalesce(container, this.getContainer());
            var g = this.opts.grid = $.coalesce(grid, this.opts.grid);

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
            this.opts.layoutCallback();
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

            this.opts.layoutCallback();
            this.updateLayout();
            this.rect.updated = true;
        },

        /**
         * Handle the result of a query for report data.
         * @param {Object} json - Data to display in the widget.
         * @returns {bool} True if the json data is valid
         */
        processJson: function(json) {
            if (json.updatedDate && this.initDate) {
                var updateDate = new Date(json.updatedDate);
                if (updateDate && updateDate > this.initDate) {
                    this.reload();
                    return false;
                }
            }
            if ((this.opts.isData && $.isNull(json.rows)) || (!this.opts.isData && ($.isNull(json.ranges) || json.ranges.length === 0))) {
                Alertify.error($.resx('errorConnectingToDataSource'));
                return false;
            }
            return true;
        },

        /**
         * Reload the widget options and reinitialize.
         * @param {bool} showMsg - If true show the widget reloaded message to the user.
         * @param {Object} options - Options to use to reload the widget instead of requesting from the server.
         */
        reload: function(showMsg, options) {
            var callback = function(opts) {
                this.destroy(false);
                this.init($.extend(this.opts, opts));
                if ($.coalesce(showMsg, true)) {
                    Alertify.success($.resx('widgetReloaded').replace('{0}', this.opts.title));
                }
                return;
            };

            if (!$.isNull(options)) {
                callback.call(this, options);
            } else {
                $.ajax({
                    method: 'GET', url: this.opts.baseUrl + 'Dashboard/WidgetOptions/' + this.opts.id,
                    headers: {
                        'Content-Type': 'application/jil; charset=utf-8',
                        'Accept': 'application/jil'
                    }
                }, callback.bind(this));
            }
        },

        refresh: function() {
            if (this.opts.isData) {
                var table = $.get('[data-toggle="dotable"]', this.getContainer());
                if (table && table.doTable) {
                    table.doTable.refresh();
                }
            } else {
                this.chart.run();
            }
            $.setText($.get('.grid-updated-time', this.getContainer()), new Date().toLocaleTimeString());
        },

        updateLayout: function() {
            if (this.opts.isData) {
                var table = $.get('[data-toggle="dotable"]', this.getContainer());
                if (table && table.doTable) {
                    table.doTable.updateLayout();
                }
            } else {
                this.chart.resize();
            }
        },

        forceRefresh: function() {
            if (this.opts.refreshSeconds > 0) {
                clearInterval(this.interval);
                this.interval = setInterval(this.refresh.bind(this), this.opts.refreshSeconds * 1000);
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
         * Handle an error.
         * @returns {bool} Always returns true.
         */
        onError: function() {
            if (this.interval) {
                clearInterval(this.interval);
            }
            return true;
        },

        /**
         * Destroy the widget.
         * @param {bool} totalDestruction - Remove the container node and null out the widget object if true.
         */
        destroy: function(totalDestruction) {
            if (this.opts.isData) {
                var table = $.get('[data-toggle="dotable"]', this.getContainer());
                if (table && table.doTable) {
                    table.doTable.destroy();
                }
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
