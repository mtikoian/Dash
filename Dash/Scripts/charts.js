/*!
 * Chart range form component.
 */
(function(root, factory) {
    root.RangeForm = factory(root.m, root.$, root.Form, root.Help, root.Alertify, root.DashChart, root.ColorPicker);
})(this, function(m, $, Form, Help, Alertify, DashChart, ColorPicker) {
    'use strict';

    function RangeForm(opts) {
        var ranges = opts.ranges || [];
        ranges = opts.ranges.map(function(x) { x.isExpanded = false; return x; });

        Form.call(this, {
            container: $.get('.range-table-wrapper', opts.content),
            columns: {
                id: { type: 'int' },
                reportId: { type: 'int' },
                xAxisColumnId: { type: 'int' },
                yAxisColumnId: { type: 'int' },
                aggregatorId: { type: 'int' },
                dateIntervalId: { type: 'int' },
                color: { type: 'string' },
                displayOrder: { type: 'int' }
            },
            appendRecord: true,
            allowEdit: opts.allowEdit,
            wantsHelp: opts.wantsHelp,
            newRecord: { id: 0, reportId: 0, xAxisColumnId: 0, yAxisColumnId: 0, aggregatorId: 0, dateIntervalId: 0, color: '', displayOrder: 0 }
        }, ranges || []);

        this.content = opts.content;
        this.chartId = opts.chartId;
        this.filterTypes = opts.filterTypes || {};
        this.saveRangesUrl = opts.saveRangesUrl;
        this.dateIntervals = opts.dateIntervals || [];
        this.aggregators = opts.aggregators || [];
        this.reports = opts.reports || [];
        this.columns = opts.columns || [];
        this.processJsonFn = opts.processJsonFn;
        this.canExportFn = opts.canExportFn;
        this.toggleExportFn = opts.toggleExportFn;
    }

    RangeForm.prototype = Object.create(Form.prototype);
    RangeForm.prototype.constructor = RangeForm;

    /**
     * Save ranges to the server then rebuild the chart.
     */
    RangeForm.prototype.saveRanges = function() {
        if ($.getAll('.mform-control-error', this.container).length > 0) {
            Alertify.error($.resx('fixIt'));
            return;
        }

        this.records.forEach(function(x, i) { x.displayOrder = i; });

        var self = this;
        $.ajax({
            method: 'POST',
            url: self.saveRangesUrl,
            data: {
                Id: self.chartId,
                Ranges: $.toPascalKeys(self.records)
            }
        }, function(data) {
            if (data) {
                self.changed = false;
                if ($.isArray(data.ranges)) {
                    data.ranges.forEach(function(x, i) { self.records[i].id = x.id; });
                }
                Alertify.success($.resx('chart.saveSuccessful'));
                self.makeChart();
            }
        });
    };

    RangeForm.prototype.makeChart = function() {
        var chartContent = $.get('.chart-container', this.content);
        if (!(chartContent && chartContent.hasAttribute('data-url'))) {
            return;
        }
        if (!(this.records && this.records.length > 0)) {
            Alertify.error($.resx('chart.rangesRequired'));
            return;
        }

        if (this.dashChart) {
            this.dashChart.destroy();
        }

        this.dashChart = new DashChart(chartContent, true, this.processJsonFn, null, this.toggleExportFn);
    };

    RangeForm.prototype.exportChart = function() {
        if (!this.canExportFn()) {
            return;
        }
        var chartContainer = $.get('.chart-container', this.content);
        if (chartContainer) {
            $.get('.export-width', this.content).value = chartContainer.offsetWidth;
            $.get('.export-data', this.content).value = this.dashChart.chart.toBase64Image();
            $.get('.export-form', this.content).submit();
        }
    };

    /**
     * Set the operatorId for a filter and unset the criteria.
     * @param {number} index - Index of the filter to update.
     * @param {Event} e - Event that triggered.
     */
    RangeForm.prototype.setReport = function(index, e) {
        if (this.set(index, 'reportId', e)) {
            this.records[index].xAxisColumnId = 0;
            this.records[index].yAxisColumnId = 0;
            this.records[index].aggregatorId = 0;
            this.records[index].dateIntervalId = 0;
            this.run();
        }
    };

    /**
     * Expand/collapse the inputs for a range.
     * @param {Object} record - Range object.
     */
    RangeForm.prototype.toggleExpanded = function(record) {
        record.isExpanded = !record.isExpanded;
    };

    /**
     * Build the ranges.
     * @returns {Object[]} Array of Mithril nodes containing the ranges.
     */
    RangeForm.prototype.rangeView = function() {
        if (!this.reports) {
            return null;
        }

        var self = this;
        return this.records.map(function(record, index) {
            var report = record.reportId ? $.findByKey(self.columns, 'reportId', record.reportId) : null;
            if (report && !report.xColumns) {
                report.xColumns = $.clone(report.columns);
                report.xColumns.unshift({ columnId: 0, title: $.resx('chart.xAxisColumn') });
                report.yColumns = $.clone(report.columns);
                report.yColumns.unshift({ columnId: 0, title: $.resx('chart.yAxisColumn') });
            }

            var dateIntervalDisabled = true;
            var inputsDisabled = !(self.opts.allowEdit && report);
            if (!inputsDisabled && self.filterTypes) {
                var col = $.findByKey(report.columns, 'columnId', record.xAxisColumnId * 1);
                if (col && col.filterTypeId === self.filterTypes.date) {
                    // show for date
                    dateIntervalDisabled = false;
                }
            }

            return m('.container.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even', key: record._index }, [
                m('.columns', [
                    m('input', { type: 'hidden', name: 'ChartRange[' + index + '].Id', value: record.id }),
                    m('.col-4',
                        self.withHelp($.resx('chart.reportText'), [
                            m('span.input-group-addon.input-group-custom', m('button.btn.btn-secondary', { onclick: self.toggleExpanded.bind(self, record) },
                                m('i.dash.text-primary', { class: record.isExpanded ? 'dash-minus' : 'dash-plus' })
                            )),
                            m('select.form-select.required', self.withDisabled({
                                name: 'ChartRange[' + index + '].ReportId', class: self.withError(record.reportId, true),
                                placeholder: $.resx('chart.report'), onchange: self.setReport.bind(self, index), value: record.reportId
                            }, !self.opts.allowEdit), self.withOptions(self.reports, record.reportId, 'id', 'name'))
                        ])
                    ),
                    m('.col-3',
                        self.withHelp($.resx('chart.xAxisColumnText'),
                            m('select.form-select.required', self.withDisabled({
                                name: 'ChartRange[' + index + '].XAxisColumnId', id: 'xAxisColumnId' + index, class: report ? self.withError(record.xAxisColumnId, true) : null,
                                placeholder: $.resx('chart.xAxisColumn'), onchange: self.set.bind(self, index, 'xAxisColumnId'), value: record.xAxisColumnId
                            }, inputsDisabled), self.withOptions(report ? report.xColumns : [{ columnId: 0, title: $.resx('chart.xAxisColumn') }], record.xAxisColumnId, 'columnId', 'title'))
                        )
                    ),
                    m('.col-3',
                        self.withHelp($.resx('chart.yAxisColumnText'),
                            m('select.form-select.required', self.withDisabled({
                                name: 'ChartRange[' + index + '].YAxisColumnId', id: 'yAxisColumnId' + index, class: report ? self.withError(record.yAxisColumnId, true) : null,
                                placeholder: $.resx('chart.yAxisColumn'), onchange: self.set.bind(self, index, 'yAxisColumnId'), value: record.yAxisColumnId
                            }, inputsDisabled), self.withOptions(report ? report.yColumns : [{ columnId: 0, title: $.resx('chart.yAxisColumn') }], record.yAxisColumnId, 'columnId', 'title'))
                        )
                    ),
                    m('.col-2', self.opts.allowEdit ? self.buttonView.call(self, index, true) : null)
                ]),
                m('.columns', { class: record.isExpanded ? '' : ' d-none' }, [
                    m('.col-4.col-ml-auto',
                        self.withHelp($.resx('chart.aggregatorText'),
                            m('select.form-select', self.withDisabled({
                                name: 'ChartRange[' + index + '].AggregatorId', id: 'aggregatorId' + index,
                                placeholder: $.resx('chart.aggregator'), onchange: self.set.bind(self, index, 'aggregatorId'), value: record.aggregatorId
                            }, inputsDisabled), self.withOptions(self.aggregators, record.aggregatorId, 'id', 'name'))
                        )
                    ),
                    m('.col-4',
                        self.withHelp($.resx('chart.dateIntervalText'),
                            m('select.form-select', self.withDisabled({
                                name: 'ChartRange[' + index + '].DateIntervalId', id: 'dateIntervalId' + index,
                                placeholder: $.resx('chart.dateInterval'), onchange: self.set.bind(self, index, 'dateIntervalId'), value: record.dateIntervalId
                            }, dateIntervalDisabled), self.withOptions(self.dateIntervals, record.dateIntervalId, 'id', 'name'))
                        )
                    ),
                    m('.col-2', [
                        self.withHelp($.resx('chart.colorText'),
                            m(ColorPicker, {
                                name: 'ChartRange[' + index + '].Color', value: record.color, disabled: inputsDisabled,
                                onSelect: self.set.bind(self, index, 'color')
                            })
                        ),
                    ]),
                    m('.col-1')
                ])
            ]);
        });
    };

    /**
     * Build the range form.
     * @returns {Object} Mithril node containing the form.
     */
    RangeForm.prototype.view = function() {
        return [
            this.rangeView(),
            m('.container.pt-1', m('.columns', [
                m('.col-6', [
                    m('.btn-toolbar', [
                        this.opts.allowEdit ? m('button.btn.btn-primary.mr-1', {
                            type: 'button', role: 'button', onclick: this.saveRanges.bind(this)
                        }, $.resx('save')) : null,
                        m('button.btn.btn-info', {
                            type: 'button', role: 'button', onclick: this.exportChart.bind(this), disabled: !this.canExportFn()
                        }, $.resx('export'))
                    ])
                ]),
                this.opts.allowEdit ? m('.col-6.text-right', [
                    m('.text-right', [
                        m('button.btn.btn-info.mr-1', {
                            type: 'button', role: 'button', onclick: this.addRecord.bind(this)
                        }, $.resx('add')),
                        m('button.btn.btn-warning.mr-1', {
                            type: 'button', role: 'button', onclick: this.deleteAllRecords.bind(this), disabled: !this.hasRecords()
                        }, $.resx('deleteAll')),
                        m(Help, { enabled: this.opts.wantsHelp, message: $.resx('chart.rangeText') })
                    ])
                ]) : null
            ]))
        ];
    };

    return RangeForm;
});

