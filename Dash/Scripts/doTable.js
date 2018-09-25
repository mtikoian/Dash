/*!
 * doT based table component. Supports ajax data, searching, sorting, paging, & resizing columns.
 */
(function(root, factory) {
    root.doTable = factory(root.doT, root.$);
})(this, function(doT, $) {
    'use strict';

    var _templates = {
        headerFn: doT.template($.get('#tableHeaderTemplate').text),
        footerFn: doT.template($.get('#tableFooterTemplate').text),
        bodyFn: doT.template($.get('#tableBodyTemplate').text),
        loadingFn: doT.template($.get('#tableLoadingTemplate').text),
        noDataFn: doT.template($.get('#tableNoDataTemplate').text),
        errorFn: doT.template($.get('#tableLoadingError').text)
    };

    /**
    * Default sorting function for the data - resets to order when data was loaded.
    * @param {Object} a - First object to compare.
    * @param {Object} b - Object to compare to.
    * @returns {number} 1 if a comes first, -1 if b comes first, else 0.
    */
    var defaultCompare = function(a, b) {
        return a._index > b._index ? 1 : a._index < b._index ? -1 : 0;
    };

    var getFieldValue = function(value) {
        if ($.isNull(value)) {
            return null;
        }
        return value.getMonth ? value : value.toLowerCase ? value.toLowerCase() : value;
    };

    /**
     * Multi-sorting function for the data.
     * @this Object[] - Array that defines current sort columns.
     * @param {Object} a - First object to compare.
     * @param {Object} b - Object to compare to.
     * @returns {number} 1 if a comes first, -1 if b comes first, else 0.
     */
    var compare = function(a, b) {
        var i = 0, len = this.length;
        for (; i < len; i++) {
            var sort = this[i];
            var aa = getFieldValue(a[sort.field]);
            var bb = getFieldValue(b[sort.field]);

            if (aa === null) {
                return 1;
            }
            if (bb === null) {
                return -1;
            }
            if (aa < bb) {
                return sort.sortDir === 'ASC' ? -1 : 1;
            }
            if (aa > bb) {
                return sort.sortDir === 'ASC' ? 1 : -1;
            }
        }
        return 0;
    };

    /**
     * Filter an array of objects to find objects where value contains the value of `this`
     * @param {Object} obj - Object to search in.
     * @returns {bool} True if object contains `this`.
     */
    var filterArray = function(obj) {
        for (var key in obj) {
            if (key.indexOf('_') < 0 && obj.hasOwnProperty(key) && (obj[key] + '').toLowerCase().indexOf(this) > -1) {
                return true;
            }
        }
        return false;
    };

    /**
     * Convert a style with 'px' to a float.
     * @param {string} val - CSS style to convert.
     * @returns {number} Numeric value.
     */
    var pixelToFloat = function(val) {
        return val.replace('px', '').replace('%', '') * 1.0;
    };

    var disableIf = function(node, disable) {
        if (!node) {
            return;
        }
        if (disable) {
            node.setAttribute('disabled', true);
        } else {
            node.removeAttribute('disabled');
        }
    };

    var getDisplayValue = function(displayCurrencyFormat, displayDateFormat, value, dataType) {
        if (!dataType || $.isNull(value)) {
            return value;
        }

        var val = value;
        if (dataType === 'currency') {
            val = $.accounting.formatMoney(val, displayCurrencyFormat);
        } else if (dataType === 'date') {
            val = $.fecha.format(val, displayDateFormat);
        }
        return val;
    };

    var doTable = function(node) {
        var opts = {};

        opts.id = node.getAttribute('id');
        opts.editable = node.getAttribute('data-editable').toLowerCase() === 'true';
        opts.searchable = node.getAttribute('data-searchable').toLowerCase() === 'true' && opts.editable;
        opts.storeSettings = node.getAttribute('data-store-settings').toLowerCase() === 'true';
        opts.loadAll = node.getAttribute('data-load-all').toLowerCase() === 'true';
        opts.url = node.getAttribute('data-url');
        opts.template = node.getAttribute('data-template');

        opts.requestMethod = node.getAttribute('data-request-method');
        opts.storeUrl = node.getAttribute('data-store-url');
        opts.width = node.hasAttribute('data-width') ? node.getAttribute('data-width') * 1 : null;
        opts.storeRequestMethod = node.getAttribute('data-store-request-method');
        opts.displayDateFormat = node.getAttribute('data-display-date-format');
        opts.displayCurrencyFormat = node.getAttribute('data-display-currency-format');
        if (node.hasAttribute('data-request-params')) {
            var params = node.getAttribute('data-request-params');
            try {
                opts.requestParams = JSON.parse(params);
            } catch (e) {
                // placeholder
            }
        }

        node.doTable = this;
        this.opts = $.extend({
            id: null,
            columns: [],
            url: '',
            requestMethod: 'POST',
            requestUsePascalCase: true,
            requestParams: {},
            searchable: true,
            loadAll: true,
            columnMinWidth: 50,
            width: 100,
            editable: true,
            storeSettings: true,
            storeUrl: null,
            storeRequestMethod: 'PUT',
            itemsPerPage: null,
            searchQuery: null,
            currentStartItem: null,
            currentEndItem: null,
            sorting: null,
            dataDateFormat: 'YYYY-MM-DD HH:mm:ss',
            displayDateFormat: 'YYYY-MM-DD HH:mm',
            displayCurrencyFormat: '{s:$} {[t:,][d:.][p:2]}'
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

        this.storeFunction = null;
        if (this.opts.storeUrl) {
            var storeUrl = this.opts.storeUrl;
            var storeRequestMethod = this.opts.storeRequestMethod;
            this.storeFunction = $.debounce(function(data) {
                $.ajax({
                    url: storeUrl,
                    method: storeRequestMethod,
                    data: data
                });
            }, 250);
        }

        var template = $.get(this.opts.template);
        this.opts.rowTemplateFn = doT.template(template ? template.text : '');
        this.opts.displayValueFn = getDisplayValue.bind(null, this.opts.displayCurrencyFormat, this.opts.displayDateFormat);

        var sorting = this.store('sorting');
        var sortColumns = [];
        if (sorting) {
            try {
                sortColumns = (typeof sorting === 'string' ? JSON.parse(sorting) : sorting) || [];
            } catch (e) {
                // placeholder
            }
        }

        if (template) {
            var tempNode = $.createNode();
            tempNode.innerHTML = '<table>' + this.opts.rowTemplateFn({}) + '</table>';
            var columns = $.getAll('td', tempNode);
            $.forEach(columns, function(x) {
                var field = x.getAttribute('data-field');
                var width = x.getAttribute('data-width');
                width = isNaN(width) ? null : width * 1;
                var type = x.getAttribute('data-type').toLowerCase();
                var column = {
                    width: $.hasPositiveValue(width) ? width : this.store(field + '.width'),
                    sortable: x.getAttribute('data-sortable').toLowerCase() === 'true',
                    label: x.getAttribute('data-label'),
                    field: field,
                    dataType: type
                };
                if (type === 'int') {
                    this.intColumns.push(field);
                } else if (type === 'date') {
                    this.dateColumns.push(field);
                } else if (type === 'currency') {
                    this.currencyColumns.push(field);
                }

                var dir = x.getAttribute('data-sort-dir');
                if (dir) {
                    column.sortDir = dir.toUpperCase();
                    column.sortOrder = x.getAttribute('data-sort-order');
                }

                var sortColumn = $.findByKey(sortColumns, 'field', field);
                if (sortColumn) {
                    column.sortDir = sortColumn.sortDir;
                    column.sortOrder = sortColumn.sortOrder;
                }

                this.opts.columns.push(column);
            }, this);
        }

        this.itemsPerPage = this.store('itemsPerPage') * 1 || 10;
        this.currentStartItem = this.store('currentStartItem') * 1 || 0;
        this.searchQuery = this.store('searchQuery') || '';
        this.width = this.store('width') * 1 || 100;

        this.draw();
        this.loadData();
    };

    /**
    * Get/set persistent values.
    * @param {string} key - Key name of the value to get/set.
    * @param {*} value - Value to set.
    * @returns {string|undefined} Value if getting, else undefined.
    */
    doTable.prototype.store = function(key, value) {
        if (!this.opts.storeSettings) {
            return;
        }
        var myKey = this.opts.id + '.' + key;
        // getter
        if ($.isUndefined(value)) {
            return $.isNull(this.opts.storeUrl) ? localStorage[myKey] : $.coalesce(this.opts[key], null);
        }

        // setter
        if ($.isNull(this.storeFunction)) {
            localStorage[myKey] = value;
        } else {
            var data;
            if (this.opts.requestUsePascalCase) {
                data = $.extend($.toPascalKeys(this.opts.requestParams), {
                    ItemsPerPage: this.itemsPerPage,
                    CurrentStartItem: this.currentStartItem,
                    SearchQuery: this.searchQuery,
                    Width: this.width,
                    Sorting: this.buildSortList(),
                    Columns: $.toPascalKeys($.map(this.opts.columns, function(c) { return { field: c.field, width: c.width * 1.0 }; }))
                });
            } else {
                data = $.extend(this.opts.requestParams, {
                    itemsPerPage: this.itemsPerPage,
                    currentStartItem: this.currentStartItem,
                    searchQuery: this.searchQuery,
                    width: this.width,
                    sorting: this.buildSortList(),
                    columns: $.map(this.opts.columns, function(c) { return { field: c.field, width: c.width * 1.0 }; })
                });
            }

            this.storeFunction.call(null, data);
        }
    };

    /**
     * Process the data array result from the ajax request.
     * @param {Object[]} data - Array of records to display.
     */
    doTable.prototype.processData = function(data) {
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
                // @todo switch to using flatpickr.parseDate and flatpickr.formatDate
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
    };

    /**
     * Load the data to populate the table.
     */
    doTable.prototype.loadData = function() {
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
            self.draw();
        });
    };

    /**
     * Force the table to refresh its data.
     */
    doTable.prototype.refresh = function() {
        this.loading = true;
        this.loadingError = false;
        this.loadData();
    };

    /**
     * Build an array containing the sorting info.
     */
    doTable.prototype.buildSortList = function() {
        var sorting = [];
        $.forEach(this.opts.columns, function(x) {
            if (x.sortDir) {
                sorting.push({ field: x.field, sortDir: x.sortDir, sortOrder: x.sortOrder });
            }
        });
        return sorting.length ? sorting : null;
    };

    /**
     * Build querystring params to fetch data from the server.
     * @returns {Object} Request parameters.
     */
    doTable.prototype.buildParams = function() {
        var sort = this.buildSortList();
        if (this.opts.requestUsePascalCase) {
            return $.extend($.toPascalKeys(this.opts.requestParams), {
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
    };

    /**
     * Set the first item index to display.
     * @param {type} index - Record index to start on.
     */
    doTable.prototype.setCurrentStartItem = function(index) {
        this.currentStartItem = index;
        this.store('currentStartItem', index);
        this.filterResults(true);
    };

    /**
     * Set the number of items to display per page.
     * @param {number|Event} e - Number or items per page, or an event that triggered the change.
     */
    doTable.prototype.setItemsPerPage = function(e) {
        if (this.loading) {
            return;
        }

        var items = (isNaN(e) ? e.target.value : e) * 1;
        if (this.itemsPerPage !== items) {
            this.itemsPerPage = items;
            this.store('itemsPerPage', items);
            this.setCurrentStartItem(0);
        }
    };

    /**
     * Set the search query for filtering table data.
     * @param {string} val - New search text.
     */
    doTable.prototype.setSearchQuery = function(val) {
        if (this.loading) {
            return;
        }

        var query = val.target ? val.target.value : val;
        if (this.searchQuery !== query) {
            this.searchQuery = query;
            this.store('searchQuery', query);
            this.requestTimer = null;
            this.currentStartItem = 0;
            this.currentEndItem = 0;
            this.filterResults(true);
        }
    };

    /**
     * Filter the data based on the search query, current page, and items per page.
     * @param {bool} refresh - Force it to refresh its data.
     */
    doTable.prototype.filterResults = function(refresh) {
        if (this.loading) {
            return;
        }

        if (refresh && !this.opts.loadAll) {
            // force the data to reload. filterResults will get called again after the data loads
            this.loadData();
        } else if (!this.opts.loadAll) {
            // we're not loading all the data to begin with. so whatever data we have should be displayed.
            this.results = this.data;
            this.currentEndItem = Math.min(this.currentStartItem + this.itemsPerPage, this.filteredTotal);
            this.pageTotal = Math.ceil(this.filteredTotal / this.itemsPerPage);
            this.draw();
        } else {
            // we're loading all the data to begin with. so figure out what data to display.
            var filteredTotal = 0;
            if (this.data.constructor !== Array) {
                this.loading = true;
                this.results = [];
            } else {
                var startItem = this.currentStartItem;
                var res = this.searchQuery ? this.data.filter(filterArray.bind(this.searchQuery.toLowerCase())) : this.data;
                filteredTotal = res.length;
                this.results = res.slice(startItem, startItem + this.itemsPerPage);
            }
            this.currentEndItem = Math.min(this.currentStartItem + this.itemsPerPage, filteredTotal);
            this.pageTotal = Math.ceil(filteredTotal / this.itemsPerPage);
            this.filteredTotal = filteredTotal;
            this.draw();
        }
    };

    /**
     * Page forward or backward.
     * @param {number} d - Direction to move, 1 is forward, -1 is backward.
     * @param {number} m - Move to end (first or last page) if true.
     */
    doTable.prototype.moveToPage = function(d, m) {
        this.changePage(d === 1 ? m ? this.pageTotal : this.currentStartItem / this.itemsPerPage + 2 : m ? 1 : this.currentStartItem / this.itemsPerPage);
    };

    /**
     * Move to the specified page number.
     * @param {number|Event} e - New page number, or an event that triggered the change.
     */
    doTable.prototype.changePage = function(e) {
        if (this.loading) {
            return;
        }

        var page = (isNaN(e) ? e.target.value : e) * 1;
        if (page <= this.pageTotal && page > 0) {
            this.setCurrentStartItem((page - 1) * this.itemsPerPage);
            this.draw();
        }
    };

    /**
     * Reset table sorting.
     */
    doTable.prototype.resetSort = function(column) {
        $.forEach(this.opts.columns, function(x) {
            if (x !== this) {
                delete x.sortOrder;
                delete x.sortDir;
            }
        }, column);
    };



    /**
     * Change the sorting order.
     * @param {string} fieldName - Name of the field to sort on.
     * @param {string} dataType - Data type of the field.
     * @param {Event} e - Event that triggered the change.
     */
    doTable.prototype.changeSort = function(fieldName, dataType, e) {
        if (this.loading) {
            return;
        }

        var sortOrder = this.opts.columns.filter(function(x) {
            return x.sortDir;
        }).length + 1;

        var column = $.findByKey(this.opts.columns, 'field', fieldName);
        if (e.shiftKey) {
            document.getSelection().removeAllRanges();
        } else {
            sortOrder = 0;
            this.resetSort(column);
        }

        if ($.isNull(column.sortDir)) {
            column.sortDir = 'ASC';
            column.sortOrder = sortOrder;
        } else if (e.shiftKey) {
            if (column.dir === 'DESC') {
                delete column.sortDir;
                delete column.sortOrder;
            } else {
                column.sortDir = 'DESC';
            }
        } else {
            column.sortDir = column.sortDir === 'ASC' ? 'DESC' : 'ASC';
        }

        this.sort();
        this.setCurrentStartItem(0);
        this.draw();
    };

    /**
     * Sort the underlying data.
     * @param {bool} refresh - Refresh the data from the server.
     */
    doTable.prototype.sort = function(refresh) {
        refresh = $.coalesce(refresh, true);

        var sortColumns = this.buildSortList();
        this.data.sort(sortColumns && sortColumns.length > 0 ? compare.bind(sortColumns) : defaultCompare);
        this.store('sorting', JSON.stringify(sortColumns));
        this.filterResults(refresh);
    };

    /**
     * Set up the table and column styles and events.
     */
    doTable.prototype.setLayout = function() {
        if (this.layoutSet) {
            return;
        }

        var contentNode = this.getContainer();
        this.layoutSet = true;
        this.table = $.get('.dotable-data', contentNode);
        this.table.style.tableLayout = 'fixed';
        this.tableHeaderRow = this.table.tHead.rows[0];

        if (this.table !== null) {
            this.clientWidth = contentNode.clientWidth;
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
    };

    /**
     * Update the table and column widths based on a window resize.
     */
    doTable.prototype.onResize = function() {
        var cWidth = this.getContainer().clientWidth;
        if (cWidth === 0) {
            return;
        }
        var scale = cWidth / this.clientWidth;
        this.clientWidth = cWidth;
        this.table.tHead.style.width = this.table.style.width = (pixelToFloat(this.table.style.width) * scale) + 'px';
        for (var i = 0; i < this.opts.columns.length; i++) {
            this.tableHeaderRow.cells[i].style.width = (pixelToFloat(this.tableHeaderRow.cells[i].style.width) * scale) + 'px';
        }
        this.updateLayout();
    };

    /**
     * Update the table header style.
     */
    doTable.prototype.updateLayout = function() {
        if (!$.isVisible(this.table)) {
            return;
        }
        var contentNode = this.getContainer();
        $.get('.dotable-scrollable', contentNode).style.paddingTop = this.table.tHead.offsetHeight + 'px';
        var colGroup = $.get('.dotable-column-group', contentNode);
        for (var i = 0; i < this.opts.columns.length; i++) {
            colGroup.children[i].style.width = this.tableHeaderRow.cells[i].style.width;
        }
        if (this.clientWidth > 0 && contentNode.clientWidth / this.clientWidth !== 1) {
            this.onResize();
        }
    };

    /**
     * Make the table header scroll horizontally with the table
     * @param {Event} e - Event that triggered the scroll.
     */
    doTable.prototype.onScroll = function(e) {
        var head = this.table.tHead;
        var scroll = e.target;
        if (-head.offsetLeft !== scroll.scrollLeft) {
            head.style.left = '-' + scroll.scrollLeft + 'px';
        }
    };

    /**
     * Handle dragging to change column widths.
     * @param {type} e - Event that triggered the change.
     */
    doTable.prototype.onHeaderMouseDown = function(e) {
        if (e.button !== 0) {
            return;
        }

        var self = this;
        var callbackFunc = function(cellEl) {
            e.stopImmediatePropagation();
            e.preventDefault();

            var contentNode = this.getContainer();
            self.resizeContext = {
                colIndex: cellEl.cellIndex,
                initX: e.clientX,
                scrWidth: $.get('.dotable-scrollable', contentNode).offsetWidth,
                initTblWidth: self.table.offsetWidth,
                initColWidth: pixelToFloat($.get('.dotable-column-group', contentNode).children[cellEl.cellIndex].style.width),
                layoutTimer: null
            };
        };
        self.inResizeArea(e, callbackFunc);
    };

    /**
     * Handle resizing columns.
     * @param {Event} e - Event that triggered the change.
     */
    doTable.prototype.onMouseMove = function(e) {
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
        $.get('.dotable-column-group', this.getContainer()).children[ctx.colIndex].style.width = this.tableHeaderRow.cells[ctx.colIndex].style.width = newColWidth + 'px';

        if (ctx.layoutTimer === null) {
            var self = this;
            var timerFunc = function() {
                self.resizeContext.layoutTimer = null;
                self.updateLayout();
            };
            ctx.layoutTimer = setTimeout(timerFunc, 25);
        }
    };

    /**
     * Update column widths and save them.
     */
    doTable.prototype.onMouseUp = function() {
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
            this.opts.columns[i].width = (pixelToFloat(this.tableHeaderRow.cells[i].style.width) / newTblWidth * 100).toFixed(2);
            this.store(this.opts.columns[i].field + '.width', this.opts.columns[i].width);
        }

        this.updateLayout();
    };

    /**
     * Check if the cursor is in the area where the user can click to drag a column.
     * @param {Event} e - Event that triggered the check.
     * @param {Function} callback - Function to run if in the resize area.
     */
    doTable.prototype.inResizeArea = function(e, callback) {
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
    };

    /**
     * Make column resizing play nice with touch. 
     * http://stackoverflow.com/questions/28218888/touch-event-handler-overrides-click-handlers
     * @param {Event} e Event that triggered the handler.
     */
    doTable.prototype.touchHandler = function(e) {
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
    };

    doTable.prototype.getContainer = function() {
        return $.get('#' + this.opts.id);
    };

    doTable.prototype.create = function() {
        var contentNode = this.getContainer();
        contentNode.innerHTML = '';

        var container = $.createNode('<div class="dash-table"></div>');
        container.innerHTML = _templates.headerFn(this) +
            _templates.bodyFn(this) +
            _templates.footerFn(this);

        contentNode.appendChild(container);

        $.on($.get('.dotable-search-input', container), 'input', this.setSearchQuery.bind(this));
        $.on($.get('.dotable-items-input', container), 'change', this.setItemsPerPage.bind(this));

        if (this.opts.editable) {
            // bind column sort and column resize events
            var thead = $.get('.dotable-head', container);
            if (thead) {
                var handler = this.touchHandler.bind(this);
                $.on(thead, 'touchstart', handler);
                $.on(thead, 'touchend', handler);
                $.on(thead, 'touchmove', handler);
                $.on(thead, 'touchcancel', handler);

                var ths = $.getAll('th', thead);
                if (ths && ths.length) {
                    var mouseFunc = this.onHeaderMouseDown.bind(this);
                    $.forEach(ths, function(x) {
                        $.on(x, 'mousedown', mouseFunc);
                    });
                }

                var arrows = $.getAll('.dotable-arrow', thead);
                if (arrows && arrows.length) {
                    var self = this;
                    $.forEach(arrows, function(x) {
                        $.on(x, 'click', self.changeSort.bind(self, x.getAttribute('data-field'), x.getAttribute('data-type').toLowerCase()));
                    });
                }
            }

            this.events = {
                resize: $.debounce(this.onResize.bind(this), 50),
                move: this.onMouseMove.bind(this),
                up: this.onMouseUp.bind(this)
            };
            $.on(window, 'resize', this.events.resize);
            $.on(window, 'mousemove', this.events.move);
            $.on(window, 'mouseup', this.events.up);
        }

        $.on('.dotable-btn-first', 'click', this.moveToPage.bind(this, -1, true));
        $.on('.dotable-btn-previous', 'click', this.moveToPage.bind(this, -1, false));
        $.on('.dotable-btn-next', 'click', this.moveToPage.bind(this, 1, false));
        $.on('.dotable-btn-last', 'click', this.moveToPage.bind(this, 1, true));

        $.on('.dotable-area', 'scroll', this.onScroll.bind(this));

        this.setLayout();
    };

    /**
     * Build the view that actually shows the table.
     */
    doTable.prototype.draw = function() {
        var contentNode = this.getContainer();
        var container = $.get('.dash-table', contentNode);
        if (!container) {
            this.create();
        }

        if (this.opts.editable) {
            // update column sort icons
            var sortArrows = $.getAll('.dotable-arrow', $.get('.dotable-head', container));
            if (sortArrows && sortArrows.length) {
                $.forEach(sortArrows, function(x) {
                    var val = $.findByKey(this.opts.columns, 'field', x.getAttribute('data-field'));
                    if (val && val.sortDir) {
                        $.removeClass(x, 'dash-sort-up');
                        $.removeClass(x, 'dash-sort-down');
                        $.removeClass(x, 'dash-sort');
                        if (val.sortDir === 'ASC') {
                            $.addClass(x, 'dash-sort-up');
                        } else {
                            $.addClass(x, 'dash-sort-down');
                        }
                    } else {
                        $.removeClass(x, 'dash-sort-up');
                        $.removeClass(x, 'dash-sort-down');
                        $.addClass(x, 'dash-sort');
                    }
                }, this);
            }
        }

        // toggle disabled status for pagination buttons
        disableIf($.get('.dotable-btn-first', contentNode), this.currentStartItem === 0);
        disableIf($.get('.dotable-btn-previous', contentNode), this.currentStartItem === 0);
        disableIf($.get('.dotable-btn-next', contentNode), this.currentStartItem >= this.filteredTotal - this.itemsPerPage);
        disableIf($.get('.dotable-btn-last', contentNode), this.currentStartItem >= this.filteredTotal - this.itemsPerPage);

        // set values for showing `x - x of x` rows
        $.setText($.get('.dotable-start-item', contentNode), this.filteredTotal ? this.currentStartItem + 1 : 0);
        $.setText($.get('.dotable-end-item', contentNode), this.currentEndItem);
        $.setText($.get('.dotable-total-items', contentNode), this.filteredTotal);

        // update table body
        var body = $.get('.dotable-body', contentNode);
        if (body) {
            body.innerHTML = '';

            if (this.loading) {
                body.innerHTML = _templates.loadingFn(this.opts.columns.length);
            } else if (this.loadingError) {
                body.innerHTML = _templates.errorFn(this.opts.columns.length);
                $.on($.get('.dotable-btn-refresh', body), 'click', this.refresh.bind(this));
            } else if (this.filteredTotal === 0) {
                body.innerHTML = _templates.noDataFn(this.opts.columns.length);
            } else {
                var bodyHTML = '';
                var rows = $.map(this.results, doTable.prototype.makeRow.bind(this));
                $.forEach(rows, function(x) {
                    bodyHTML += this(x);
                }, this.opts.rowTemplateFn);
                body.innerHTML = bodyHTML;
            }
        }

        this.updateLayout();
    };

    doTable.prototype.makeRow = function(obj) {
        var newObj = $.clone(obj);
        $.forEach(this.opts.columns, function(x) {
            if (newObj.hasOwnProperty(x.field)) {
                newObj[x.field] = this(newObj[x.field], x.dataType);
            }
        }, this.opts.displayValueFn);
        return newObj;
    };

    doTable.prototype.destroy = function() {
        if (this.opts.editable) {
            $.off(window, 'resize', this.events.resize);
            $.off(window, 'mousemove', this.events.move);
            $.off(window, 'mouseup', this.events.up);
        }
    };

    return doTable;
});
