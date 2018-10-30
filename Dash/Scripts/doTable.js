/*!
 * doT based table component. Supports ajax data, searching, sorting, paging, & resizing columns.
 */
(function(root, factory) {
    root.doTable = factory(root.doT, root.$, root.flatpickr);
})(this, function(doT, $, flatpickr) {
    'use strict';

    /**
    * Store the systemwide templates so we don't have to keep loading them.
    */
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

    /**
     * Get the case insensitive value for a field to use when sorting.
     * @param {string} value - Value to clean up.
     * @returns {string} New value to use for sorting.
     */
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

    /**
     * Format a field value for display.
     * @param {string} displayCurrencyFormat - Format to use for currency.
     * @param {string} displayDateFormat - Format to use for dates.
     * @param {string} value - Value to format.
     * @param {string} dataType - Datatype of column.
     * @returns
     */
    var getDisplayValue = function(displayCurrencyFormat, displayDateFormat, value, dataType) {
        if (!dataType || $.isNull(value)) {
            return value;
        }

        var val = value;
        if (dataType === 'currency') {
            val = $.accounting.formatMoney(val, displayCurrencyFormat);
        } else if (dataType === 'date') {
            val = flatpickr.formatDate(val, displayDateFormat);
        }
        return val;
    };

    /**
     * Convert a name with dashes to camel case. 
     * @param {String} str - String to format
     * @returns {String} Updated string
     */
    var camelCase = function(str) {
        var replaceFunc = function(all, letter) {
            return letter.toUpperCase();
        };
        return str.replace(/-([a-z])/ig, replaceFunc);
    };

    /**
     * Read data attributes from a node.
     * @param {Node} node - Node to get attributes from.
     * @returns {Object} Options object containing data attribute values.
     */
    var parseAttributes = function(node) {
        if (!node) {
            return {};
        }
        var attributes = node.attributes;
        var opts = { id: node.id };
        for (var i = 0; i < attributes.length; ++i) {
            var name = attributes[i].name;
            if (name.toLowerCase().indexOf('data-') === 0) {
                var value = attributes[i].value;
                if (['true', 'false'].indexOf(value.toLowerCase()) !== -1) {
                    value = value.toLowerCase() === 'true';
                } else if (!isNaN(value)) {
                    value = value * 1;
                }
                opts[camelCase(name.replace('data-', ''))] = value;
            }
        }
        if (node.hasAttribute('json-request-params')) {
            try {
                opts.requestParams = JSON.parse(node.getAttribute('json-request-params'));
            } catch (e) {
                // placeholder
            }
        }
        return opts;
    };

    /**
     * Build options for the table by combining attributes from the node with default values.
     * @param {Node} node - DOM node to pull attributes from.
     * @returns {Object} Options object.
     */
    var buildOpts = function(node) {
        return $.extend({
            id: null,
            columns: [],
            resultUrl: '',
            requestParams: {},
            searchable: true,
            loadAll: true,
            columnMinWidth: 50,
            width: 100,
            editable: true,
            storeUrl: null,
            itemsPerPage: null,
            searchQuery: null,
            dataDateFormat: 'Y-m-d H:i:S',
            displayDateFormat: 'Y-m-d H:i:S',
            displayCurrencyFormat: '{s:$} {[t:,][d:.][p:2]}',
            checkUpdateDate: false
        }, parseAttributes(node));
    };

    /**
     * Create the table component.
     * @param {Node} node - DOM Node that will contain the table.
     */
    var doTable = function(node) {
        node.doTable = this;
        this.opts = buildOpts(node);
        this.setDefaults(node);
        this.parseColumns();
        this.create();
        this.update();
        this.loadData();
    };

    /**
     * Add the default settings for the table.
     * @param {Node} node - DOM node to pull attributes from.
     */
    doTable.prototype.setDefaults = function(node) {
        this.data = null;
        this.loading = true;
        this.loadingError = false;
        this.filteredTotal = 0;
        this.results = [];
        this.pageTotal = 0;
        this.totalDistance = 0;
        this.lastSeenAt = { x: null, y: null };
        this.intColumns = [];
        this.dateColumns = [];
        this.currencyColumns = [];
        this.storeFunction = null;
        this.initDate = new Date();

        var storeUrl = this.opts.storeUrl;
        if (storeUrl) {
            this.storeFunction = $.debounce(function(data) {
                $.ajax({
                    url: storeUrl,
                    method: 'POST',
                    data: data
                });
            }, 250);
        }

        var template = $.get(node.getAttribute('data-template'));
        this.opts.rowTemplateFn = doT.template(template ? template.text : '');
        this.opts.displayValueFn = getDisplayValue.bind(null, this.opts.displayCurrencyFormat, this.opts.displayDateFormat);
        this.itemsPerPage = this.store('itemsPerPage') * 1 || 10;
        this.currentStartItem = this.store('currentStartItem') * 1 || 0;
        this.currentEndItem = 0;
        this.searchQuery = this.store('searchQuery') || '';
        this.width = this.store('width') * 1 || 100;
    };

    /**
     * Parse sorting settings.
     * @returns {Object[]} Array of sorting objects.
     */
    doTable.prototype.parseSorting = function() {
        var sorting = this.store('sorting');
        var sortColumns = [];
        if (sorting) {
            try {
                sortColumns = (typeof sorting === 'string' ? JSON.parse(sorting) : sorting) || [];
            } catch (e) {
                // placeholder
            }
        }
        return sortColumns;
    };

    /**
     * Parse row template to get column properties.
     */
    doTable.prototype.parseColumns = function() {
        var sortColumns = this.parseSorting();
        var tempNode = $.createNode();
        tempNode.innerHTML = '<table>' + this.opts.rowTemplateFn({}) + '</table>';

        $.forEach($.getAll('td', tempNode), function(x) {
            var field = x.getAttribute('data-field');
            var width = x.getAttribute('data-width');
            width = isNaN(width) ? null : width * 1;

            var type = x.getAttribute('data-type').toLowerCase();
            if (type === 'int') {
                this.intColumns.push(field);
            } else if (type === 'date') {
                this.dateColumns.push(field);
            } else if (type === 'currency') {
                this.currencyColumns.push(field);
            }

            var column = {
                width: $.hasPositiveValue(width) ? width : this.store(field + '.width'),
                sortable: x.getAttribute('data-sortable').toLowerCase() === 'true',
                label: x.getAttribute('data-label'),
                field: field,
                dataType: type
            };

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
    };

    /**
    * Get/set persistent values.
    * @param {string} key - Key name of the value to get/set.
    * @param {*} value - Value to set.
    * @returns {string|undefined} Value if getting, else undefined.
    */
    doTable.prototype.store = function(key, value) {
        var myKey = this.opts.id + '.' + key;
        if (!this.opts.editable) {
            return $.coalesce(this.opts[key], null);
        }
        // getter
        if ($.isUndefined(value)) {
            return $.isNull(this.opts.storeUrl) ? localStorage[myKey] : $.coalesce(this.opts[key], null);
        }

        // setter
        if ($.isNull(this.storeFunction)) {
            localStorage[myKey] = value;
        } else {
            var data = $.extend(this.opts.requestParams, {
                itemsPerPage: this.itemsPerPage,
                currentStartItem: this.currentStartItem,
                searchQuery: this.searchQuery,
                width: this.width,
                sorting: this.buildSortList(),
                columns: $.map(this.opts.columns, function(c) { return { field: c.field, width: c.width * 1.0 }; })
            });
            this.storeFunction.call(null, data);
        }
    };

    /**
     * Process the data array result from the ajax request.
     * @param {Object[]} data - Array of records to display.
     */
    doTable.prototype.processData = function(data) {
        if (this.opts.checkUpdateDate && data.updatedDate) {
            if (new Date(data.updatedDate) > this.initDate) {
                // underlying table has changed so we need to reload the page.
                $.content.forceRefresh();
                return;
            }
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
                data.rows[i][x] = $.isNull(data.rows[i][x]) ? null : flatpickr.parseDate(data.rows[i][x], this.opts.dataDateFormat);
            }
            for (j = 0; j < this.currencyColumns.length; j++) {
                x = this.currencyColumns[j];
                data.rows[i][x] = $.isNull(data.rows[i][x]) ? null : $.accounting.unformat(data.rows[i][x]);
            }
        }
        this.data = data.rows;
        this.filteredTotal = data.filteredTotal;
        this.currentEndItem = Math.min(this.currentStartItem + this.itemsPerPage, this.filteredTotal);
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
            method: 'POST',
            url: this.opts.resultUrl,
            data: this.buildParams(),
            block: false
        }, this.processData.bind(this), function() {
            self.loading = false;
            self.loadingError = true;
            self.update();
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
        this.currentEndItem = Math.min(this.currentStartItem + this.itemsPerPage, this.filteredTotal);
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
            //this.currentEndItem = Math.min(this.currentStartItem + this.itemsPerPage, this.filteredTotal);
            this.pageTotal = Math.ceil(this.filteredTotal / this.itemsPerPage);
            this.update();
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
            this.update();
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
            this.update();
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
        this.update();
    };

    /**
     * Sort the underlying data.
     * @param {bool} refresh - Refresh the data from the server.
     */
    doTable.prototype.sort = function(refresh) {
        var sortColumns = this.buildSortList();
        this.data.sort(sortColumns && sortColumns.length > 0 ? compare.bind(sortColumns) : defaultCompare);
        this.store('sorting', JSON.stringify(sortColumns));
        this.filterResults($.coalesce(refresh, true));
    };

    /**
     * Set up the table and column styles and events.
     */
    doTable.prototype.setLayout = function() {
        var table = this.getTable();
        if (table !== null) {
            table.style.tableLayout = 'fixed';
            this.clientWidth = this.getContainer().clientWidth;
            table.tHead.style.width = table.style.width = (this.width / 100 * table.offsetWidth) + 'px';

            var hWidth = table.tHead.offsetWidth;
            var tWidth = table.offsetWidth;
            var i = 0;
            var cells = table.tHead.rows[0].cells;
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
     * Update the table header style.
     */
    doTable.prototype.updateLayout = function() {
        var table = this.getTable();
        if (!$.isVisible(table)) {
            return;
        }
        var contentNode = this.getContainer();
        $.get('.dotable-scrollable', contentNode).style.paddingTop = table.tHead.offsetHeight + 'px';
        var colGroup = $.get('.dotable-column-group', contentNode);
        var tableHeaderRow = this.getTableHeaderRow();
        for (var i = 0; i < this.opts.columns.length; i++) {
            colGroup.children[i].style.width = tableHeaderRow.cells[i].style.width;
        }
        if (this.clientWidth > 0 && contentNode.clientWidth / this.clientWidth !== 1) {
            this.onResize();
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
        var table = this.getTable();
        table.tHead.style.width = table.style.width = (pixelToFloat(table.style.width) * scale) + 'px';

        var tableHeaderRow = this.getTableHeaderRow();
        var cell;
        for (var i = 0; i < this.opts.columns.length; i++) {
            cell = tableHeaderRow.cells[i];
            cell.style.width = (pixelToFloat(cell.style.width) * scale) + 'px';
        }
        this.updateLayout();
    };

    /**
     * Make the table header scroll horizontally with the table
     * @param {Event} e - Event that triggered the scroll.
     */
    doTable.prototype.onScroll = function(e) {
        var head = this.getTable().tHead;
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
            var table = this.getTable();
            self.resizeContext = {
                colIndex: cellEl.cellIndex,
                initX: e.clientX,
                scrWidth: $.get('.dotable-scrollable', contentNode).offsetWidth,
                initTblWidth: table.offsetWidth,
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
        var table = this.getTable();
        if (table.tHead.style.cursor !== newStyle) {
            table.tHead.style.cursor = newStyle;
        }

        var ctx = this.resizeContext;
        if ($.isNull(ctx)) {
            return;
        }

        e.stopImmediatePropagation();
        e.preventDefault();

        var newColWidth = Math.max(ctx.initColWidth + e.clientX - ctx.initX, this.opts.columnMinWidth);
        table.tHead.style.width = table.style.width = (ctx.initTblWidth + (newColWidth - ctx.initColWidth)) + 'px';

        var tableHeaderRow = this.getTableHeaderRow();
        $.get('.dotable-column-group', this.getContainer()).children[ctx.colIndex].style.width = tableHeaderRow.cells[ctx.colIndex].style.width = newColWidth + 'px';

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

        var table = this.getTable();
        var tableHeaderRow = this.getTableHeaderRow();
        var newTblWidth = table.offsetWidth;
        this.width = (newTblWidth / ctx.scrWidth * 100).toFixed(2);
        this.store('width', this.width);
        for (var i = 0; i < this.opts.columns.length; i++) {
            this.opts.columns[i].width = (pixelToFloat(tableHeaderRow.cells[i].style.width) / newTblWidth * 100).toFixed(2);
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
        var table = this.getTable();
        for (el = table.tHead; el !== null; el = el.offsetParent) {
            tblX -= el.offsetLeft + el.clientLeft - el.scrollLeft;
        }

        var cellEl = e.target;
        while (cellEl !== table.tHead && cellEl !== null) {
            if (cellEl.nodeName === 'TH') {
                break;
            }
            cellEl = cellEl.parentNode;
        }

        if (cellEl === table.tHead) {
            var cells = this.getTableHeaderRow().cells;
            for (var i = cells.length - 1; i >= 0; i--) {
                cellEl = cells[i];
                if (cellEl.offsetLeft <= tblX) {
                    break;
                }
            }
        }

        if (cellEl !== null) {
            var x = tblX;
            for (el = cellEl; el !== table.tHead; el = el.offsetParent) {
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

    /**
     * Helper to get the table container node.
     * @returns {Node} Container node reference.
     */
    doTable.prototype.getContainer = function() {
        return $.get('#' + this.opts.id);
    };

    /**
     * Helper to get the table node.
     * @returns {Node} Table node reference.
     */
    doTable.prototype.getTable = function() {
        return $.get('.dotable-data', this.getContainer());
    };

    /**
     * Helper to get the table header row node.
     * @returns {Node} Header row node reference.
     */
    doTable.prototype.getTableHeaderRow = function() {
        return $.get('.dotable-data', this.getContainer()).tHead.rows[0];
    };

    /**
     * Create the structure of the table and bind events.
     */
    doTable.prototype.create = function() {
        var contentNode = this.getContainer();
        contentNode.innerHTML = '';

        var container = $.createNode('<div class="dash-table"></div>');
        container.innerHTML = _templates.headerFn(this) + _templates.bodyFn(this) + _templates.footerFn(this);
        contentNode.appendChild(container);

        $.on($.get('.dotable-search-input', container), 'input', this.setSearchQuery.bind(this));
        $.on($.get('.dotable-items-input', container), 'change', this.setItemsPerPage.bind(this));
        $.on($.get('.dotable-btn-first', container), 'click', this.moveToPage.bind(this, -1, true));
        $.on($.get('.dotable-btn-previous', container), 'click', this.moveToPage.bind(this, -1, false));
        $.on($.get('.dotable-btn-next', container), 'click', this.moveToPage.bind(this, 1, false));
        $.on($.get('.dotable-btn-last', container), 'click', this.moveToPage.bind(this, 1, true));
        $.on($.get('.dotable-area', container), 'scroll', this.onScroll.bind(this));

        if (this.opts.editable) {
            // bind column sort and column resize events
            var thead = $.get('.dotable-head', container);
            if (thead) {
                var handler = this.touchHandler.bind(this);
                $.on(thead, 'touchstart', handler);
                $.on(thead, 'touchend', handler);
                $.on(thead, 'touchmove', handler);
                $.on(thead, 'touchcancel', handler);

                var mouseFunc = this.onHeaderMouseDown.bind(this);
                $.forEach($.getAll('th', thead), function(x) {
                    $.on(x, 'mousedown', mouseFunc);
                });

                $.forEach($.getAll('.dotable-arrow', thead), function(x) {
                    $.on(x, 'click', this.changeSort.bind(this, x.getAttribute('data-field'), x.getAttribute('data-type').toLowerCase()));
                }, this);
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

        this.setLayout();
    };

    /**
     * Process a single row making the formatted display values for each column.
     * @param {Object} obj - Row of data to process.
     * @returns {Object} New object with values ready to display.
     */
    doTable.prototype.makeRow = function(obj) {
        var newObj = $.clone(obj);
        $.forEach(this.opts.columns, function(x) {
            if (newObj.hasOwnProperty(x.field)) {
                newObj[x.field] = this(newObj[x.field], x.dataType);
            }
        }, this.opts.displayValueFn);
        return newObj;
    };

    /**
     * Update the table contents and other buttons/labels.
     */
    doTable.prototype.update = function() {
        var contentNode = this.getContainer();

        if (this.opts.editable) {
            // update column sort icons
            $.forEach($.getAll('.dotable-arrow', $.get('.dotable-head', $.get('.dash-table', contentNode))), function(x) {
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

        // toggle disabled status for pagination buttons
        $.disableIf($.get('.dotable-btn-first', contentNode), this.currentStartItem === 0);
        $.disableIf($.get('.dotable-btn-previous', contentNode), this.currentStartItem === 0);
        $.disableIf($.get('.dotable-btn-next', contentNode), this.currentStartItem >= this.filteredTotal - this.itemsPerPage);
        $.disableIf($.get('.dotable-btn-last', contentNode), this.currentStartItem >= this.filteredTotal - this.itemsPerPage);

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
                $.forEach($.map(this.results, doTable.prototype.makeRow.bind(this)), function(x) {
                    bodyHTML += this(x);
                }, this.opts.rowTemplateFn);
                body.innerHTML = bodyHTML;
            }
        }

        // set values for showing `x - x of x` rows
        $.setText($.get('.dotable-start-item', contentNode), this.filteredTotal ? this.currentStartItem + 1 : 0);
        $.setText($.get('.dotable-end-item', contentNode), this.currentEndItem);
        $.setText($.get('.dotable-total-items', contentNode), this.filteredTotal);

        this.updateLayout();
    };

    /**
     * Destroy the table. Remove bound events.
     */
    doTable.prototype.destroy = function() {
        if (this.opts.editable) {
            $.off(window, 'resize', this.events.resize);
            $.off(window, 'mousemove', this.events.move);
            $.off(window, 'mouseup', this.events.up);
        }
    };

    return doTable;
});
