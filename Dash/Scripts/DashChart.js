/*!
 * Wraps functionality needed for creating charts using Chart.js.
 */
(function(root, factory) {
    root.DashChart = factory(root.$, root.Chart, root.flatpickr);
})(this, function($, Chart, flatpickr) {
    'use strict';

    /**
     * Declare DashChart class.
     * @param {Node} content - DOM node that contains the chart.
     * @param {bool} showLegend - Show or hide the chart legend.
     */
    var DashChart = function(content, showLegend) {
        this.content = content;
        this.url = content.getAttribute('data-url');
        this.canvas = $.get('.chart-canvas', content);
        this.showLegend = $.coalesce(showLegend, true);
        this.initDate = new Date();
        this.chart = null;
        this.run();
    };

    DashChart.prototype = {
        /**
         * Create the chart.
         */
        run: function() {
            var self = this;

            $.show($.get('.chart-spinner', self.content));
            $.hide($.get('.chart-error', self.content));
            $.hide(self.canvas.parentNode);

            $.ajax({
                method: 'POST',
                url: self.url,
                block: false
            }, function(data) {
                // destroy existing chart if any
                self.destroy();

                $.hide($.get('.chart-spinner', self.content));
                $.hide($.get('.chart-error', self.content));
                $.show(self.canvas.parentNode);

                if (data.updatedDate && new Date(data.updatedDate) > self.initDate) {
                    $.content.forceRefresh();
                    return;
                }
                if ($.isNull(data.ranges) || data.ranges.length === 0) {
                    $.show($.get('.chart-error', self.content));
                    return;
                }
                var ranges = $.isArray(data.ranges) ? data.ranges : [data];
                if (!ranges.some(function(x) { return x.rows && x.rows.length; })) {
                    $.show($.get('.chart-error', self.content));
                    return;
                }

                var chartType = data.type.toLowerCase();
                var isRadial = chartType === 'pie' || chartType === 'doughnut' || chartType === 'polararea';
                var datasets = [];

                $.forEach(ranges, function(range) {
                    var type = range.xType.toLowerCase();
                    if (type === 'currency') {
                        var currencyFormat = $.accounting.parseFormat(range.currencyFormat);
                        range.labels = $.map(range.labels, function(x) {
                            return $.accounting.formatMoney(x, currencyFormat);
                        });
                    } else if (type === 'date') {
                        var dateFormat = range.dateFormat;
                        range.labels = $.map(range.labels, function(x) {
                            // replace space with `T` to make date match iso8601 as closely as possible, after that browsers should be able to parse to a Date object safely
                            return flatpickr.formatDate(new Date(x.replace(' ', 'T')), dateFormat);
                        });
                    }

                    if (isRadial) {
                        $.forEach(range.rows, function(row, j) {
                            datasets.push({ value: row, label: this.labels[j] });
                        }, range);
                    } else {
                        datasets.push({
                            data: range.rows,
                            label: range.yTitle,
                            fillColor: range.color,
                            strokeColor: range.color,
                            pointColor: range.color,
                            pointStrokeColor: '#fff',
                            pointHighlightFill: '#fff',
                            pointHighlightStroke: range.color,
                        });
                    }
                });

                var labels = ranges[0].labels;
                var options = {
                    showXLabels: labels.length > 20 ? 20 : true
                };

                var chart = new Chart(self.canvas.getContext('2d'));
                switch (chartType) {
                    case 'horizontalbar':
                        self.chart = chart.HorizontalBar({ labels: labels, datasets: datasets }, options);
                        break;
                    case 'bar':
                        self.chart = chart.Bar({ labels: labels, datasets: datasets }, options);
                        break;
                    case 'radar':
                        self.chart = chart.Radar({ labels: labels, datasets: datasets }, options);
                        break;
                    case 'pie':
                        self.chart = chart.Pie(datasets, options);
                        break;
                    case 'doughnut':
                        self.chart = chart.Doughnut(datasets, options);
                        break;
                    case 'polararea':
                        self.chart = chart.PolarArea(datasets, options);
                        break;
                    default:
                        self.chart = chart.Line({ labels: labels, datasets: datasets }, options);
                        break;
                }

                if (self.chart && self.showLegend) {
                    var legend = self.chart.generateLegend();
                    var parent = self.canvas.parentNode.parentNode;
                    var existingLegend = $.get('.chart-legend', parent);
                    if (existingLegend) {
                        parent.replaceChild($.createNode(legend), existingLegend);
                    } else {
                        parent.appendChild($.createNode(legend));
                    }
                }
            }, function() {
                $.hide($.get('.chart-spinner', self.content));
                $.show($.get('.chart-error', self.content));
                $.hide(self.canvas.parentNode);
            });
        },

        /**
         * Destroy the chart object.
         */
        destroy: function() {
            $.destroy(this.chart);
        }
    };

    return DashChart;
});
