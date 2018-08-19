/*!
 * Mithril based table component. Supports ajax data, searching, sorting, paging, & resizing columns.
 */
(function(root, factory) {
    root.Table = factory(root.m, root.$, root.pjax, root.Alertify);
})(this, function(m, $, pjax, Alertify) {
    'use strict';

    var Table = {
        pageOptions: [m('option', { value: '10' }, '10'), m('option', { value: '20' }, '20'), m('option', { value: '50' }, '50'), m('option', { value: '100' }, '100')],

        /**
         * Get/set persistent values.
         * @param {string} key - Key name of the value to get/set.
         * @param {*} value - Value to set.
         * @returns {string|undefined} Value if getting, else undefined.
         */
        store: function(key, value) {
            if (!this.opts.storeSettings) {
                return;
            }
            var myKey = this.opts.id + '.' + key;
            // getter
            if ($.isUndefined(value)) {
                return $.isNull(this.opts.storageFunction) ? localStorage[myKey] : $.coalesce(this.opts[key], null);
            }

            // setter
            if ($.isNull(this.opts.storageFunction)) {
                localStorage[myKey] = value;
            } else {
                // ignore the param actually passed and use the values from the object
                // passing the whole object lets the storage function decide when to actually save using a debounce
                if ($.isFunction(this.opts.storageFunction)) {
                    this.opts.storageFunction({
                        itemsPerPage: this.itemsPerPage,
                        currentStartItem: this.currentStartItem,
                        searchQuery: this.searchQuery,
                        width: this.width,
                        sorting: this.sorting,
                        columnWidths: $.map(this.opts.columns, function(c) { return { field: c.field, width: c.width * 1.0 }; })
                    });
                }
            }
        },

        /**
         * Process the data array result from the ajax request.
         * @param {Object[]} data - Array of records to display.
         */
        processData: function(data) {
            if (this.opts.dataCallback) {
                this.opts.dataCallback(data);
            }

            var i = 0, len = data.rows.length, j = 0;
            for (; i < len; i++) {
                // add an index to the data so we can reset to the default sort order later if the user wants
                data.rows[i]._index = i;

                var x;
                // convert input to appropriate types
                for (j = 0; j < this.intColumns.length; j++) {
                    x = this.intColumns[j];
                    data.rows[i][x] = $.isNull(data.rows[i][x]) ? null : data.rows[i][x] * 1;
                }
                for (j = 0; j < this.dateColumns.length; j++) {
                    x = this.dateColumns[j];
                    data.rows[i][x] = $.isNull(data.rows[i][x]) ? null : $.fecha.parse(data.rows[i][x], this.opts.dataDateFormat);
                }
                for (j = 0; j < this.currencyColumns.length; j++) {
                    x = this.currencyColumns[j];
                    data.rows[i][x] = $.isNull(data.rows[i][x]) ? null : $.accounting.unformat(data.rows[i][x]);
                }
            }
            this.data = data.rows;
            this.filteredTotal = data.filteredTotal;

            this.loading = false;
            this.sort(false);
            this.filterResults();
            // redraw is needed because $.ajax uses fetch which is async
            m.redraw();
        },

        /**
         * Load the data to populate the table.
         */
        loadData: function() {
            this.loading = true;
            this.loadingError = false;

            var self = this;
            $.ajax({
                method: this.opts.requestMethod,
                url: this.opts.url,
                data: this.buildParams(),
                block: false,
                headers: {
                    'Content-Type': 'application/jil; charset=utf-8',
                    'Accept': 'application/jil'
                }
            }, this.processData.bind(this), function(data) {
                self.loading = false;
                self.loadingError = true;
                if (self.opts.errorCallback) {
                    self.opts.errorCallback(data);
                }
                // redraw is needed because $.ajax uses fetch which is async
                m.redraw();
            });
        },

        /**
         * Force the table to refresh its data.
         */
        refresh: function() {
            this.loading = true;
            this.loadingError = false;
            this.loadData();
        },

        /**
         * Build querystring params to fetch data from the server.
         * @returns {Object} Request parameters.
         */
        buildParams: function() {
            var sort = this.sorting.length > 0 ? $.map(this.sorting, function(obj, i) { return { field: obj.field, dir: obj.dir, index: i }; }) : null;
            if (this.opts.requestUsePascalCase) {
                return $.extend(this.opts.requestParams, {
                    StartItem: this.currentStartItem,
                    Items: this.itemsPerPage,
                    Query: this.searchQuery,
                    Sort: $.toPascalKeys(sort)
                });
            }
            return $.extend(this.opts.requestParams, {
                startItem: this.currentStartItem,
                items: this.itemsPerPage,
                query: this.searchQuery,
                sort: sort
            });
        },

        /**
         * Set the first item index to display.
         * @param {type} index - Record index to start on.
         */
        setCurrentStartItem: function(index) {
            this.currentStartItem = index;
            this.store('currentStartItem', index);
            this.filterResults(true);
        },

        /**
         * Set the number of items to display per page.
         * @param {number|Event} e - Number or items per page, or an event that triggered the change.
         */
        setItemsPerPage: function(e) {
            if (this.loading) {
                return;
            }

            var items = (isNaN(e) ? e.target.value : e) * 1;
            if (this.itemsPerPage !== items) {
                this.itemsPerPage = items;
                this.store('itemsPerPage', items);
                this.setCurrentStartItem(0);
            }
        },

        /**
         * Set the search query for filtering table data.
         * @param {string} val - New search text.
         */
        setSearchQuery: function(val) {
            if (this.loading) {
                return;
            }

            var query = val.target ? val.target.value : val;
            if (this.searchQuery !== query) {
                this.searchQuery = query;
                this.runSearch(query);
            }
        },

        /**
         * Change search query and filter results.
         * @param {string} query - New search text.
         */
        runSearch: function(query) {
            this.store('searchQuery', query);
            this.requestTimer = null;
            this.currentStartItem = 0;
            this.filterResults(true);
        },

        /**
         * Filter the data based on the search query, current page, and items per page.
         * @param {bool} refresh - Force it to refresh its data.
         */
        filterResults: function(refresh) {
            if (this.loading) {
                return;
            }

            if (refresh && !this.opts.loadAllData) {
                // force the data to reload. filterResults will get called again after the data loads
                this.loadData();
            } else if (!this.opts.loadAllData) {
                // we're not loading all the data to begin with. so whatever data we have should be displayed.
                this.results = this.data;
                this.pageTotal = Math.ceil(this.filteredTotal / this.itemsPerPage);
            } else {
                // we're loading all the data to begin with. so figure out what data to display.
                var filteredTotal = 0;
                if (this.data.constructor !== Array) {
                    this.loading = true;
                    this.results = [];
                } else {
                    var startItem = this.currentStartItem;
                    var res = this.searchQuery ? this.data.filter(this.filterArray.bind(this.searchQuery.toLowerCase())) : this.data;
                    filteredTotal = res.length;
                    this.results = res.slice(startItem, startItem + this.itemsPerPage);
                }

                this.filteredTotal = filteredTotal;
                this.pageTotal = Math.ceil(filteredTotal / this.itemsPerPage);
            }
        },

        /**
         * Page forward or backward.
         * @param {number} d - Direction to move, 1 is forward, -1 is backward.
         * @param {number} m - Move to end (first or last page) if true.
         */
        moveToPage: function(d, m) {
            this.changePage(d === 1 ? m ? this.pageTotal : this.currentStartItem / this.itemsPerPage + 2 : m ? 1 : this.currentStartItem / this.itemsPerPage);
        },

        /**
         * Move to the specified page number.
         * @param {number|Event} e - New page number, or an event that triggered the change.
         */
        changePage: function(e) {
            if (this.loading) {
                return;
            }

            var page = (isNaN(e) ? e.target.value : e) * 1;
            if (page <= this.pageTotal && page > 0) {
                this.setCurrentStartItem((page - 1) * this.itemsPerPage);
            }
        },

        /**
         * Change the sorting order.
         * @param {string} fieldName - Name of the field to sort on.
         * @param {string} dataType - Data type of the field.
         * @param {Event} e - Event that triggered the change.
         */
        changeSort: function(fieldName, dataType, e) {
            if (this.loading) {
                return;
            }

            var val = $.findByKey(this.sorting, 'field', fieldName);
            if (e.shiftKey) {
                document.getSelection().removeAllRanges();
            } else {
                this.sorting = [];
            }

            if (val === null) {
                this.sorting.push({ field: fieldName, dir: 'ASC', dataType: dataType || 'string' });
            } else if (e.shiftKey) {
                if (val.dir === 'DESC') {
                    this.sorting.splice(val._i, 1);
                } else {
                    val.dir = 'DESC';
                    this.sorting[val._i] = val;
                }
            } else {
                val.dir = val.dir === 'ASC' ? 'DESC' : 'ASC';
                this.sorting.push(val);
            }

            this.sort();
            this.setCurrentStartItem(0);
        },

        /**
         * Sort the underlying data.
         * @param {bool} refresh - Refresh the data from the server.
         */
        sort: function(refresh) {
            refresh = $.coalesce(refresh, true);
            this.data.sort(this.sorting.length > 0 ? this.compare.bind(this) : this.defaultCompare);
            this.filterResults(refresh);
            this.store('sorting', JSON.stringify(this.sorting));
        },

        /**
         * Set up the table and column styles and events.
         */
        setLayout: function() {
            if (this.layoutSet) {
                return;
            }

            this.layoutSet = true;
            this.table = $.get('.table-data', this.content);
            this.table.style.tableLayout = 'fixed';
            this.tableHeaderRow = this.table.tHead.rows[0];

            if (this.table !== null) {
                this.clientWidth = this.content.clientWidth;
                this.table.tHead.style.width = this.table.style.width = (this.width / 100 * this.table.offsetWidth) + 'px';

                var hWidth = this.table.tHead.offsetWidth;
                var tWidth = this.table.offsetWidth;
                var i = 0;
                var cells = this.tableHeaderRow.cells;
                $.forEach(this.opts.columns, function(x) {
                    if (!x.width) {
                        x.width = cells[i].offsetWidth / hWidth * 100;
                    }
                    cells[i].style.width = x.width / 100 * tWidth + 'px';
                    ++i;
                });
            }
        },

        /**
         * Update the table and column widths based on a window resize.
         */
        onResize: function() {
            var cWidth = this.content.clientWidth;
            if (cWidth === 0) {
                return;
            }
            var scale = cWidth / this.clientWidth;
            this.clientWidth = cWidth;
            this.table.tHead.style.width = this.table.style.width = (this.pixelToFloat(this.table.style.width) * scale) + 'px';
            for (var i = 0; i < this.opts.columns.length; i++) {
                this.tableHeaderRow.cells[i].style.width = (this.pixelToFloat(this.tableHeaderRow.cells[i].style.width) * scale) + 'px';
            }
            this.updateLayout();
        },

        /**
         * Update the table header style.
         */
        updateLayout: function() {
            if (!$.isVisible(this.table)) {
                return;
            }
            $.get('.table-scrollable', this.content).style.paddingTop = this.table.tHead.offsetHeight + 'px';
            var colGroup = $.get('.table-column-group', this.content);
            for (var i = 0; i < this.opts.columns.length; i++) {
                colGroup.children[i].style.width = this.tableHeaderRow.cells[i].style.width;
            }
            if (this.clientWidth > 0 && this.content.clientWidth / this.clientWidth !== 1) {
                this.onResize();
            }
        },

        /**
         * Make the table header scroll horizontally with the table
         * @param {Event} e - Event that triggered the scroll.
         */
        onScroll: function(e) {
            var head = this.table.tHead;
            var scroll = e.target;
            if (-head.offsetLeft !== scroll.scrollLeft) {
                head.style.left = '-' + scroll.scrollLeft + 'px';
            }
        },

        /**
         * Handle dragging to change column widths.
         * @param {type} e - Event that triggered the change.
         */
        onHeaderMouseDown: function(e) {
            if (e.button !== 0) {
                return;
            }

            var self = this;
            var callbackFunc = function(cellEl) {
                e.stopImmediatePropagation();
                e.preventDefault();

                self.resizeContext = {
                    colIndex: cellEl.cellIndex,
                    initX: e.clientX,
                    scrWidth: $.get('.table-scrollable', self.content).offsetWidth,
                    initTblWidth: self.table.offsetWidth,
                    initColWidth: self.pixelToFloat($.get('.table-column-group', self.content).children[cellEl.cellIndex].style.width),
                    layoutTimer: null
                };
            };
            self.inResizeArea(e, callbackFunc);
        },

        /**
         * Handle resizing columns.
         * @param {Event} e - Event that triggered the change.
         */
        onMouseMove: function(e) {
            var newStyle = '';
            var cursorFunc = function() {
                newStyle = 'col-resize';
            };
            this.inResizeArea(e, cursorFunc);
            if (this.table.tHead.style.cursor !== newStyle) {
                this.table.tHead.style.cursor = newStyle;
            }

            var ctx = this.resizeContext;
            if ($.isNull(ctx)) {
                return;
            }

            e.stopImmediatePropagation();
            e.preventDefault();

            var newColWidth = Math.max(ctx.initColWidth + e.clientX - ctx.initX, this.opts.columnMinWidth);
            this.table.tHead.style.width = this.table.style.width = (ctx.initTblWidth + (newColWidth - ctx.initColWidth)) + 'px';
            $.get('.table-column-group', this.content).children[ctx.colIndex].style.width = this.tableHeaderRow.cells[ctx.colIndex].style.width = newColWidth + 'px';

            if (ctx.layoutTimer === null) {
                var self = this;
                var timerFunc = function() {
                    self.resizeContext.layoutTimer = null;
                    self.updateLayout();
                };
                ctx.layoutTimer = setTimeout(timerFunc, 25);
            }
        },

        /**
         * Update column widths and save them.
         */
        onMouseUp: function() {
            var ctx = this.resizeContext;
            if ($.isNull(ctx)) {
                return;
            }

            if (ctx.layoutTimer !== null) {
                clearTimeout(ctx.layoutTimer);
            }
            this.resizeContext = null;

            var newTblWidth = this.table.offsetWidth;
            this.width = (newTblWidth / ctx.scrWidth * 100).toFixed(2);
            this.store('width', this.width);
            for (var i = 0; i < this.opts.columns.length; i++) {
                this.opts.columns[i].width = (this.pixelToFloat(this.tableHeaderRow.cells[i].style.width) / newTblWidth * 100).toFixed(2);
                this.store(this.opts.columns[i].field + '.width', this.opts.columns[i].width);
            }

            this.updateLayout();
        },

        /**
         * Check if the cursor is in the area where the user can click to drag a column.
         * @param {Event} e - Event that triggered the check.
         * @param {Function} callback - Function to run if in the resize area.
         */
        inResizeArea: function(e, callback) {
            var tblX = e.clientX;
            var el;
            for (el = this.table.tHead; el !== null; el = el.offsetParent) {
                tblX -= el.offsetLeft + el.clientLeft - el.scrollLeft;
            }

            var cellEl = e.target;
            while (cellEl !== this.table.tHead && cellEl !== null) {
                if (cellEl.nodeName === 'TH') {
                    break;
                }
                cellEl = cellEl.parentNode;
            }

            if (cellEl === this.table.tHead) {
                for (var i = this.tableHeaderRow.cells.length - 1; i >= 0; i--) {
                    cellEl = this.tableHeaderRow.cells[i];
                    if (cellEl.offsetLeft <= tblX) {
                        break;
                    }
                }
            }

            if (cellEl !== null) {
                var x = tblX;
                for (el = cellEl; el !== this.table.tHead; el = el.offsetParent) {
                    if (el === null) {
                        break;
                    }
                    x -= el.offsetLeft - el.scrollLeft + el.clientLeft;
                }
                if (x < 10 && cellEl.cellIndex !== 0) {
                    callback.call(this, cellEl.previousElementSibling);
                } else if (x > cellEl.clientWidth - 10) {
                    callback.call(this, cellEl);
                }
            }
        },

        /**
         * Make column resizing play nice with touch. 
         * http://stackoverflow.com/questions/28218888/touch-event-handler-overrides-click-handlers
         * @param {Event} e Event that triggered the handler.
         */
        touchHandler: function(e) {
            var mouseEvent = null;
            var simulatedEvent = document.createEvent('MouseEvent');
            var touch = e.changedTouches[0];

            switch (e.type) {
                case 'touchstart':
                    mouseEvent = 'mousedown';
                    this.totalDistance = 0;
                    this.lastSeenAt.x = touch.clientX;
                    this.lastSeenAt.y = touch.clientY;
                    break;
                case 'touchmove':
                    mouseEvent = 'mousemove';
                    break;
                case 'touchend':
                    if (this.lastSeenAt.x) {
                        this.totalDistance += Math.sqrt(Math.pow(this.lastSeenAt.y - touch.clientY, 2) + Math.pow(this.lastSeenAt.x - touch.clientX, 2));
                    }
                    mouseEvent = this.totalDistance > 5 ? 'mouseup' : 'click';
                    this.lastSeenAt = { x: null, y: null };
                    break;
            }

            simulatedEvent.initMouseEvent(mouseEvent, true, true, window, 1, touch.screenX, touch.screenY, touch.clientX, touch.clientY, false, false, false, false, 0, null);
            $.dispatch(touch.target, simulatedEvent);
            e.preventDefault();
        },

        /**
         * Get the value for the field coverted to the correct data type.
         * @param {string} value - Value to convert.
         * @returns {*} Converted value.
         */
        getFieldValue: function(value) {
            if ($.isNull(value)) {
                return null;
            }
            return value.getMonth ? value : value.toLowerCase ? value.toLowerCase() : value;
        },

        /**
         * Get the formatted value to display for the field.
         * @param {string} value - Value to format.
         * @param {string} dataType - Data type to format to.
         * @returns {*} Converted value.
         */
        getDisplayValue: function(value, dataType) {
            if (!dataType || $.isNull(value)) {
                return value;
            }

            var val = value;
            if (dataType === 'currency') {
                val = $.accounting.formatMoney(val, this.opts.displayCurrencyFormat);
            } else if (dataType === 'date') {
                val = $.fecha.format(val, this.opts.displayDateFormat);
            }
            return val;
        },

        /**
         * Default sorting function for the data - resets to order when data was loaded.
         * @param {Object} a - First object to compare.
         * @param {Object} b - Object to compare to.
         * @returns {number} 1 if a comes first, -1 if b comes first, else 0.
         */
        defaultCompare: function(a, b) {
            return a._index > b._index ? 1 : a._index < b._index ? -1 : 0;
        },

        /**
         * Multi-sorting function for the data.
         * @param {Object} a - First object to compare.
         * @param {Object} b - Object to compare to.
         * @returns {number} 1 if a comes first, -1 if b comes first, else 0.
         */
        compare: function(a, b) {
            var sorting = this.sorting;
            var i = 0, len = sorting.length;
            for (; i < len; i++) {
                var sort = sorting[i];
                var aa = this.getFieldValue(a[sort.field]);
                var bb = this.getFieldValue(b[sort.field]);

                if (aa === null) {
                    return 1;
                }
                if (bb === null) {
                    return -1;
                }
                if (aa < bb) {
                    return sort.dir === 'ASC' ? -1 : 1;
                }
                if (aa > bb) {
                    return sort.dir === 'ASC' ? 1 : -1;
                }
            }
            return 0;
        },

        /**
         * Filter an array of objects to find objects where value contains the value of `this`
         * @param {Object} obj - Object to search in.
         * @returns {bool} True if object contains `this`.
         */
        filterArray: function(obj) {
            for (var key in obj) {
                if (key.indexOf('_') < 0 && obj.hasOwnProperty(key) && (obj[key] + '').toLowerCase().indexOf(this) > -1) {
                    return true;
                }
            }
            return false;
        },

        /**
         * Convert a style with 'px' to a float.
         * @param {string} val - CSS style to convert.
         * @returns {number} Numeric value.
         */
        pixelToFloat: function(val) {
            return val.replace('px', '').replace('%', '') * 1.0;
        },

        /**
         * Build the table header tags.
         * @param {Object} obj - Column to build the tag for.
         * @returns {Object} Mithril TH node.
         */
        tableHeaders: function(obj) {
            var field = obj.field;
            var attrs = { class: obj.classes || '' };

            var content = [obj.label || field];
            if ($.isUndefined(obj.sortable) || obj.sortable === true) {
                var val = $.findByKey(this.sorting, 'field', field);
                var arrowAttrs = {
                    class: val ? (val.dir === 'ASC' ? 'dash-sort-up' : 'dash-sort-down') : this.opts.editable ? 'dash-sort' : ''
                };
                if (this.opts.editable) {
                    arrowAttrs.onclick = this.changeSort.bind(this, field, obj.dataType.toLowerCase());
                }
                content.push(m('i.float-right.dash.data-table-arrow', arrowAttrs));
            } else {
                attrs.class += ' disabled';
            }
            if (this.opts.editable) {
                attrs.onmousedown = this.onHeaderMouseDown.bind(this);
            }
            return m('th.text-no-select', attrs, content);
        },

        /**
         * Build the view that actually shows the table.
         * @returns {Object}  Mithril DIV node.
         */
        view: function() {
            return m('.dash-table',
                {
                    ontableRefresh: this.refresh.bind(this),
                    ontableDestroy: this.destroy.bind(this),
                    onlayoutUpdate: this.updateLayout.bind(this),
                    'data-unload-event': 'tableUnload',
                    'data-toggle': 'table'
                }, [
                    !this.opts.editable ? m('span#table-items-per-page') :
                        m('.container',
                            m('.columns.form-horizontal.m-2', [
                                m('.col-6',
                                    this.opts.searchable ? m('.input-group.col-8.col-mr-auto', [
                                        m('span.input-group-addon.text-no-select', m('i.dash.dash-search')),
                                        m('input.form-input', { type: 'text', oninput: this.setSearchQuery.bind(this), value: this.searchQuery, disabled: this.loading })
                                    ]) : null
                                ),
                                m('.col-6',
                                    m('.input-group.col-8.col-ml-auto', [
                                        m('span.input-group-addon.text-no-select', this.opts.resources.perPage),
                                        m('select.form-select', {
                                            id: 'table-items-per-page', onchange: this.setItemsPerPage.bind(this),
                                            value: this.itemsPerPage, disabled: this.loading
                                        }, this.pageOptions)
                                    ])
                                )
                            ])
                        ),
                    m('.table-scrollable', { class: this.opts.editable ? '' : ' table-no-edit' }, [
                        m('.table-area', { onscroll: this.onScroll.bind(this) }, [
                            m('table.table.table-hover.table-sm.table-striped.table-data', [
                                m('colgroup.table-column-group', this.colGroups),
                                m('thead', {
                                    ontouchstart: this.touchHandler.bind(this),
                                    ontouchend: this.touchHandler.bind(this),
                                    ontouchmove: this.touchHandler.bind(this),
                                    ontouchcancel: this.touchHandler.bind(this)
                                }, [m('tr', $.map(this.opts.columns, this.tableHeaders, this))]),
                                m('tbody', this.tableBodyView())
                            ])
                        ])
                    ]),
                    this.tableFooterView()
                ]);
        },

        /**
         * Build a single table cell.
         * @param {Object} obj - Table record to build cell for.
         * @param {number} index - Row index of this row.
         * @param {Object} column - Column to build cell for.
         * @returns {Object} Mithril TD node.
         */
        tableCellView: function(obj, index, column) {
            return m('td', this.columnRenderer[column.field](obj, column, index));
        },

        /**
         * Build the table footer nodes
         * @returns {Object} Mithril TR node(s).
         */
        tableBodyView: function() {
            if (this.loading) {
                return m('tr', m('td', { colspan: this.opts.columns.length }, m('.loading.loading-lg')));
            }
            if (this.loadingError) {
                return m('tr.table-loading-error', m('td', { colspan: this.opts.columns.length }, [
                    m('.table-loading-error-message', this.opts.resources.loadingError),
                    m('.btn.btn-info', { onclick: this.refresh.bind(this) }, this.opts.resources.tryAgain)
                ]));
            }
            if (this.filteredTotal === 0) {
                return m('tr', [m('td', { colspan: this.opts.columns.length }, this.opts.resources.noData)]);
            }
            var self = this;
            return $.map(self.results, function(row, index) {
                return m('tr', { key: row._index }, $.map(self.opts.columns, function(column) {
                    return m('td', self.columnRenderer[column.field](row, column, index));
                }));
            });
        },

        /**
         * Build the table footer nodes.
         * @returns {Object} Mithril DIV node.
         */
        tableFooterView: function() {
            if (this.loading || this.loadingError) {
                return null;
            }

            var currentPage = (this.currentStartItem + this.itemsPerPage) / this.itemsPerPage;
            if (this.opts.pageDropdown) {
                // limit page dropdown to 10000 options
                var max = Math.min(this.pageTotal, 10000);
                var optionList = [max], i = max;
                while (i > 0) {
                    optionList[i] = m('option', { value: i }, i);
                    --i;
                }
            }

            var res = this.opts.resources;
            return m('.container', m('.columns.m-2', [
                m('.col-4.btn-toolbar', { class: this.filteredTotal > this.itemsPerPage ? '' : ' invisible' }, [
                    m('button.btn.btn-secondary', {
                        type: 'button', role: 'button', title: res.firstPage, onclick: this.moveToPage.bind(this, -1, true)
                    }, m('i.dash.dash-to-start-alt.text-primary')),
                    m('button.btn.btn-secondary', {
                        type: 'button', role: 'button', title: res.previousPage, onclick: this.moveToPage.bind(this, -1, false)
                    }, m('i.dash.dash-to-start.text-primary')),
                    m('button.btn.btn-secondary', {
                        type: 'button', role: 'button', title: res.nextPage, onclick: this.moveToPage.bind(this, 1, false)
                    }, m('i.dash.dash-to-end.text-primary')),
                    m('button.btn.btn-secondary', {
                        type: 'button', role: 'button', title: res.lastPage, onclick: this.moveToPage.bind(this, 1, true)
                    }, m('i.dash.dash-to-end-alt.text-primary'))
                ]),
                m('.col-4', { class: this.filteredTotal > this.itemsPerPage ? '' : ' invisible' },
                    !this.opts.pageDropdown ? null : m('.input-group.col-8.col-mx-auto', [
                        m('span.input-group-addon.text-no-select', res.page),
                        m('select.form-select', { onchange: this.changePage.bind(this), value: currentPage, disabled: this.pageTotal === 0 }, optionList)
                    ])
                ),
                m('.col-4.text-right.my-auto', res.showing
                    .replace('{0}', Math.min(this.currentStartItem + 1, this.filteredTotal))
                    .replace('{1}', Math.min(this.currentStartItem + this.itemsPerPage, this.filteredTotal))
                    .replace('{2}', this.filteredTotal)
                )
            ]));
        },

        destroy: function() {
            if (this.opts.editable) {
                $.off(window, 'resize', this.events.resize);
                $.off(window, 'mousemove', this.events.move);
                $.off(window, 'mouseup', this.events.up);
            }
        },

        oninit: function(vnode) {
            var opts = vnode.attrs || {};

            var data = null;
            if (opts.data) {
                data = opts.data;
                delete opts.data;
            }

            this.opts = $.extend({
                content: null,
                id: null,
                columns: [],
                url: '',
                requestMethod: 'GET',
                requestUsePascalCase: true,
                requestParams: {},
                searchable: true,
                loadAllData: true,
                columnMinWidth: 50,
                width: 100,
                editable: true,
                pageDropdown: true,
                storeSettings: true,
                storageFunction: null,
                itemsPerPage: null,
                searchQuery: null,
                currentStartItem: null,
                sorting: null,
                dataCallback: null,
                errorCallback: null,
                dataDateFormat: 'YYYY-MM-DD HH:mm:ss',
                displayDateFormat: 'YYYY-MM-DD HH:mm',
                displayCurrencyFormat: '{s:$} {[t:,][d:.][p:2]}',
                resources: {
                    firstPage: $.resx('firstPage'),
                    previousPage: $.resx('previousPage'),
                    nextPage: $.resx('nextPage'),
                    lastPage: $.resx('lastPage'),
                    noData: $.resx('noData'),
                    showing: $.resx('showing'),
                    page: $.resx('page') || 'Page',
                    perPage: $.resx('perPage'),
                    loadingError: $.resx('loadingError'),
                    tryAgain: $.resx('tryAgain')
                }
            }, opts);

            this.layoutSet = false;
            this.data = null;
            this.loading = true;
            this.loadingError = false;
            this.filteredTotal = 0;
            this.results = [];
            this.pageTotal = 0;
            this.totalDistance = 0;
            this.lastSeenAt = { x: null, y: null };
            this.columnRenderer = {};
            this.colGroups = [];
            this.intColumns = [];
            this.dateColumns = [];
            this.currencyColumns = [];

            var self = this;
            for (var i = 0; i < this.opts.columns.length; i++) {
                var column = this.opts.columns[i];
                column.width = $.hasPositiveValue(column.width) ? column.width : this.store(column.field + '.width');
                if (!($.isNull(column.links) || column.links.length === 0)) {
                    column.links = column.links.filter(function(link) {
                        return !$.isNull(link);
                    });
                }

                this.columnRenderer[column.field] = $.isNull(column.links) || column.links.length === 0 ?
                    function(obj, column) { return self.getDisplayValue(obj[column.field], column.dataType.toLowerCase()); } :
                    function(obj, column) {
                        return $.map(column.links, function(link) {
                            if (link.jsonLogic && !$.jsonLogic.apply(link.jsonLogic, obj)) {
                                return null;
                            }
                            var label = $.coalesce(link.label, self.getDisplayValue(obj[column.field], column.dataType.toLowerCase()));
                            var attr = $.clone(link.attributes) || {};
                            var href = link.href || null;
                            if (href) {
                                for (var prop in obj) {
                                    if (href.indexOf('{' + prop + '}') > -1 && obj.hasOwnProperty(prop)) {
                                        href = href.replace(new RegExp('{' + prop + '}', 'g'), obj[prop]);
                                    }
                                }
                            }
                            var classes = (attr['class'] || '').split(' ');
                            var isBtn = classes.indexOf('btn') !== -1;
                            if (isBtn) {
                                attr['type'] = attr['role'] = 'button';
                            } else {
                                classes.push('btn');
                                classes.push('btn-link');
                            }
                            attr['class'] = classes.filter(function(x) { return x && x.length; }).join(' ');
                            attr['title'] = label;
                            if (attr['target']) {
                                attr['href'] = href;
                            } else {
                                attr['data-method'] = link.method ? link.method.toUpperCase() : 'GET';
                                attr['data-href'] = href;
                                attr['onclick'] = function() {
                                    var node = this.getAttribute('data-href') ? this : this.parentNode;
                                    var options = {
                                        url: node.getAttribute('data-href'), method: node.getAttribute('data-method')
                                    };
                                    if (node.getAttribute('data-confirm')) {
                                        options.history = false;
                                        Alertify.dismissAll();
                                        Alertify.confirm(node.getAttribute('data-confirm'), pjax.invoke.bind(null, options));
                                    } else if (node.getAttribute('data-prompt')) {
                                        options.history = false;
                                        Alertify.dismissAll();
                                        Alertify.prompt(node.getAttribute('data-prompt'), function(promptValue) {
                                            if (!$.hasValue(promptValue)) {
                                                Alertify.error($.resx('errorNameRequired'));
                                                return false;
                                            }
                                            options.url += ((!/[?&]/.test(options.url)) ? '?prompt' : '&prompt') + '=' + encodeURIComponent(promptValue);
                                            pjax.invoke(options);
                                        });
                                    } else {
                                        options.history = node.getAttribute('data-method') === 'GET';
                                        pjax.invoke(options);
                                    }
                                };
                            }
                            return m(isBtn ? 'button' : 'a', attr, $.isNull(link.icon) ? label : m('i', { class: 'dash dash-' + link.icon.toLowerCase() }));
                        });
                    };

                this.colGroups.push(m('col'));

                var type = column.dataType.toLowerCase();
                if (type === 'int') {
                    this.intColumns.push(column.field);
                } else if (type === 'date') {
                    this.dateColumns.push(column.field);
                } else if (type === 'currency') {
                    this.currencyColumns.push(column.field);
                }
            }

            this.itemsPerPage = this.store('itemsPerPage') * 1 || 10;
            this.currentStartItem = this.store('currentStartItem') * 1 || 0;
            this.searchQuery = this.store('searchQuery') || '';
            this.width = this.store('width') * 1 || 100;
            var sorting = this.store('sorting');
            this.sorting = (typeof sorting === 'string' ? JSON.parse(sorting) : sorting) || [];

            if (data) {
                this.processData({ rows: data });
            } else {
                this.loadData();
            }
        },

        oncreate: function(vnode) {
            this.content = vnode.dom;
            if (this.opts.editable) {
                this.events = {
                    resize: $.debounce(this.onResize.bind(this), 50),
                    move: this.onMouseMove.bind(this),
                    up: this.onMouseUp.bind(this)
                };
                $.on(window, 'resize', this.events.resize);
                $.on(window, 'mousemove', this.events.move);
                $.on(window, 'mouseup', this.events.up);
            }

            this.setLayout();
            this.updateLayout();
        },

        onupdate: function() {
            this.updateLayout();
        }
    };

    return Table;
});