/*!
 * Chart form component.
 */
(function(root, factory) {
    root.ChartDetails = factory(root.$, root.Alertify, root.BaseDetails, root.RangeForm);
})(this, function($, Alertify, BaseDetails, RangeForm) {
    'use strict';

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

    ChartDetails.prototype.toggleExport = function(val) {
        this.enableExport = val;
    };

    ChartDetails.prototype.destroy = function() {
        $.destroy(this.rangeForm);
        $.destroy(this.chart);
    };

    return ChartDetails;
});

/*!
 * Functionality for displaying chart forms.
 */
(function($, Alertify, Chart, ChartDetails, ShareForm) {
    'use strict';

    // Change chart settings.
    Chart.defaults.global.responsive = true;
    Chart.defaults.global.maintainAspectRatio = false;
    Chart.defaults.global.title.display = false;
    Chart.defaults.global.legend.position = 'bottom';
    Chart.defaults.global.legend.labels.fontSize = 16;
    Chart.defaults.global.legend.labels.fontFamily = 'Calibri';
    Chart.defaults.global.layout = { padding: 10 };
    Chart.defaults.scale.ticks.fontFamily = 'Calibri';
    Chart.defaults.scale.ticks.fontSize = 12;

    var _charts = {};
    var _shares = {};

    /**
     * Request settings to display a chart and call the method to initialize it.
     */
    $.on(document, 'chartLoad', function(e) {
        var form = $.isNode(e) ? e : e.target;
        if (!$.hasClass(form, 'chart-form')) {
            return;
        }

        var id = form.getAttribute('data-id');
        $.ajax({
            method: 'GET',
            url: form.getAttribute('data-url')
        }, function(data) {
            data.content = form;
            _charts[id] = new ChartDetails(data);
        });
    }, true);

    /**
     * Clean up when closing the chart dialog.
     */
    $.on(document, 'chartUnload', function(e) {
        var form = $.isNode(e) ? e : e.target;
        var id = form.getAttribute('data-id');
        var chart = _charts[id];
        if (chart) {
            chart.destroy();
        }
        delete _charts[id];
    }, true);

    /**
     * Load the settings to display the chart share form.
     */
    $.on(document, 'chartShareLoad', function(e) {
        var form = $.isNode(e) ? e : e.target;
        if (!$.hasClass(form, 'chart-share-form')) {
            return;
        }

        var id = form.getAttribute('data-id');
        _shares[id] = new ShareForm({ content: form, formName: 'Shares' });
        _shares[id].run();
    }, true);

    /**
     * Clean up when the chart share dialog closes.
     */
    $.on(document, 'chartShareUnload', function(e) {
        var form = $.isNode(e) ? e : e.target;
        var id = form.getAttribute('data-id');
        var share = _shares[id];
        if (share) {
            share.destroy();
        }
        delete _shares[id];
    }, true);
})(this.$, this.Alertify, this.Chart, this.ChartDetails, this.ShareForm);
