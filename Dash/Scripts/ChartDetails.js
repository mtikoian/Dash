/*!
 * Wraps report form functionality.
 */
(function(root, factory) {
    root.ChartDetails = factory(root.$, root.Alertify, root.BaseDetails, root.RangeForm);
})(this, function($, Alertify, BaseDetails, RangeForm) {
    'use strict';

    /**
     * Declare ChartDetails class.
     * @param {Object} opts - Report settings
     */
    function ChartDetails(opts) {
        opts = opts || {};

        BaseDetails.call(this, opts);

        this.enableExport = false;

        this.rangeForm = new RangeForm({
            chartId: opts.chartId,
            content: opts.content,
            ranges: opts.ranges,
            allowEdit: opts.allowEdit,
            wantsHelp: opts.wantsHelp,
            filterTypes: opts.filterTypes || {},
            saveRangesUrl: opts.saveRangesUrl,
            dateIntervals: opts.dateIntervals,
            aggregators: opts.aggregators,
            reports: opts.reports,
            columns: opts.columns,
            processJsonFn: this.processJson.bind(this),
            canExportFn: this.canExport.bind(this),
            toggleExportFn: this.toggleExport.bind(this)
        });
        this.rangeForm.run();

        if (this.rangeForm.records && this.rangeForm.records.length) {
            this.rangeForm.makeChart();
        }
    }

    ChartDetails.prototype = Object.create(BaseDetails.prototype);
    ChartDetails.prototype.constructor = BaseDetails;

    /**
     * Handle the result of a query for report data.
     * @param {Object} json - Query result.
     * @returns {bool}  True if data is valid.
     */
    ChartDetails.prototype.processJson = function(json) {
        if (json.updatedDate && this.initDate) {
            var updateDate = new Date(json.updatedDate);
            if (updateDate && updateDate > this.initDate) {
                // chart has been modified - warn the user to refresh
                $.dispatch(document, $.events.chartLoad);
                Alertify.success($.resx('chartModified'));
                return false;
            }
        }

        if ($.isNull(json.ranges) || json.ranges.length === 0) {
            Alertify.error($.resx('errorConnectingToDataSource'));
            this.enableExport = false;
            $.show($.get('.chart-error', this));
            return false;
        }

        this.enableExport = true;
        $.get('.export-filename', this.content).value = json.title;

        var elem = $.get('.sql-content', this.content);
        var ranges = json.ranges.map(function(x) { return { title: x.title, sql: x.sql, error: x.error }; });
        this.setSql(elem, ranges.filter(function(x) { return x.sql; }).map(function(x) { return '-- ' + x.title + '\n' + x.sql + '\n'; }).join('\n'),
            ranges.filter(function(x) { return x.error; }).map(function(x) { return x.title + ':<br>' + x.error + '<br>'; }).join('<br>'));

        return true;
    };

    ChartDetails.prototype.canExport = function() {
        return this.enableExport;
    };

    /**
     * Set export flag.
     * @param {bool} val - Enable export if true, else disable.
     */
    ChartDetails.prototype.toggleExport = function(val) {
        this.enableExport = val;
    };

    ChartDetails.prototype.destroy = function() {
        $.destroy(this.rangeForm);
        $.destroy(this.chart);
    };

    return ChartDetails;
});
