/*!
 * Wraps functionality needed for creating charts using Chart.js.
 */
(function(root, factory) {
    root.DashChart = factory(root.$, root.Chart, root.flatpickr);
})(this, function($, Chart, flatpickr) {
    'use strict';

    var _defaultPalette = ['#0000FF', '#FE9200', '#F44E3B', '#FCDC00', '#00FF00', '#A4DD00', '#68CCCA',
        '#73D8FF', '#AEA1FF', '#FDA1FF', '#D33115', '#E27300', '#FCC400', '#B0BC00', '#68BC00',
        '#16A5A5', '#009CE0', '#7B64FF', '#FA28FF', '#9F0500', '#C45100', '#FB9E00',
        '#808900', '#194D33', '#0C797D', '#0062B1', '#653294', '#AB149E', '#4D4D4D'
    ];

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
        this.chart = null;
        this.initDate = new Date();
        this.run();
    };

    DashChart.prototype = {
        /**
         * Generate a random number within range.
         * @param {number[]} range - Min and max value.
         * @returns {number} Random number.
         */
        randomWithin: function(range) {
            return Math.floor(range[0] + Math.random() * (range[1] + 1 - range[0]));
        },

        /**
         * Generates a random color and a lighter partner color.
         * @param {Number} i - Index to use when picking color.
         * @returns {string[]} Color codes.
         */
        randomColor: function(i) {
            var seed = i ? i % _defaultPalette.length : this.randomWithin([0, _defaultPalette.length - 1]);
            var rgb = $.colors.hex2rgb(_defaultPalette[seed]);
            return [
                'rgba(' + rgb.r + ',' + rgb.g + ',' + rgb.b + ', 1)',
                'rgba(' + rgb.r + ',' + rgb.g + ',' + rgb.b + ', .9)'
            ];
        },

        /**
         * Parse a hex color into a hsla color and a lighter partner color.
         * @param {string} hex - Hex color code with leading '#'.
         * @returns {string[]} Color codes.
         */
        parseColor: function(hex) {
            if (!hex) {
                return this.randomColor();
            }
            var rgb = $.colors.hex2rgb(hex);
            return [
                'rgba(' + rgb.r + ',' + rgb.g + ',' + rgb.b + ', 1)',
                'rgba(' + rgb.r + ',' + rgb.g + ',' + rgb.b + ', .9)'
            ];
        },

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
                if (data.updatedDate && new Date(data.updatedDate) > self.initDate) {
                    $.content.forceRefresh();
                    return;
                }
                if ($.isNull(data.ranges) || data.ranges.length === 0) {
                    return;
                }

                var ranges = $.isArray(data.ranges) ? data.ranges : [data];
                if (!ranges.some(function(x) { return x.rows && x.rows.length; })) {
                    $.hide($.get('.chart-spinner', self.content));
                    return;
                }

                $.hide($.get('.chart-spinner', self.content));
                $.hide($.get('.chart-error', self.content));
                $.show(self.canvas.parentNode);

                if (self.chart) {
                    self.chart.destroy();
                    self.chart = null;
                }

                ranges.forEach(function(range) {
                    var type = range.xType.toLowerCase();
                    if (type === 'currency') {
                        var currencyFormat = $.accounting.parseFormat(range.currencyFormat);
                        range.labels = range.labels.map(function(l) {
                            return $.accounting.formatMoney(l, currencyFormat);
                        });
                    } else if (type === 'date') {
                        var dateFormat = range.dateFormat;
                        range.labels = range.labels.map(function(l) {
                            return flatpickr.formatDate(new Date(l), dateFormat);
                        });
                    }
                });

                var chartType = data.type.toLowerCase();
                var isRadial = chartType === 'pie' || chartType === 'doughnut';
                var labels = ranges[0].labels;
                var tooManyLabels = labels.length > 20;
                var datasets = [];

                if (isRadial) {
                    ranges.forEach(function(x) {
                        x.rows.forEach(function(y, j) {
                            var color = self.randomColor(j);
                            datasets.push({
                                value: y,
                                color: color[0],
                                highlight: color[1],
                                label: x.labels[j]
                            });
                        });
                    });
                } else {
                    ranges.forEach(function(x, i) {
                        var color = self.parseColor(x.color);
                        datasets.push({
                            backgroundColor: color[0],
                            borderColor: 'rgb(255, 255, 255)',
                            borderWidth: 2,
                            data: x.rows,
                            label: x.yTitle,
                            yAxisID: 'y-axis-' + i,

                            fillColor: color[0],
                            strokeColor: color[0],
                            pointColor: color[0],
                            pointStrokeColor: '#fff',
                            pointHighlightFill: '#fff',
                            pointHighlightStroke: color[0],
                        });
                    });
                }

                var options = {
                    showXLabels: tooManyLabels ? 20 : true
                };

                var chart = new Chart(self.canvas.getContext('2d'));
                switch (chartType) {
                    case 'horizontalbar':
                        self.chart = chart.HorizontalBar({
                            labels: labels,
                            datasets: datasets
                        }, options);
                        break;
                    case 'bar':
                        self.chart = chart.Bar({
                            labels: labels,
                            datasets: datasets
                        }, options);
                        break;
                    case 'pie':
                        self.chart = chart.Pie(datasets, options);
                        break;
                    case 'doughnut':
                        self.chart = chart.Doughnut(datasets, options);
                        break;
                    default:
                        self.chart = chart.Line({
                            labels: labels,
                            datasets: datasets
                        }, options);
                        break;
                }

                if (self.chart && self.showLegend) {
                    var legend = self.chart.generateLegend();
                    self.canvas.parentNode.parentNode.appendChild($.createNode(legend));
                }
            }, function() {
                $.hide($.get('.chart-spinner', self.content));
                $.show($.get('.chart-error', self.content));
                $.hide(self.canvas.parentNode);
            });
        },

        destroy: function() {
            $.destroy(this.chart);
        }
    };

    return DashChart;
});
