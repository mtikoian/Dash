/*!
 * Wraps report form functionality.
 */
(function(root, factory) {
    root.ReportDetails = factory(root.m, root.$, root.Alertify, root.BaseDetails, root.FilterForm, root.GroupForm, root.Table);
})(this, function(m, $, Alertify, BaseDetails, FilterForm, GroupForm, Table) {
    'use strict';

    /**
     * Declare ReportDetails class.
     * @param {Object} opts - Report settings
     */
    function ReportDetails(opts) {
        opts = opts || {};

        BaseDetails.call(this, opts);

        if (!(opts.reportColumns && opts.reportColumns.length)) {
            return;
        }

        this.isProc = opts.loadAllData;

        var self = this;
        var saveUrl = opts.saveColumnsUrl;
        var saveStorageFunc = $.debounce(!opts.allowEdit ? $.noop : function(settings) {
            if ($.isNull(self.dataTable.previousColumnWidths) || !$.equals(settings.columnWidths, self.dataTable.previousColumnWidths)) {
                $.ajax({
                    method: 'PUT',
                    url: saveUrl,
                    data: {
                        Id: opts.reportId,
                        Columns: $.toPascalKeys(settings.columnWidths),
                        ReportWidth: settings.width * 1
                    },
                    block: false
                });
                self.dataTable.previousColumnWidths = settings.columnWidths;
            }
        }, 250);

        this.dataTable = new Table({
            content: $.get('.report-data-container', opts.content),
            url: opts.dataUrl,
            requestMethod: 'POST',
            requestParams: { Id: opts.reportId, Save: true },
            searchable: false,
            loadAllData: opts.loadAllData,
            editable: opts.allowEdit,
            headerButtons: [m('a.btn.btn-primary.mr-2', { href: opts.exportUrl, target: '_blank' }, $.resx('export'))],
            itemsPerPage: opts.rowLimit,
            currentStartItem: 0,
            sorting: opts.sortColumns || [],
            storageFunction: saveStorageFunc,
            width: opts.width,
            columns: opts.reportColumns || [],
            displayDateFormat: opts.dateFormat,
            displayCurrencyFormat: opts.currencyFormat,
            dataCallback: this.processJson.bind(this),
            errorCallback: this.processJson.bind(this)
        });
        this.dataTable.previousColumnWidths = opts.reportColumns.map(function(x) { return { field: x.field, width: x.width * 1.0 }; });

        this.filterForm = new FilterForm({
            reportId: opts.reportId,
            content: opts.content,
            filters: opts.filters,
            columns: {
                id: { type: 'int' },
                columnId: { type: 'int' },
                operatorId: { type: 'int' },
                criteria: { type: 'str' },
                criteria2: { type: 'str' }
            },
            appendRecord: true,
            allowEdit: opts.allowEdit,
            wantsHelp: opts.wantsHelp,
            newRecord: { id: null, columnId: '', operatorId: '', criteria: null, criteria2: null },
            dateFormat: opts.dateFormat,
            saveFiltersUrl: opts.saveFiltersUrl,
            filterOperators: opts.filterOperators,
            filterOperatorIds: opts.filterOperatorIds,
            filterTypes: opts.filterTypes,
            dateOperators: opts.dateOperators,
            lookups: opts.lookups,
            isProc: opts.isProc,
            dataTable: this.dataTable,
            columnFn: this.getColumnList.bind(this)
        });
        this.filterForm.run();

        this.groupForm = new GroupForm({
            reportId: opts.reportId,
            content: opts.content,
            groups: opts.groups,
            allowEdit: opts.allowEdit,
            wantsHelp: opts.wantsHelp,
            saveGroupsUrl: opts.saveGroupsUrl,
            aggregatorId: opts.aggregatorId,
            aggregator: opts.aggregator,
            aggregators: opts.aggregators,
            isProc: opts.isProc,
            dataTable: this.dataTable,
            columnFn: this.getColumnList.bind(this)
        });
        this.groupForm.run();
    }

    ReportDetails.prototype = Object.create(BaseDetails.prototype);
    ReportDetails.prototype.constructor = BaseDetails;

    /**
     * Handle the result of a query for report data.
     * @param {Object} json - Query result.
     * @returns {bool}  True if data is valid.
     */
    ReportDetails.prototype.processJson = function(json) {
        if (json.updatedDate && this.initDate) {
            var updateDate = new Date(json.updatedDate);
            if (updateDate && updateDate > this.initDate) {
                // report has been modified - warn the user to refresh
                Alertify.error($.resx('reportModified'));
                return false;
            }
        }

        if (json.dataSql) {
            this.setSql($.get('.sql-data-content', this.content), json.dataSql, json.dataError);
        }
        if (json.countSql) {
            this.setSql($.get('.sql-count-content', this.content), json.countSql, json.countError);
        }

        if ($.isNull(json.rows)) {
            Alertify.error($.resx('errorConnectingToDataSource'));
            return false;
        }
        return true;
    };

    /**
     * Clean up our mess.
     */
    ReportDetails.prototype.destroy = function() {
        $.destroy(this.dataTable);
        $.destroy(this.filterForm);
        $.destroy(this.groupForm);
    };

    return ReportDetails;
});
