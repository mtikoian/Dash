/*!
 * Wraps functionality needed to display a dashboard widget.
 */
(function(root, factory) {
    root.Widget = factory(root.m, root.$, root.Alertify, root.Table, root.DashChart, root.Draggabilly, root.Rect);
})(this, function(m, $, Alertify, Table, DashChart, Draggabilly, Rect) {
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
            this.table = null;
            this.chart = null;
            this.interval = null;
            this.isFullscreen = false;
            this.initDate = new Date();
            this.dragMargin = 0;

            if (opts.isData) {
                this.tableOpts = {
                    content: '#widgetData_' + opts.id,
                    id: 'widgetTable_' + opts.id,
                    url: opts.url,
                    requestMethod: 'POST',
                    requestParams: { Id: opts.reportId },
                    loadAllData: false,
                    editable: false,
                    itemsPerPage: opts.reportRowLimit || 10,
                    currentStartItem: 0,
                    sorting: opts.sortColumns,
                    storageFunction: $.noop,
                    width: Math.max(opts.reportWidth || 100, 100),
                    columns: opts.columns,
                    dataCallback: this.processJson.bind(this),
                    errorCallback: this.onError.bind(this),
                    displayDateFormat: opts.displayDateFormat,
                    displayCurrencyFormat: opts.displayCurrencyFormat
                };
            }

            this.render();

            var container = this.getContainer();
            this.rect = new Rect(opts.width, opts.height, opts.x, opts.y);
            this.setupDraggie(container);

            if (!opts.isData) {
                this.chart = new DashChart(container, false, this.processJson.bind(this), this.onError.bind(this));
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
            var firstRender = !parentNode;

            if (firstRender) {
                // have to create the parent node by hand first - rendering multiple views to the same parentNode with mithril causes an overwrite
                parentNode = $.createNode();
                parentNode.id = 'widget_' + this.opts.id;
                parentNode.setAttribute('data-url', this.opts.url);
                parentNode.className = buildClassList(this.opts);
                $.get('#dashboard').appendChild(parentNode);
            }

            var self = this;
            // now render the rest of the widget content
            m.mount(parentNode, {
                view: function() {
                    return [
                        m('.grid-header.columns', [
                            m('span.grid-title.col-8', self.opts.title),
                            m('span.grid-buttons.col-4.text-right', [
                                m('a.btn.btn-link.btn-refresh', { title: $.resx('refresh'), onclick: self.forceRefresh.bind(self) },
                                    m('i.dash.dash-arrows-cw')
                                ),
                                m('a.btn.btn-link.btn-fullscreen', { title: $.resx('toggleFullScreen'), onclick: self.toggleFullScreen.bind(self) },
                                    m('i.dash.dash-max')
                                ),
                                m('a.btn.btn-link.dash-ajax.fs-disabled', {
                                    href: self.opts.baseUrl + (self.opts.isData ? 'Report' : 'Chart') + '/Details/' + (self.opts.isData ? self.opts.reportId : self.opts.chartId),
                                    title: $.resx(self.opts.isData ? 'viewReport' : 'viewChart')
                                }, m('i.dash.dash-info')),
                                m('a.btn.btn-link.dash-ajax.fs-disabled', { href: self.opts.baseUrl + 'Dashboard/Edit/' + self.opts.id, title: $.resx('editWidget') },
                                    m('i.dash.dash-pencil')
                                ),
                                m('a.btn.btn-link.dash-ajax.fs-disabled', { title: $.resx('deleteWidget'), onclick: self.deleteWidget.bind(self) },
                                    m('i.dash.dash-trash')
                                )
                            ])
                        ]),
                        m('.grid-body', [
                            self.opts.isData ? m('.widget-data', { id: 'widgetData_' + self.opts.id }, m(Table, self.tableOpts)) :
                                m('.widget-chart', { id: 'widgetChart_' + self.opts.id }, [
                                    m('.chart-spinner', m('.loading.loading-lg')),
                                    m('.chart-error.d-none.pl-1',
                                        m('div', [
                                            m('p', $.resx('errorChartLoad')),
                                            m('.btn.btn-info.btn-sm', { onclick: self.refresh.bind(self) }, $.resx('tryAgain'))
                                        ])
                                    ),
                                    m('.canvas-container', m('canvas.chart-canvas.d-none'))
                                ])
                        ]),
                        m('.grid-footer', [
                            m('span.grid-updated-time', new Date().toLocaleTimeString()),
                            m('span.resizable-handle.float-right', m('i.dash.dash-corner')),
                            m('span.drag-handle.float-right', m('i.dash.dash-move'))
                        ])
                    ];
                }
            });

            if (firstRender) {
                // add our system wide events
                $.dialogs.processContent($.get('#widget_' + this.opts.id));
            }
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

            if (this.opts.isData) {
                // @todo trigger update event
                // this.table.updateLayout();
            }

            this.rect.updated = true;
        },

        deleteWidget: function() {
            var self = this;
            Alertify.confirm($.resx('areYouSure'), function() {
                $.ajax({
                    method: 'DELETE',
                    url: self.opts.baseUrl + 'Dashboard/Delete/' + self.opts.id
                }, self.destroy.bind(self));
            });
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
                $.ajax({ method: 'GET', url: this.opts.baseUrl + 'Dashboard/WidgetOptions/' + this.opts.id }, callback.bind(this));
            }
        },

        refresh: function() {
            if ($.dialogs.hasOpenDialog()) {
                // don't refresh when a dialog is open
                return;
            }

            if (this.opts.isData) {
                // @todo trigger refresh event
                //this.table.refresh();
            } else {
                this.chart.run();
            }

            var updatedAt = $.get('.grid-updated-time', this.getContainer());
            if (updatedAt) {
                updatedAt.innerText = new Date().toLocaleTimeString();
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
            //$.destroy(this.table);
            $.destroy(this.chart);
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

            if (this.opts.isData) {
                // @todo trigger update event
                // this.table.updateLayout();
            } else {
                this.chart.resize();
            }
        }
    };

    return Widget;
});
