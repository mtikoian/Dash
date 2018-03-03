/*!
 * Wraps functionality needed for creating charts using Chart.js.
 */
(function(root, factory) {
    root.RngnChart = factory(root.$, root.Alertify, root.Chart);
})(this, function($, Alertify, Chart) {
    'use strict';

    /**
     * Declare RngnChart class.
     * @param {Node} content - DOM node that contains the chart.
     * @param {Node} canvas - Canvas element to display the chart in.
     * @param {bool} showLegend - Show or hide the chart legend.
     * @param {Function} dataFn - Function to call if after loading data. Runs before other functions.
     * @param {Function} errorFn - Function to call if an error occurs loading data.
     * @param {Function} toggleExportFn - Function to enable/disable export.
     */
    var RngnChart = function(content, showLegend, dataFn, errorFn, toggleExportFn) {
        this.content = content;
        this.url = content.getAttribute('data-url');
        this.canvas = $.get('.chart-canvas', content);
        this.showLegend = $.coalesce(showLegend, true);
        this.dataFn = dataFn;
        this.errorFn = errorFn;
        this.toggleExportFn = toggleExportFn;
        this.chart = null;
        this.run();
    };

    /**
     * Declare RngnChart class methods.
     */
    RngnChart.prototype = {
        /**
         * Generate a random number within range.
         * @param {number[]} range - Min and max value.
         * @returns {number} Random number. 
         */
        randomWithin: function (range) {
            return Math.floor(range[0] + Math.random() * (range[1] + 1 - range[0]));
        },

        /**
         * Generates a random color and a lighter partner color.
         * @returns {string[]} Color codes.
         */
        randomColor: function () {
            var hsl = [this.randomWithin([0, 360]), this.randomWithin([50, 100]), this.randomWithin([30, 80])];
            return [
                'hsla(' + hsl[0] + ',' + hsl[1] + '%,' + hsl[2] + '%, 1)',
                'hsla(' + hsl[0] + ',' + hsl[1] + '%,' + hsl[2] + '%, .2)'
            ];
        },

        /**
         * Generates an array of random colors.
         * @param {number} cnt - Number of colors to generate.
         * @returns {string[]} Array of color codes.
         */
        randomColors: function (cnt) {
            var result = new Array(cnt);
            for (var i = 0; i < cnt; i++) {
                result[i] = this.randomColor()[0];
            }
            return result;
        },

        /**
         * Parse a hex color into a hsla color and a lighter partner color.
         * @param {string} hex - Hex color code with leading '#'.
         * @returns {string[]} Color codes.
         */
        parseColor: function (hex) {
            if (!hex) {
                return this.randomColor();
            }
            var hsl = $.colors.rgb2hsl($.colors.hex2rgb(hex));
            return [
                'hsla(' + (hsl[0] * 360).toFixed(0) + ',' + (hsl[1] * 100).toFixed(0) + '%,' + (hsl[2] * 100).toFixed(0) + '%, 1)',
                'hsla(' + (hsl[0] * 360).toFixed(0) + ',' + (hsl[1] * 100).toFixed(0) + '%,' + (hsl[2] * 100).toFixed(0) + '%, .2)'
            ];
        },

        /**
         * Build the chart.
         */
        run: function () {
            var self = this;

            $.show($.get('.chart-spinner', self.content));
            $.hide($.get('.chart-error', self.content));
            $.hide(self.canvas);
            if (self.toggleExportFn) {
                self.toggleExportFn(false);
            }

            $.ajax({
                method: 'POST',
                url: self.url,
                block: false
            }, function (data) {
                if ($.isFunction(self.dataFn)) {
                    if (!self.dataFn(data)) {
                        return;
                    }
                }

                var ranges = $.isArray(data.ranges) ? data.ranges : [data];
                if (!ranges.some(function(x) { return x.rows && x.rows.length; })) {
                    $.hide($.get('.chart-spinner', self.content));
                    Alertify.error($.resx('chart.noData'));
                    return;
                }

                $.hide($.get('.chart-spinner', self.content));
                $.hide($.get('.chart-error', self.content));
                $.show(self.canvas);
                if (self.toggleExportFn) {
                    self.toggleExportFn(true);
                }

                if (self.chart) {
                    // this code is used for refreshing dashboard chart data, but won't be used on the chart dialog
                    ranges.forEach(function (x, i) {
                        self.chart.data.datasets[i].data = ranges[i].rows;
                        self.chart.data.datasets[i].label = ranges[i].yTitle;
                    });
                    self.chart.data.labels = ranges[0].labels;
                    self.chart.update();
                } else {
                    var scales = {};
                    var tooltips = { callbacks: {} };
                    if (ranges[0].xType === 'currency') {
                        var currencyFormat = $.accounting.parseFormat(ranges[0].currencyFormat);
                        scales.xAxes = [{
                            ticks: {
                                callback: function (value) {
                                    return $.accounting.formatMoney(value, currencyFormat);
                                }
                            }
                        }];
                        tooltips.callbacks.title = function (tooltipItems, data) {
                            var title = '';
                            if (tooltipItems.length > 0) {
                                if (tooltipItems[0].xLabel) {
                                    title = tooltipItems[0].xLabel;
                                } else if (data.labels.length > 0 && tooltipItems[0].index < data.labels.length) {
                                    title = data.labels[tooltipItems[0].index];
                                }
                                title = $.accounting.formatMoney(title, currencyFormat);
                            }
                            return title;
                        };
                    } else if (ranges[0].xType === 'date') {
                        var dateFormat = ranges[0].dateFormat;
                        scales.xAxes = [{
                            ticks: {
                                callback: function (value) {
                                    return $.fecha.format(new Date(value), dateFormat);
                                }
                            }
                        }];
                        tooltips.callbacks.title = function (tooltipItems, data) {
                            var title = '';
                            if (tooltipItems.length > 0) {
                                if (tooltipItems[0].xLabel) {
                                    title = tooltipItems[0].xLabel;
                                } else if (data.labels.length > 0 && tooltipItems[0].index < data.labels.length) {
                                    title = data.labels[tooltipItems[0].index];
                                }
                                title = $.fecha.format(new Date(title), dateFormat);
                            }
                            return title;
                        };
                    }

                    var chartType = data.type.toLowerCase();
                    var isRadial = chartType === 'pie' || chartType === 'doughnut';
                    var datasets = [];
                    scales.yAxes = [];
                    ranges.forEach(function (x, i) {
                        var color = self.parseColor(x.color);
                        datasets.push({
                            backgroundColor: color[0],
                            borderColor: 'rgb(255, 255, 255)',
                            borderWidth: 2,
                            data: x.rows,
                            label: x.yTitle,
                            yAxisID: 'y-axis-' + i
                        });

                        var ticks = {};
                        if (x.yType === 'currency') {
                            var currencyFormat = $.accounting.parseFormat(x.currencyFormat);
                            ticks.callback = function (value) {
                                return $.accounting.formatMoney(value, currencyFormat);
                            };
                            tooltips.callbacks.label = function (item) {
                                return $.accounting.formatMoney(item.yLabel, currencyFormat);
                            };
                        } else if (x.yType === 'date') {
                            var dateFormat = x.dateFormat;
                            ticks.callback = function (value) {
                                return $.fecha.format(new Date(value), dateFormat);
                            };
                            tooltips.callbacks.label = function (item) {
                                return $.fecha.format(new Date(item.yLabel), dateFormat);
                            };
                        }

                        if (!isRadial) {
                            scales.yAxes.push({
                                id: 'y-axis-' + i,
                                position: i === 0 ? 'left' : 'right',
                                ticks: ticks,
                                gridLines: {
                                    color: color[1]
                                }
                            });
                        }
                    });

                    var chartData = {
                        type: chartType === 'horizontalbar' ? 'horizontalBar' : chartType,
                        options: {
                            responsive: true,
                            scales: isRadial ? null : scales,
                            tooltips: tooltips,
                            legend: { display: self.showLegend }
                        },
                        data: {
                            labels: ranges[0].labels,
                            datasets: datasets
                        }
                    };

                    if (isRadial) {
                        chartData.data.datasets[0].backgroundColor = self.randomColors(ranges[0].rows.length);
                    }

                    self.chart = new Chart(self.canvas.getContext('2d'), chartData);
                }
            }, function () {
                if ($.isFunction(self.errorFn)) {
                    if (!self.errorFn()) {
                        return;
                    }
                }
                $.hide($.get('.chart-spinner', self.content));
                $.show($.get('.chart-error', self.content));
                if (self.toggleExportFn) {
                    self.toggleExportFn(false);
                }
            });
        },

        /**
         * Destroy the chart.
         */
        destroy: function () {
            $.destroy(this.chart);
        }
    };

    return RngnChart;
});