/*!
 * Chart.js
 * https://github.com/avlcodemonkey/Chart.js
 * Version: 1.3.0
 *
 * Copyright 2015 Nick Downie
 * Released under the MIT license
 * https://github.com/nnnick/Chart.js/blob/master/LICENSE.md
 *
 * Copyright 2018 Chris Pittman
 * Released under the MIT license
 * https://github.com/avlcodemonkey/Chart.js/blob/v1x/LICENSE.md
 */
(function(root) {
    'use strict';

    // Global Chart helpers object for utility methods and classes
    var helpers = {};

    // Basic js utility methods
    helpers.each = function(loopable, callback, self) {
        var additionalArgs = Array.prototype.slice.call(arguments, 3);
        // Check to see if null or undefined firstly.
        if (loopable) {
            if (loopable.length === +loopable.length)
                for (var i = 0; i < loopable.length; i++)
                    callback.apply(self, [loopable[i], i].concat(additionalArgs));
            else
                for (var item in loopable)
                    callback.apply(self, [loopable[item], item].concat(additionalArgs));
        }
    };

    helpers.clone = function(obj) {
        var objClone = {};
        helpers.each(obj, function(value, key) {
            if (obj.hasOwnProperty(key))
                objClone[key] = value;
        });
        return objClone;
    };

    helpers.extend = function(base) {
        helpers.each(Array.prototype.slice.call(arguments, 1), function(extensionObject) {
            helpers.each(extensionObject, function(value, key) {
                if (extensionObject.hasOwnProperty(key))
                    base[key] = value;
            });
        });
        return base;
    };

    helpers.merge = function() {
        // Merge properties in left object over to a shallow clone of object right.
        var args = Array.prototype.slice.call(arguments, 0);
        args.unshift({});
        return helpers.extend.apply(null, args);
    };

    helpers.where = function(collection, filterCallback) {
        var filtered = [];
        helpers.each(collection, function(item) {
            if (filterCallback(item))
                filtered.push(item);
        });

        return filtered;
    };

    helpers.findNextWhere = function(arrayToSearch, filterCallback, startIndex) {
        // Default to start of the array
        if (!startIndex)
            startIndex = -1;
        for (var i = startIndex + 1; i < arrayToSearch.length; i++) {
            var currentItem = arrayToSearch[i];
            if (filterCallback(currentItem))
                return currentItem;
        }
    };

    helpers.findPreviousWhere = function(arrayToSearch, filterCallback, startIndex) {
        // Default to end of the array
        if (!startIndex)
            startIndex = arrayToSearch.length;
        for (var i = startIndex - 1; i >= 0; i--) {
            var currentItem = arrayToSearch[i];
            if (filterCallback(currentItem))
                return currentItem;
        }
    };

    var inherits = helpers.inherits = function(extensions) {
        // Basic javascript inheritance based on the model created in Backbone.js
        var parent = this;
        var ChartElement = (extensions && extensions.hasOwnProperty('constructor')) ? extensions.constructor : function() { return parent.apply(this, arguments); };

        var Surrogate = function() { this.constructor = ChartElement; };
        Surrogate.prototype = parent.prototype;
        ChartElement.prototype = new Surrogate();

        ChartElement.extend = inherits;

        if (extensions)
            helpers.extend(ChartElement.prototype, extensions);

        ChartElement.__super__ = parent.prototype;

        return ChartElement;
    };

    helpers.noop = function() { };

    helpers.uid = (function() {
        var id = 0;
        return function() {
            return 'chart-' + id++;
        };
    })();

    // Math methods
    helpers.isNumber = function(n) {
        return !isNaN(parseFloat(n)) && isFinite(n);
    };

    helpers.max = function(array) {
        return Math.max.apply(Math, array);
    };

    helpers.min = function(array) {
        return Math.min.apply(Math, array);
    };

    helpers.getDecimalPlaces = function(num) {
        if (num % 1 !== 0 && helpers.isNumber(num)) {
            var s = num.toString();
            // no exponent, e.g. 0.01
            if (s.indexOf('e-') < 0)
                return s.split('.')[1].length;
            // no decimal point, e.g. 1e-9
            if (s.indexOf('.') < 0)
                return parseInt(s.split('e-')[1]);
            // exponent and decimal point, e.g. 1.23e-9
            var parts = s.split('.')[1].split('e-');
            return parts[0].length + parseInt(parts[1]);
        }
        return 0;
    };

    helpers.toRadians = function(degrees) {
        return degrees * (Math.PI / 180);
    };

    // Gets the angle from vertical upright to the point about a centre.
    helpers.getAngleFromPoint = function(centrePoint, anglePoint) {
        var distanceFromXCenter = anglePoint.x - centrePoint.x,
            distanceFromYCenter = anglePoint.y - centrePoint.y,
            radialDistanceFromCenter = Math.sqrt(distanceFromXCenter * distanceFromXCenter + distanceFromYCenter * distanceFromYCenter);
        var angle = Math.PI * 2 + Math.atan2(distanceFromYCenter, distanceFromXCenter);

        // If the segment is in the top left quadrant, we need to add another rotation to the angle
        if (distanceFromXCenter < 0 && distanceFromYCenter < 0)
            angle += Math.PI * 2;

        return {
            angle: angle,
            distance: radialDistanceFromCenter
        };
    };

    helpers.aliasPixel = function(pixelWidth) {
        return (pixelWidth % 2 === 0) ? 0 : 0.5;
    };

    helpers.splineCurve = function(FirstPoint, MiddlePoint, AfterPoint, t) {
        // Props to Rob Spencer at scaled innovation for his post on splining between points
        // http://scaledinnovation.com/analytics/splines/aboutSplines.html
        var d01 = Math.sqrt(Math.pow(MiddlePoint.x - FirstPoint.x, 2) + Math.pow(MiddlePoint.y - FirstPoint.y, 2)),
            d12 = Math.sqrt(Math.pow(AfterPoint.x - MiddlePoint.x, 2) + Math.pow(AfterPoint.y - MiddlePoint.y, 2)),
            fa = t * d01 / (d01 + d12),// scaling factor for triangle Ta
            fb = t * d12 / (d01 + d12);
        return {
            inner: {
                x: MiddlePoint.x - fa * (AfterPoint.x - FirstPoint.x),
                y: MiddlePoint.y - fa * (AfterPoint.y - FirstPoint.y)
            },
            outer: {
                x: MiddlePoint.x + fb * (AfterPoint.x - FirstPoint.x),
                y: MiddlePoint.y + fb * (AfterPoint.y - FirstPoint.y)
            }
        };
    };

    helpers.calculateScaleRange = function(valuesArray, drawingSize, textSize, startFromZero, integersOnly) {
        // Set a minimum step of two - a point at the top of the graph, and a point at the base
        var minSteps = 2,
            maxSteps = Math.floor(drawingSize / (textSize * 1.5)),
            skipFitting = (minSteps >= maxSteps);

        // Filter out null values since these would min() to zero
        var values = [];
        helpers.each(valuesArray, function(v) {
            if (v !== null)
                values.push(v);
        });
        var minValue = helpers.min(values),
            maxValue = helpers.max(values);

        // We need some degree of separation here to calculate the scales if all the values are the same
        // Adding/minusing 0.5 will give us a range of 1.
        if (maxValue === minValue) {
            maxValue += 0.5;
            // So we don't end up with a graph with a negative start value if we've said always start from zero
            if (minValue >= 0.5 && !startFromZero)
                minValue -= 0.5;
            else
                // Make up a whole number above the values
                maxValue += 0.5;
        }

        var valueRange = Math.abs(maxValue - minValue),
            rangeOrderOfMagnitude = Math.floor(Math.log(valueRange) / Math.LN10),
            graphMax = Math.ceil(maxValue / (1 * Math.pow(10, rangeOrderOfMagnitude))) * Math.pow(10, rangeOrderOfMagnitude),
            graphMin = (startFromZero) ? 0 : Math.floor(minValue / (1 * Math.pow(10, rangeOrderOfMagnitude))) * Math.pow(10, rangeOrderOfMagnitude),
            graphRange = graphMax - graphMin,
            stepValue = Math.pow(10, rangeOrderOfMagnitude),
            numberOfSteps = Math.round(graphRange / stepValue);

        // If we have more space on the graph we'll use it to give more definition to the data
        while ((numberOfSteps > maxSteps || (numberOfSteps * 2) < maxSteps) && !skipFitting) {
            if (numberOfSteps > maxSteps) {
                stepValue *= 2;
                numberOfSteps = Math.round(graphRange / stepValue);
                // Don't ever deal with a decimal number of steps - cancel fitting and just use the minimum number of steps.
                if (numberOfSteps % 1 !== 0)
                    skipFitting = true;
            } else {
                // We can fit in double the amount of scale points on the scale
                // If user has declared ints only, and the step value isn't a decimal
                if (integersOnly && rangeOrderOfMagnitude >= 0) {
                    //If the user has said integers only, we need to check that making the scale more granular wouldn't make it a float
                    if (stepValue / 2 % 1 === 0) {
                        stepValue /= 2;
                        numberOfSteps = Math.round(graphRange / stepValue);
                    } else {
                        // If it would make it a float break out of the loop
                        break;
                    }
                } else {
                    // If the scale doesn't have to be an int, make the scale more granular anyway.
                    stepValue /= 2;
                    numberOfSteps = Math.round(graphRange / stepValue);
                }
            }
        }

        if (skipFitting) {
            numberOfSteps = minSteps;
            stepValue = graphRange / numberOfSteps;
        }

        return {
            steps: numberOfSteps,
            stepValue: stepValue,
            min: graphMin,
            max: graphMin + (numberOfSteps * stepValue)
        };
    };

    /* eslint-disable */
    // Blows up lint errors based on the new Function constructor
    // Templating methods
    // Javascript micro templating by John Resig - source at http://ejohn.org/blog/javascript-micro-templating/
    helpers.template = function(templateString, valuesObject) {
        // If templateString is function rather than string-template - call the function for valuesObject
        if (templateString instanceof Function)
            return templateString(valuesObject);

        var cache = {};
        function tmpl(str, data) {
            // Figure out if we're getting a template, or if we need to
            // load the template - and be sure to cache the result.
            var fn = !/\W/.test(str) ?
                cache[str] = cache[str] :

                // Generate a reusable function that will serve as a template
                // generator (and which will be cached).
                new Function("obj",
                    "var p=[],print=function(){p.push.apply(p,arguments);};" +

                    // Introduce the data as local variables using with(){}
                    "with(obj){p.push('" +

                    // Convert the template into pure JavaScript
                    str
                        .replace(/[\r\t\n]/g, " ")
                        .split("<%").join("\t")
                        .replace(/((^|%>)[^\t]*)'/g, "$1\r")
                        .replace(/\t=(.*?)%>/g, "',$1,'")
                        .split("\t").join("');")
                        .split("%>").join("p.push('")
                        .split("\r").join("\\'") +
                    "');}return p.join('');"
                );

            // Provide some basic currying to the user
            return data ? fn(data) : fn;
        }
        return tmpl(templateString, valuesObject);
    };
    /* eslint-enable */

    // DOM methods
    helpers.getRelativePosition = function(evt) {
        var mouseX, mouseY;
        var e = evt.originalEvent || evt,
            canvas = evt.currentTarget || evt.srcElement,
            boundingRect = canvas.getBoundingClientRect();

        if (e.touches) {
            mouseX = e.touches[0].clientX - boundingRect.left;
            mouseY = e.touches[0].clientY - boundingRect.top;
        } else {
            mouseX = e.clientX - boundingRect.left;
            mouseY = e.clientY - boundingRect.top;
        }

        return {
            x: mouseX,
            y: mouseY
        };
    };

    helpers.bindEvents = function(chartInstance, arrayOfEvents, handler) {
        // Create the events object if it's not already present
        if (!chartInstance.events) chartInstance.events = {};

        helpers.each(arrayOfEvents, function(eventName) {
            chartInstance.events[eventName] = function() {
                handler.apply(chartInstance, arguments);
            };
            chartInstance.chart.canvas.addEventListener(eventName, chartInstance.events[eventName]);
        });
    };

    helpers.unbindEvents = function(chartInstance, arrayOfEvents) {
        helpers.each(arrayOfEvents, function(handler, eventName) {
            chartInstance.chart.canvas.removeEventListener(eventName, handler);
        });
        if (window && arrayOfEvents.windowResize)
            window.removeEventListener('resize', arrayOfEvents.windowResize);
    };

    helpers.getMaximumWidth = function(domNode) {
        var container = domNode.parentNode,
            padding = parseInt(helpers.getStyle(container, 'padding-left')) + parseInt(helpers.getStyle(container, 'padding-right'));
        return container ? container.clientWidth - padding : 0;
    };

    helpers.getMaximumHeight = function(domNode) {
        var container = domNode.parentNode,
            padding = parseInt(helpers.getStyle(container, 'padding-bottom')) + parseInt(helpers.getStyle(container, 'padding-top'));
        return container ? container.clientHeight - padding : 0;
    };

    helpers.getStyle = function(el, property) {
        return document.defaultView.getComputedStyle(el, null).getPropertyValue(property);
    };

    helpers.retinaScale = function(chart) {
        var ctx = chart.ctx,
            width = chart.canvas.width,
            height = chart.canvas.height;

        if (window.devicePixelRatio) {
            ctx.canvas.style.width = width + 'px';
            ctx.canvas.style.height = height + 'px';
            ctx.canvas.height = height * window.devicePixelRatio;
            ctx.canvas.width = width * window.devicePixelRatio;
            ctx.scale(window.devicePixelRatio, window.devicePixelRatio);
        }
    };

    // Canvas methods
    helpers.clear = function(chart) {
        chart.ctx.clearRect(0, 0, chart.width, chart.height);
    };

    helpers.fontString = function(pixelSize, fontStyle, fontFamily) {
        return fontStyle + ' ' + pixelSize + 'px ' + fontFamily;
    };

    helpers.longestText = function(ctx, font, arrayOfStrings) {
        ctx.font = font;
        var longest = 0;
        helpers.each(arrayOfStrings, function(string) {
            var textWidth = ctx.measureText(string).width;
            longest = (textWidth > longest) ? textWidth : longest;
        });
        return longest;
    };

    helpers.drawRoundedRectangle = function(ctx, x, y, width, height, radius) {
        ctx.beginPath();
        ctx.moveTo(x + radius, y);
        ctx.lineTo(x + width - radius, y);
        ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
        ctx.lineTo(x + width, y + height - radius);
        ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
        ctx.lineTo(x + radius, y + height);
        ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
        ctx.lineTo(x, y + radius);
        ctx.quadraticCurveTo(x, y, x + radius, y);
        ctx.closePath();
    };

    root.ChartHelpers = helpers;
}(this));

(function(root, helpers) {
    'use strict';

    // Occupy the global variable of Chart, and create a simple base class
    var Chart = function(context) {
        this.canvas = context.canvas;
        this.ctx = context;
        this.width = context.canvas.offsetWidth || context.canvas.width;
        this.height = context.canvas.offsetHeight || context.canvas.height;
        this.aspectRatio = this.width / this.height;

        // High pixel density displays - multiply the size of the canvas height/width by the device pixel ratio, then scale.
        helpers.retinaScale(this);

        return this;
    };

    // Globally expose the defaults to allow for user updating/changing
    Chart.defaults = {
        global: {
            // String - Font family for scale, tooltips, points, etc
            fontFamily: '"Lato", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',

            // Boolean - If we should show the scale at all
            showScale: true,

            // String - Colour of the scale line
            scaleLineColor: 'rgba(0,0,0,.1)',

            // Number - Pixel width of the scale line
            scaleLineWidth: 1,

            // Boolean - Whether to show labels on the scale
            scaleShowLabels: true,

            // Boolean or a positive integer denoting number of labels to be shown on x axis
            showXLabels: true,

            // Interpolated JS string - can access value
            scaleLabel: '<%=value%>',

            // Boolean - Whether the scale should stick to integers, and not show any floats even if drawing space is there
            scaleIntegersOnly: true,

            // Boolean - Whether the scale should start at zero, or an order of magnitude down from the lowest value
            scaleBeginAtZero: false,

            // Number - Scale label font size in pixels
            scaleFontSize: 12,

            // String - Scale label font weight style
            scaleFontStyle: 'normal',

            // String - Scale label font colour
            scaleFontColor: '#666',

            // Boolean - whether to maintain the starting aspect ratio or not when responsive, if set to false, will take up entire container
            maintainAspectRatio: false,

            // Boolean - Determines whether to draw tooltips on the canvas or not - attaches events to touchmove & mousemove
            showTooltips: true,

            // Array - Array of string names to attach tooltip events
            tooltipEvents: ['mousemove', 'touchstart', 'touchmove', 'mouseout'],

            // String - Tooltip background colour
            tooltipFillColor: 'rgba(0,0,0,0.8)',

            // Number - Tooltip label font size in pixels
            tooltipFontSize: 12,

            // String - Tooltip font weight style
            tooltipFontStyle: 'normal',

            // String - Tooltip label font colour
            tooltipFontColor: '#fff',

            // Number - Tooltip title font size in pixels
            tooltipTitleFontSize: 14,

            // String - Tooltip title font weight style
            tooltipTitleFontStyle: 'bold',

            // String - Tooltip title font colour
            tooltipTitleFontColor: '#fff',

            // String - Tooltip title template
            tooltipTitleTemplate: '<%= label%>',

            // Number - pixel width of padding around tooltip text
            tooltipYPadding: 6,

            // Number - pixel width of padding around tooltip text
            tooltipXPadding: 6,

            // Number - Size of the caret on the tooltip
            tooltipCaretSize: 8,

            // Number - Pixel radius of the tooltip border
            tooltipCornerRadius: 6,

            // Number - Pixel offset from point x to tooltip edge
            tooltipXOffset: 10,

            // String - Template string for single tooltips
            tooltipTemplate: '<%if (label){%><%=label%>: <%}%><%= value %>',

            // String - Template string for single tooltips
            multiTooltipTemplate: '<%= datasetLabel %>: <%= value %>',

            // String - Colour behind the legend colour block
            multiTooltipKeyBackground: '#fff',

            // Array - A list of colors to use as the defaults
            segmentColorDefault: ['#A6CEE3', '#1F78B4', '#B2DF8A', '#33A02C', '#FB9A99', '#E31A1C', '#FDBF6F', '#FF7F00', '#CAB2D6', '#6A3D9A', '#B4B482', '#B15928'],

            // Array - A list of highlight colors to use as the defaults
            segmentHighlightColorDefaults: ['#CEF6FF', '#47A0DC', '#DAFFB2', '#5BC854', '#FFC2C1', '#FF4244', '#FFE797', '#FFA728', '#F2DAFE', '#9265C2', '#DCDCAA', '#D98150'],
        }
    };

    // Create a dictionary of chart types, to allow for extension of existing types
    Chart.types = {};

    Chart.Type = function(data, options, chart) {
        this.options = options;
        this.chart = chart;
        this.id = helpers.uid();
        this.events = {};

        this.resize();
        if (root) {
            var self = this;
            self.events.windowResize = (function() {
                // Basic debounce of resize function so it doesn't hurt performance when resizing browser.
                var timeout;
                return function() {
                    clearTimeout(timeout);
                    timeout = setTimeout(function() {
                        self.resize(self.render, true);
                    }, 50);
                };
            })();
            root.addEventListener('resize', self.events.windowResize);
        }

        // Initialize is always called when a chart type is created. By default it is a no op, but it should be extended
        this.initialize.call(this, data);
    };

    // Core methods that'll be a part of every chart type
    helpers.extend(Chart.Type.prototype, {
        initialize: function() {
            return this;
        },

        clear: function() {
            helpers.clear(this.chart);
            return this;
        },

        resize: function(callback) {
            var canvas = this.chart.canvas,
                newWidth = helpers.getMaximumWidth(this.chart.canvas),
                newHeight = this.options.maintainAspectRatio ? newWidth / this.chart.aspectRatio : helpers.getMaximumHeight(this.chart.canvas);

            canvas.width = this.chart.width = newWidth;
            canvas.height = this.chart.height = newHeight;

            helpers.retinaScale(this.chart);

            if (typeof callback === 'function')
                callback.apply(this, Array.prototype.slice.call(arguments, 1));
            return this;
        },

        reflow: helpers.noop,

        render: function(reflow) {
            if (reflow)
                this.reflow();
            this.draw();
            return this;
        },

        generateLegend: function() {
            return helpers.template(this.options.legendTemplate, this);
        },

        destroy: function() {
            this.clear();
            helpers.unbindEvents(this, this.events);
            var canvas = this.chart.canvas;

            // Reset canvas height/width attributes starts a fresh with the canvas context
            canvas.width = this.chart.width;
            canvas.height = this.chart.height;
            canvas.style.removeProperty('width');
            canvas.style.removeProperty('height');
        },

        showTooltip: function(chartElements) {
            // Only redraw the chart if we've actually changed what we're hovering on.
            if (typeof this.activeElements === 'undefined')
                this.activeElements = [];

            var isChanged = (function(elements) {
                var changed = false;

                if (elements.length !== this.activeElements.length) {
                    changed = true;
                    return changed;
                }

                helpers.each(elements, function(element, index) {
                    if (element !== this.activeElements[index])
                        changed = true;
                }, this);
                return changed;
            }).call(this, chartElements);

            if (!isChanged)
                return;

            this.activeElements = chartElements;
            this.draw();
            if (chartElements.length > 0) {
                // If we have multiple datasets, show a MultiTooltip for all of the data points at that index
                if (this.datasets && this.datasets.length > 1) {
                    var dataArray,
                        dataIndex;

                    for (var i = this.datasets.length - 1; i >= 0; i--) {
                        dataArray = this.datasets[i].points || this.datasets[i].bars || this.datasets[i].segments;
                        dataIndex = dataArray.indexOf(chartElements[0]);
                        if (dataIndex !== -1)
                            break;
                    }
                    var tooltipLabels = [],
                        tooltipColors = [],
                        medianPosition = (function() {
                            var elements = [],
                                dataCollection,
                                xPositions = [],
                                yPositions = [],
                                xMax,
                                yMax,
                                xMin,
                                yMin;
                            helpers.each(this.datasets, function(dataset) {
                                dataCollection = dataset.points || dataset.bars || dataset.segments;
                                if (dataCollection[dataIndex] && dataCollection[dataIndex].hasValue()) {
                                    elements.push(dataCollection[dataIndex]);
                                }
                            });

                            helpers.each(elements, function(element) {
                                xPositions.push(element.x);
                                yPositions.push(element.y);

                                // Include any colour information about the element
                                tooltipLabels.push(helpers.template(this.options.multiTooltipTemplate, element));
                                tooltipColors.push({
                                    fill: element._saved.fillColor || element.fillColor,
                                    stroke: element._saved.strokeColor || element.strokeColor
                                });
                            }, this);

                            yMin = helpers.min(yPositions);
                            yMax = helpers.max(yPositions);
                            xMin = helpers.min(xPositions);
                            xMax = helpers.max(xPositions);

                            return {
                                x: (xMin > this.chart.width / 2) ? xMin : xMax,
                                y: (yMin + yMax) / 2
                            };
                        }).call(this, dataIndex);

                    new Chart.MultiTooltip({
                        x: medianPosition.x,
                        y: medianPosition.y,
                        xPadding: this.options.tooltipXPadding,
                        yPadding: this.options.tooltipYPadding,
                        xOffset: this.options.tooltipXOffset,
                        fillColor: this.options.tooltipFillColor,
                        textColor: this.options.tooltipFontColor,
                        fontFamily: this.options.fontFamily,
                        fontStyle: this.options.tooltipFontStyle,
                        fontSize: this.options.tooltipFontSize,
                        titleTextColor: this.options.tooltipTitleFontColor,
                        titleFontStyle: this.options.tooltipTitleFontStyle,
                        titleFontSize: this.options.tooltipTitleFontSize,
                        cornerRadius: this.options.tooltipCornerRadius,
                        labels: tooltipLabels,
                        legendColors: tooltipColors,
                        legendColorBackground: this.options.multiTooltipKeyBackground,
                        title: helpers.template(this.options.tooltipTitleTemplate, chartElements[0]),
                        chart: this.chart,
                        ctx: this.chart.ctx
                    }).draw();
                } else {
                    helpers.each(chartElements, function(element) {
                        var tooltipPosition = element.tooltipPosition();
                        new Chart.Tooltip({
                            x: Math.round(tooltipPosition.x),
                            y: Math.round(tooltipPosition.y),
                            xPadding: this.options.tooltipXPadding,
                            yPadding: this.options.tooltipYPadding,
                            fillColor: this.options.tooltipFillColor,
                            textColor: this.options.tooltipFontColor,
                            fontFamily: this.options.fontFamily,
                            fontStyle: this.options.tooltipFontStyle,
                            fontSize: this.options.tooltipFontSize,
                            caretHeight: this.options.tooltipCaretSize,
                            cornerRadius: this.options.tooltipCornerRadius,
                            text: helpers.template(this.options.tooltipTemplate, element),
                            chart: this.chart
                        }).draw();
                    }, this);
                }
            }
            return this;
        },

        toBase64Image: function() {
            return this.chart.canvas.toDataURL.apply(this.chart.canvas, arguments);
        }
    });

    Chart.Type.extend = function(extensions) {
        var parent = this;

        var ChartType = function() {
            return parent.apply(this, arguments);
        };

        // Copy the prototype object of the this class
        ChartType.prototype = helpers.clone(parent.prototype);
        // Now overwrite some of the properties in the base class with the new extensions
        helpers.extend(ChartType.prototype, extensions);

        ChartType.extend = Chart.Type.extend;

        if (extensions.name || parent.prototype.name) {
            var chartName = extensions.name || parent.prototype.name;
            // Assign any potential default values of the new chart type
            // If none are defined, we'll use a clone of the chart type this is being extended from.
            // I.e. if we extend a line chart, we'll use the defaults from the line chart if our new chart
            // doesn't define some defaults of their own.

            var baseDefaults = (Chart.defaults[parent.prototype.name]) ? helpers.clone(Chart.defaults[parent.prototype.name]) : {};
            Chart.defaults[chartName] = helpers.extend(baseDefaults, extensions.defaults);
            Chart.types[chartName] = ChartType;

            // Register this new chart type in the Chart prototype
            Chart.prototype[chartName] = function(data, options) {
                return new ChartType(data, helpers.merge(Chart.defaults.global, Chart.defaults[chartName], options || {}), this);
            };
        } else {
            root.console.warn('Name not provided for this chart, so it hasnt been registered');
        }
        return parent;
    };

    Chart.Element = function(configuration) {
        helpers.extend(this, configuration);
        this.initialize.apply(this, arguments);
        this.save();
    };

    helpers.extend(Chart.Element.prototype, {
        initialize: function() { },

        restore: function(props) {
            if (!props)
                helpers.extend(this, this._saved);
            else
                helpers.each(props, function(key) {
                    this[key] = this._saved[key];
                }, this);
            return this;
        },

        save: function() {
            this._saved = helpers.clone(this);
            delete this._saved._saved;
            return this;
        },

        update: function(newProps) {
            helpers.each(newProps, function(value, key) {
                this._saved[key] = this[key];
                this[key] = value;
            }, this);
            return this;
        },

        transition: function(props, ease) {
            helpers.each(props, function(value, key) {
                this[key] = ((value - this._saved[key]) * ease) + this._saved[key];
            }, this);
            return this;
        },

        tooltipPosition: function() {
            return {
                x: this.x,
                y: this.y
            };
        },

        hasValue: function() {
            return helpers.isNumber(this.value);
        }
    });

    Chart.Element.extend = helpers.inherits;

    Chart.Point = Chart.Element.extend({
        inRange: function(chartX, chartY) {
            return ((Math.pow(chartX - this.x, 2) + Math.pow(chartY - this.y, 2)) < Math.pow(this.hitDetectionRadius + this.radius, 2));
        },

        draw: function() {
            var ctx = this.ctx;
            ctx.beginPath();
            ctx.arc(this.x, this.y, this.radius, 0, Math.PI * 2);
            ctx.closePath();

            ctx.strokeStyle = this.strokeColor;
            ctx.lineWidth = this.strokeWidth;
            ctx.fillStyle = this.fillColor;
            ctx.fill();
            ctx.stroke();
        }
    });

    Chart.Arc = Chart.Element.extend({
        inRange: function(chartX, chartY) {
            var pointRelativePosition = helpers.getAngleFromPoint(this, {
                x: chartX,
                y: chartY
            });

            // Normalize all angles to 0 - 2*PI (0 - 360°)
            var pointRelativeAngle = pointRelativePosition.angle % (Math.PI * 2),
                startAngle = (Math.PI * 2 + this.startAngle) % (Math.PI * 2),
                endAngle = (Math.PI * 2 + this.endAngle) % (Math.PI * 2) || 360;

            // Calculate wether the pointRelativeAngle is between the start and the end angle
            var betweenAngles = (endAngle < startAngle) ?
                pointRelativeAngle <= endAngle || pointRelativeAngle >= startAngle :
                pointRelativeAngle >= startAngle && pointRelativeAngle <= endAngle;

            // Check if within the range of the open/close angle
            var withinRadius = (pointRelativePosition.distance >= this.innerRadius && pointRelativePosition.distance <= this.outerRadius);

            // Ensure within the outside of the arc centre, but inside arc outer
            return (betweenAngles && withinRadius);
        },

        tooltipPosition: function() {
            var centreAngle = this.startAngle + ((this.endAngle - this.startAngle) / 2),
                rangeFromCentre = (this.outerRadius - this.innerRadius) / 2 + this.innerRadius;
            return {
                x: this.x + (Math.cos(centreAngle) * rangeFromCentre),
                y: this.y + (Math.sin(centreAngle) * rangeFromCentre)
            };
        },

        draw: function() {
            var ctx = this.ctx;
            ctx.beginPath();
            ctx.arc(this.x, this.y, this.outerRadius < 0 ? 0 : this.outerRadius, this.startAngle, this.endAngle);
            ctx.arc(this.x, this.y, this.innerRadius < 0 ? 0 : this.innerRadius, this.endAngle, this.startAngle, true);
            ctx.closePath();

            ctx.strokeStyle = this.strokeColor;
            ctx.lineWidth = this.strokeWidth;
            ctx.fillStyle = this.fillColor;
            ctx.fill();
            ctx.lineJoin = 'bevel';

            if (this.showStroke)
                ctx.stroke();
        }
    });

    Chart.Rectangle = Chart.Element.extend({
        draw: function() {
            var ctx = this.ctx,
                halfWidth = this.width / 2,
                leftX = this.x - halfWidth,
                rightX = this.x + halfWidth,
                top = this.base - (this.base - this.y),
                halfStroke = this.strokeWidth / 2;

            // Canvas doesn't allow us to stroke inside the width so we can adjust the sizes to fit if we're setting a stroke on the line
            if (this.showStroke) {
                leftX += halfStroke;
                rightX -= halfStroke;
                top += halfStroke;
            }

            ctx.beginPath();
            ctx.fillStyle = this.fillColor;
            ctx.strokeStyle = this.strokeColor;
            ctx.lineWidth = this.strokeWidth;

            // It'd be nice to keep this class totally generic to any rectangle and simply specify which border to miss out.
            ctx.moveTo(leftX, this.base);
            ctx.lineTo(leftX, top);
            ctx.lineTo(rightX, top);
            ctx.lineTo(rightX, this.base);
            ctx.fill();
            if (this.showStroke)
                ctx.stroke();
        },

        height: function() {
            return this.base - this.y;
        },

        inRange: function(chartX, chartY) {
            // incorporate vertical padding so tooltips will be visible when value is zero
            var yPadding = this.value === 0 ? 1 : 0;
            return (chartX >= this.x - this.width / 2 && chartX <= this.x + this.width / 2) && (chartY >= this.y - yPadding && chartY <= this.base + yPadding);
        }
    });

    Chart.Tooltip = Chart.Element.extend({
        draw: function() {
            var ctx = this.chart.ctx;
            ctx.font = helpers.fontString(this.fontSize, this.fontStyle, this.fontFamily);

            this.xAlign = 'center';
            this.yAlign = 'above';

            // Distance between the actual element.y position and the start of the tooltip caret
            var caretPadding = this.caretPadding = 2;
            var tooltipWidth = ctx.measureText(this.text).width + 2 * this.xPadding,
                tooltipRectHeight = this.fontSize + 2 * this.yPadding,
                tooltipHeight = tooltipRectHeight + this.caretHeight + caretPadding;

            if (this.x + tooltipWidth / 2 > this.chart.width)
                this.xAlign = 'left';
            else if (this.x - tooltipWidth / 2 < 0)
                this.xAlign = 'right';

            if (this.y - tooltipHeight < 0)
                this.yAlign = 'below';

            var tooltipX = this.x - tooltipWidth / 2,
                tooltipY = this.y - tooltipHeight;

            ctx.fillStyle = this.fillColor;

            switch (this.yAlign) {
                case 'above':
                    // Draw a caret above the x/y
                    ctx.beginPath();
                    ctx.moveTo(this.x, this.y - caretPadding);
                    ctx.lineTo(this.x + this.caretHeight, this.y - (caretPadding + this.caretHeight));
                    ctx.lineTo(this.x - this.caretHeight, this.y - (caretPadding + this.caretHeight));
                    ctx.closePath();
                    ctx.fill();
                    break;
                case 'below':
                    tooltipY = this.y + caretPadding + this.caretHeight;
                    // Draw a caret below the x/y
                    ctx.beginPath();
                    ctx.moveTo(this.x, this.y + caretPadding);
                    ctx.lineTo(this.x + this.caretHeight, this.y + caretPadding + this.caretHeight);
                    ctx.lineTo(this.x - this.caretHeight, this.y + caretPadding + this.caretHeight);
                    ctx.closePath();
                    ctx.fill();
                    break;
            }

            switch (this.xAlign) {
                case 'left':
                    tooltipX = this.x - tooltipWidth + (this.cornerRadius + this.caretHeight);
                    break;
                case 'right':
                    tooltipX = this.x - (this.cornerRadius + this.caretHeight);
                    break;
            }

            helpers.drawRoundedRectangle(ctx, tooltipX, tooltipY, tooltipWidth, tooltipRectHeight, this.cornerRadius);
            ctx.fill();
            ctx.fillStyle = this.textColor;
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(this.text, tooltipX + tooltipWidth / 2, tooltipY + tooltipRectHeight / 2);
        }
    });

    Chart.MultiTooltip = Chart.Element.extend({
        initialize: function() {
            this.font = helpers.fontString(this.fontSize, this.fontStyle, this.fontFamily);
            this.titleFont = helpers.fontString(this.titleFontSize, this.titleFontStyle, this.fontFamily);
            this.titleHeight = this.title ? this.titleFontSize * 1.5 : 0;
            this.height = (this.labels.length * this.fontSize) + ((this.labels.length - 1) * (this.fontSize / 2)) + (this.yPadding * 2) + this.titleHeight;
            this.ctx.font = this.titleFont;

            var titleWidth = this.ctx.measureText(this.title).width,
                //Label has a legend square as well so account for this.
                labelWidth = helpers.longestText(this.ctx, this.font, this.labels) + this.fontSize + 3,
                longestTextWidth = helpers.max([labelWidth, titleWidth]);

            this.width = longestTextWidth + (this.xPadding * 2);

            var halfHeight = this.height / 2;
            // Check to ensure the height will fit on the canvas
            if (this.y - halfHeight < 0)
                this.y = halfHeight;
            else if (this.y + halfHeight > this.chart.height)
                this.y = this.chart.height - halfHeight;

            // Decide whether to align left or right based on position on canvas
            if (this.x > this.chart.width / 2)
                this.x -= this.xOffset + this.width;
            else
                this.x += this.xOffset;
        },

        getLineHeight: function(index) {
            var baseLineHeight = this.y - (this.height / 2) + this.yPadding,
                afterTitleIndex = index - 1;
            // If the index is zero, we're getting the title
            return index === 0 ? (baseLineHeight + this.titleHeight / 3) :
                (baseLineHeight + ((this.fontSize * 1.5 * afterTitleIndex) + this.fontSize / 2) + this.titleHeight);
        },

        draw: function() {
            helpers.drawRoundedRectangle(this.ctx, this.x, this.y - this.height / 2, this.width, this.height, this.cornerRadius);
            var ctx = this.ctx;
            ctx.fillStyle = this.fillColor;
            ctx.fill();
            ctx.closePath();
            ctx.textAlign = 'left';
            ctx.textBaseline = 'middle';
            ctx.fillStyle = this.titleTextColor;
            ctx.font = this.titleFont;
            ctx.fillText(this.title, this.x + this.xPadding, this.getLineHeight(0));
            ctx.font = this.font;

            helpers.each(this.labels, function(label, index) {
                ctx.fillStyle = this.textColor;
                ctx.fillText(label, this.x + this.xPadding + this.fontSize + 3, this.getLineHeight(index + 1));
                ctx.clearRect(this.x + this.xPadding, this.getLineHeight(index + 1) - this.fontSize / 2, this.fontSize, this.fontSize);
                ctx.fillStyle = this.legendColors[index].fill;
                ctx.fillRect(this.x + this.xPadding, this.getLineHeight(index + 1) - this.fontSize / 2, this.fontSize, this.fontSize);
            }, this);
        }
    });

    Chart.Scale = Chart.Element.extend({
        initialize: function() {
            this.fit();
        },

        buildYLabels: function() {
            this.yLabels = [];
            var stepDecimalPlaces = helpers.getDecimalPlaces(this.stepValue);
            for (var i = 0; i <= this.steps; i++)
                this.yLabels.push(helpers.template(this.templateString, { value: (this.min + (i * this.stepValue)).toFixed(stepDecimalPlaces) }));
            this.yLabelWidth = (this.display && this.showLabels) ? helpers.longestText(this.ctx, this.font, this.yLabels) + 10 : 0;
        },

        // Fitting loop to rotate x Labels and figure out what fits there, and also calculate how many Y steps to use
        fit: function() {
            // First we need the width of the yLabels, assuming the xLabels aren't rotated

            // To do that we need the base line at the top and base of the chart, assuming there is no x label rotation
            this.startPoint = (this.display) ? this.fontSize : 0;
            this.endPoint = (this.display) ? this.height - (this.fontSize * 1.5) - 5 : this.height; // -5 to pad labels

            // Apply padding settings to the start and end point.
            this.startPoint += this.padding;
            this.endPoint -= this.padding;

            // Cache the starting endpoint, excluding the space for x labels
            var cachedEndPoint = this.endPoint;

            // Cache the starting height, so can determine if we need to recalculate the scale yAxis
            var cachedHeight = this.endPoint - this.startPoint,
                cachedYLabelWidth;

            // Build the current yLabels so we have an idea of what size they'll be to start
            /*
			 *	This sets what is returned from calculateScaleRange as static properties of this class:
			 *
				this.steps;
				this.stepValue;
				this.min;
				this.max;
			 *
			 */
            this.calculateYRange(cachedHeight);

            // With these properties set we can now build the array of yLabels and also the width of the largest yLabel
            this.buildYLabels();

            this.calculateXLabelRotation();

            while ((cachedHeight > this.endPoint - this.startPoint)) {
                cachedHeight = this.endPoint - this.startPoint;
                cachedYLabelWidth = this.yLabelWidth;

                this.calculateYRange(cachedHeight);
                this.buildYLabels();

                // Only go through the xLabel loop again if the yLabel width has changed
                if (cachedYLabelWidth < this.yLabelWidth) {
                    this.endPoint = cachedEndPoint;
                    this.calculateXLabelRotation();
                }
            }
        },

        calculateXLabelRotation: function() {
            //Get the width of each grid by calculating the difference
            //between x offsets between 0 and 1.

            this.ctx.font = this.font;

            var firstWidth = this.ctx.measureText(this.xLabels[0]).width,
                lastWidth = this.ctx.measureText(this.xLabels[this.xLabels.length - 1]).width,
                firstRotated;

            this.xScalePaddingRight = lastWidth / 2 + 3;
            this.xScalePaddingLeft = (firstWidth / 2 > this.yLabelWidth) ? firstWidth / 2 : this.yLabelWidth;

            this.xLabelRotation = 0;
            if (this.display) {
                var originalLabelWidth = helpers.longestText(this.ctx, this.font, this.xLabels),
                    cosRotation;
                this.xLabelWidth = originalLabelWidth;
                //Allow 3 pixels x2 padding either side for label readability
                var xGridWidth = Math.floor(this.calculateX(1) - this.calculateX(0)) - 6;

                //Max label rotate should be 90 - also act as a loop counter
                while ((this.xLabelWidth > xGridWidth && this.xLabelRotation === 0) || (this.xLabelWidth > xGridWidth && this.xLabelRotation <= 90 && this.xLabelRotation > 0)) {
                    cosRotation = Math.cos(helpers.toRadians(this.xLabelRotation));
                    firstRotated = cosRotation * firstWidth;

                    // We're right aligning the text now.
                    if (firstRotated + this.fontSize / 2 > this.yLabelWidth)
                        this.xScalePaddingLeft = firstRotated + this.fontSize / 2;
                    this.xScalePaddingRight = this.fontSize / 2;
                    this.xLabelRotation++;
                    this.xLabelWidth = cosRotation * originalLabelWidth;
                }
                if (this.xLabelRotation > 0)
                    this.endPoint -= Math.sin(helpers.toRadians(this.xLabelRotation)) * originalLabelWidth + 3;
            } else {
                this.xLabelWidth = 0;
                this.xScalePaddingRight = this.padding;
                this.xScalePaddingLeft = this.padding;
            }
        },

        // Needs to be overidden in each Chart type. Otherwise we need to pass all the data into the scale class
        calculateYRange: helpers.noop,

        drawingArea: function() {
            return this.startPoint - this.endPoint;
        },

        calculateY: function(value) {
            var scalingFactor = this.drawingArea() / (this.min - this.max);
            return this.endPoint - (scalingFactor * (value - this.min));
        },

        calculateX: function(index) {
            var innerWidth = this.width - (this.xScalePaddingLeft + this.xScalePaddingRight),
                valueWidth = innerWidth / Math.max((this.valuesCount - ((this.offsetGridLines) ? 0 : 1)), 1),
                valueOffset = (valueWidth * index) + this.xScalePaddingLeft;
            if (this.offsetGridLines)
                valueOffset += (valueWidth / 2);
            return Math.round(valueOffset);
        },

        update: function(newProps) {
            helpers.extend(this, newProps);
            this.fit();
        },

        draw: function() {
            var ctx = this.ctx,
                yLabelGap = (this.endPoint - this.startPoint) / this.steps,
                xStart = Math.round(this.xScalePaddingLeft);
            if (this.display) {
                ctx.fillStyle = this.textColor;
                ctx.font = this.font;
                helpers.each(this.yLabels, function(labelString, index) {
                    var yLabelCenter = this.endPoint - (yLabelGap * index),
                        linePositionY = Math.round(yLabelCenter),
                        drawHorizontalLine = this.showHorizontalLines;

                    ctx.textAlign = 'right';
                    ctx.textBaseline = 'middle';
                    if (this.showLabels)
                        ctx.fillText(labelString, xStart - 10, yLabelCenter);

                    // This is X axis, so draw it
                    if (index === 0 && !drawHorizontalLine)
                        drawHorizontalLine = true;

                    if (drawHorizontalLine)
                        ctx.beginPath();

                    if (index > 0) {
                        // This is a grid line in the centre, so drop that
                        ctx.lineWidth = this.gridLineWidth;
                        ctx.strokeStyle = this.gridLineColor;
                    } else {
                        // This is the first line on the scale
                        ctx.lineWidth = this.lineWidth;
                        ctx.strokeStyle = this.lineColor;
                    }

                    linePositionY += helpers.aliasPixel(ctx.lineWidth);

                    if (drawHorizontalLine) {
                        ctx.moveTo(xStart, linePositionY);
                        ctx.lineTo(this.width, linePositionY);
                        ctx.stroke();
                        ctx.closePath();
                    }

                    ctx.lineWidth = this.lineWidth;
                    ctx.strokeStyle = this.lineColor;
                    ctx.beginPath();
                    ctx.moveTo(xStart - 5, linePositionY);
                    ctx.lineTo(xStart, linePositionY);
                    ctx.stroke();
                    ctx.closePath();
                }, this);

                //  xLabelsSkipper is a number which if gives 0 as remainder [ indexof(xLabel)/xLabelsSkipper ], we print xLabels, otherwise, we skip it
                //                   if number then divide and determine                                        | else, if true, print all labels, else we never print
                this.xLabelsSkipper = helpers.isNumber(this.showXLabels) ? Math.ceil(this.xLabels.length / this.showXLabels) : (this.showXLabels === true) ? 1 : this.xLabels.length + 1;
                helpers.each(this.xLabels, function(label, index) {
                    var xPos = this.calculateX(index) + helpers.aliasPixel(this.lineWidth),
                        // Check to see if line/bar here and decide where to place the line
                        linePos = this.calculateX(index - (this.offsetGridLines ? 0.5 : 0)) + helpers.aliasPixel(this.lineWidth),
                        isRotated = (this.xLabelRotation > 0),
                        drawVerticalLine = this.showVerticalLines;

                    // This is Y axis, so draw it
                    if (index === 0 && !drawVerticalLine)
                        drawVerticalLine = true;

                    if (drawVerticalLine)
                        ctx.beginPath();

                    if (index > 0) {
                        // This is a grid line in the centre, so drop that
                        ctx.lineWidth = this.gridLineWidth;
                        ctx.strokeStyle = this.gridLineColor;
                    } else {
                        // This is the first line on the scale
                        ctx.lineWidth = this.lineWidth;
                        ctx.strokeStyle = this.lineColor;
                    }

                    if (drawVerticalLine) {
                        ctx.moveTo(linePos, this.endPoint);
                        ctx.lineTo(linePos, this.startPoint - 3);
                        ctx.stroke();
                        ctx.closePath();
                    }

                    ctx.lineWidth = this.lineWidth;
                    ctx.strokeStyle = this.lineColor;

                    // Small lines at the bottom of the base grid line
                    if (index % this.xLabelsSkipper === 0) {
                        ctx.beginPath();
                        ctx.moveTo(linePos, this.endPoint);
                        ctx.lineTo(linePos, this.endPoint + 5);
                        ctx.stroke();
                        ctx.closePath();
                    }

                    ctx.save();
                    ctx.translate(xPos, this.endPoint + (isRotated ? 12 : 8));
                    ctx.rotate(helpers.toRadians(this.xLabelRotation) * -1);
                    ctx.font = this.font;
                    ctx.textAlign = isRotated ? 'right' : 'center';
                    ctx.textBaseline = isRotated ? 'middle' : 'top';
                    if (index % this.xLabelsSkipper === 0)
                        ctx.fillText(label, 0, 0);
                    ctx.restore();
                }, this);
            }
        }
    });

    Chart.RadialScale = Chart.Element.extend({
        initialize: function() {
            this.size = helpers.min([this.height, this.width]);
            this.drawingArea = (this.display) ? (this.size / 2) - (this.fontSize / 2 + this.backdropPaddingY) : (this.size / 2);
        },

        calculateCenterOffset: function(value) {
            // Take into account half font size + the yPadding of the top value
            return (value - this.min) * (this.drawingArea / (this.max - this.min));
        },

        update: function() {
            if (!this.lineArc)
                this.setScaleSize();
            else
                this.drawingArea = (this.display) ? (this.size / 2) - (this.fontSize / 2 + this.backdropPaddingY) : (this.size / 2);
            this.buildYLabels();
        },

        buildYLabels: function() {
            this.yLabels = [];
            var stepDecimalPlaces = helpers.getDecimalPlaces(this.stepValue);
            for (var i = 0; i <= this.steps; i++)
                this.yLabels.push(helpers.template(this.templateString, { value: (this.min + (i * this.stepValue)).toFixed(stepDecimalPlaces) }));
        },

        getCircumference: function() {
            return ((Math.PI * 2) / this.valuesCount);
        },

        setScaleSize: function() {
            /*
			 * Right, this is really confusing and there is a lot of maths going on here
			 * The gist of the problem is here: https://gist.github.com/nnnick/696cc9c55f4b0beb8fe9
			 *
			 * Reaction: https://dl.dropboxusercontent.com/u/34601363/toomuchscience.gif
			 *
			 * Solution:
			 *
			 * We assume the radius of the polygon is half the size of the canvas at first
			 * at each index we check if the text overlaps.
			 *
			 * Where it does, we store that angle and that index.
			 *
			 * After finding the largest index and angle we calculate how much we need to remove
			 * from the shape radius to move the point inwards by that x.
			 *
			 * We average the left and right distances to get the maximum shape radius that can fit in the box
			 * along with labels.
			 *
			 * Once we have that, we can find the centre point for the chart, by taking the x text protrusion
			 * on each side, removing that from the size, halving it and adding the left x protrusion width.
			 *
			 * This will mean we have a shape fitted to the canvas, as large as it can be with the labels
			 * and position it in the most space efficient manner
			 *
			 * https://dl.dropboxusercontent.com/u/34601363/yeahscience.gif
			 */

            // Get maximum radius of the polygon. Either half the height (minus the text width) or half the width.
            // Use this to calculate the offset + change. - Make sure L/R protrusion is at least 0 to stop issues with centre points
            var largestPossibleRadius = helpers.min([(this.height / 2 - this.pointLabelFontSize - 5), this.width / 2]),
                pointPosition,
                i,
                textWidth,
                halfTextWidth,
                furthestRight = this.width,
                furthestRightIndex,
                furthestRightAngle,
                furthestLeft = 0,
                furthestLeftIndex,
                furthestLeftAngle,
                xProtrusionLeft,
                xProtrusionRight,
                radiusReductionRight,
                radiusReductionLeft;
            this.ctx.font = helpers.fontString(this.pointLabelFontSize, this.pointLabelFontStyle, this.fontFamily);
            for (i = 0; i < this.valuesCount; i++) {
                // 5px to space the text slightly out - similar to what we do in the draw function.
                pointPosition = this.getPointPosition(i, largestPossibleRadius);
                textWidth = this.ctx.measureText(helpers.template(this.templateString, { value: this.labels[i] })).width + 5;
                if (i === 0 || i === this.valuesCount / 2) {
                    // If we're at index zero, or exactly the middle, we're at exactly the top/bottom
                    // of the radar chart, so text will be aligned centrally, so we'll half it and compare
                    // w/left and right text sizes
                    halfTextWidth = textWidth / 2;
                    if (pointPosition.x + halfTextWidth > furthestRight) {
                        furthestRight = pointPosition.x + halfTextWidth;
                        furthestRightIndex = i;
                    }
                    if (pointPosition.x - halfTextWidth < furthestLeft) {
                        furthestLeft = pointPosition.x - halfTextWidth;
                        furthestLeftIndex = i;
                    }
                } else if (i < this.valuesCount / 2) {
                    // Less than half the values means we'll left align the text
                    if (pointPosition.x + textWidth > furthestRight) {
                        furthestRight = pointPosition.x + textWidth;
                        furthestRightIndex = i;
                    }
                } else if (i > this.valuesCount / 2) {
                    // More than half the values means we'll right align the text
                    if (pointPosition.x - textWidth < furthestLeft) {
                        furthestLeft = pointPosition.x - textWidth;
                        furthestLeftIndex = i;
                    }
                }
            }

            xProtrusionLeft = furthestLeft;
            xProtrusionRight = Math.ceil(furthestRight - this.width);
            furthestRightAngle = this.getIndexAngle(furthestRightIndex);
            furthestLeftAngle = this.getIndexAngle(furthestLeftIndex);
            radiusReductionRight = xProtrusionRight / Math.sin(furthestRightAngle + Math.PI / 2);
            radiusReductionLeft = xProtrusionLeft / Math.sin(furthestLeftAngle + Math.PI / 2);

            // Ensure we actually need to reduce the size of the chart
            radiusReductionRight = helpers.isNumber(radiusReductionRight) ? radiusReductionRight : 0;
            radiusReductionLeft = helpers.isNumber(radiusReductionLeft) ? radiusReductionLeft : 0;

            this.drawingArea = largestPossibleRadius - (radiusReductionLeft + radiusReductionRight) / 2;
            this.setCenterPoint(radiusReductionLeft, radiusReductionRight);
        },

        setCenterPoint: function(leftMovement, rightMovement) {
            var maxRight = this.width - rightMovement - this.drawingArea,
                maxLeft = leftMovement + this.drawingArea;
            this.xCenter = (maxLeft + maxRight) / 2;
            // Always vertically in the center as the text height doesn't change
            this.yCenter = (this.height / 2);
        },

        getIndexAngle: function(index) {
            // Start from the top instead of right, so remove a quarter of the circle
            return index * ((Math.PI * 2) / this.valuesCount) - (Math.PI / 2);
        },

        getPointPosition: function(index, distanceFromCenter) {
            var thisAngle = this.getIndexAngle(index);
            return {
                x: (Math.cos(thisAngle) * distanceFromCenter) + this.xCenter,
                y: (Math.sin(thisAngle) * distanceFromCenter) + this.yCenter
            };
        },

        draw: function() {
            if (this.display) {
                var ctx = this.ctx;
                helpers.each(this.yLabels, function(label, index) {
                    // Don't draw a center value
                    if (index > 0) {
                        var yCenterOffset = index * (this.drawingArea / this.steps),
                            yHeight = this.yCenter - yCenterOffset,
                            pointPosition;

                        // Draw circular lines around the scale
                        if (this.lineWidth > 0) {
                            ctx.strokeStyle = this.lineColor;
                            ctx.lineWidth = this.lineWidth;

                            if (this.lineArc) {
                                ctx.beginPath();
                                ctx.arc(this.xCenter, this.yCenter, yCenterOffset, 0, Math.PI * 2);
                                ctx.closePath();
                                ctx.stroke();
                            } else {
                                ctx.beginPath();
                                for (var i = 0; i < this.valuesCount; i++) {
                                    pointPosition = this.getPointPosition(i, this.calculateCenterOffset(this.min + (index * this.stepValue)));
                                    if (i === 0)
                                        ctx.moveTo(pointPosition.x, pointPosition.y);
                                    else
                                        ctx.lineTo(pointPosition.x, pointPosition.y);
                                }
                                ctx.closePath();
                                ctx.stroke();
                            }
                        }
                        if (this.showLabels) {
                            ctx.font = helpers.fontString(this.fontSize, this.fontStyle, this.fontFamily);
                            if (this.showLabelBackdrop) {
                                var labelWidth = ctx.measureText(label).width;
                                ctx.fillStyle = this.backdropColor;
                                ctx.fillRect(
                                    this.xCenter - labelWidth / 2 - this.backdropPaddingX,
                                    yHeight - this.fontSize / 2 - this.backdropPaddingY,
                                    labelWidth + this.backdropPaddingX * 2,
                                    this.fontSize + this.backdropPaddingY * 2
                                );
                            }
                            ctx.textAlign = 'center';
                            ctx.textBaseline = 'middle';
                            ctx.fillStyle = this.fontColor;
                            ctx.fillText(label, this.xCenter, yHeight);
                        }
                    }
                }, this);

                if (!this.lineArc) {
                    ctx.lineWidth = this.angleLineWidth;
                    ctx.strokeStyle = this.angleLineColor;
                    for (var i = this.valuesCount - 1; i >= 0; i--) {
                        var centerOffset = null, outerPosition = null;

                        if (this.angleLineWidth > 0 && (i % this.angleLineInterval === 0)) {
                            centerOffset = this.calculateCenterOffset(this.max);
                            outerPosition = this.getPointPosition(i, centerOffset);
                            ctx.beginPath();
                            ctx.moveTo(this.xCenter, this.yCenter);
                            ctx.lineTo(outerPosition.x, outerPosition.y);
                            ctx.stroke();
                            ctx.closePath();
                        }

                        if (this.backgroundColors && this.backgroundColors.length === this.valuesCount) {
                            if (centerOffset === null)
                                centerOffset = this.calculateCenterOffset(this.max);
                            if (outerPosition === null)
                                outerPosition = this.getPointPosition(i, centerOffset);
                            var previousOuterPosition = this.getPointPosition(i === 0 ? this.valuesCount - 1 : i - 1, centerOffset);
                            var nextOuterPosition = this.getPointPosition(i === this.valuesCount - 1 ? 0 : i + 1, centerOffset);
                            var previousOuterHalfway = { x: (previousOuterPosition.x + outerPosition.x) / 2, y: (previousOuterPosition.y + outerPosition.y) / 2 };
                            var nextOuterHalfway = { x: (outerPosition.x + nextOuterPosition.x) / 2, y: (outerPosition.y + nextOuterPosition.y) / 2 };

                            ctx.beginPath();
                            ctx.moveTo(this.xCenter, this.yCenter);
                            ctx.lineTo(previousOuterHalfway.x, previousOuterHalfway.y);
                            ctx.lineTo(outerPosition.x, outerPosition.y);
                            ctx.lineTo(nextOuterHalfway.x, nextOuterHalfway.y);
                            ctx.fillStyle = this.backgroundColors[i];
                            ctx.fill();
                            ctx.closePath();
                        }

                        // Extra 3px out for some label spacing
                        var pointLabelPosition = this.getPointPosition(i, this.calculateCenterOffset(this.max) + 5);
                        ctx.font = helpers.fontString(this.pointLabelFontSize, this.pointLabelFontStyle, this.fontFamily);
                        ctx.fillStyle = this.pointLabelFontColor;

                        var labelsCount = this.labels.length,
                            halfLabelsCount = this.labels.length / 2,
                            quarterLabelsCount = halfLabelsCount / 2,
                            upperHalf = (i < quarterLabelsCount || i > labelsCount - quarterLabelsCount),
                            exactQuarter = (i === quarterLabelsCount || i === labelsCount - quarterLabelsCount);
                        if (i === 0)
                            ctx.textAlign = 'center';
                        else if (i === halfLabelsCount)
                            ctx.textAlign = 'center';
                        else if (i < halfLabelsCount)
                            ctx.textAlign = 'left';
                        else
                            ctx.textAlign = 'right';

                        // Set the correct text baseline based on outer positioning
                        if (exactQuarter)
                            ctx.textBaseline = 'middle';
                        else if (upperHalf)
                            ctx.textBaseline = 'bottom';
                        else
                            ctx.textBaseline = 'top';

                        ctx.fillText(this.labels[i], pointLabelPosition.x, pointLabelPosition.y);
                    }
                }
            }
        }
    });

    root.Chart = Chart;
}(this, this.ChartHelpers));

(function(Chart, helpers) {
    'use strict';

    Chart.Type.extend({
        name: 'Bar',
        defaults: {
            // Boolean - Whether the scale should start at zero, or an order of magnitude down from the lowest value
            scaleBeginAtZero: true,

            // Boolean - Whether grid lines are shown across the chart
            scaleShowGridLines: true,

            // String - Colour of the grid lines
            scaleGridLineColor: 'rgba(0,0,0,.05)',

            // Number - Width of the grid lines
            scaleGridLineWidth: 1,

            // Boolean - Whether to show horizontal lines (except X axis)
            scaleShowHorizontalLines: true,

            // Boolean - Whether to show vertical lines (except Y axis)
            scaleShowVerticalLines: true,

            // Boolean - If there is a stroke on each bar
            barShowStroke: true,

            // Number - Pixel width of the bar stroke
            barStrokeWidth: 2,

            // Number - Spacing between each of the X value sets
            barValueSpacing: 5,

            // Number - Spacing between data sets within X values
            barDatasetSpacing: 1,

            // String - A legend template
            legendTemplate: '<ul class="chart-legend <%=name.toLowerCase()%>-legend"><% for (var i=0; i<datasets.length; i++){%><li><span class="legend-icon" style="background-color:<%=datasets[i].fillColor%>"></span><span class="legend-text"><%if(datasets[i].label){%><%=datasets[i].label%><%}%></span></li><%}%></ul>'
        },

        initialize: function(data) {
            // Expose options as a scope variable here so we can access it in the ScaleClass
            var options = this.options;

            this.ScaleClass = Chart.Scale.extend({
                offsetGridLines: true,

                calculateBarX: function(datasetCount, datasetIndex, barIndex) {
                    // Reusable method for calculating the xPosition of a given bar based on datasetIndex & width of the bar
                    var xWidth = this.calculateBaseWidth(),
                        xAbsolute = this.calculateX(barIndex) - (xWidth / 2),
                        barWidth = this.calculateBarWidth(datasetCount);

                    return xAbsolute + (barWidth * datasetIndex) + (datasetIndex * options.barDatasetSpacing) + barWidth / 2;
                },

                calculateBaseWidth: function() {
                    return (this.calculateX(1) - this.calculateX(0)) - (2 * options.barValueSpacing);
                },

                calculateBarWidth: function(datasetCount) {
                    // The padding between datasets is to the right of each bar, providing that there are more than 1 dataset
                    var baseWidth = this.calculateBaseWidth() - ((datasetCount - 1) * options.barDatasetSpacing);

                    return (baseWidth / datasetCount);
                }
            });

            this.datasets = [];

            // Set up tooltip events on the chart
            if (this.options.showTooltips) {
                helpers.bindEvents(this, this.options.tooltipEvents, function(evt) {
                    var activeBars = (evt.type !== 'mouseout') ? this.getBarsAtEvent(evt) : [];

                    this.eachBars(function(bar) {
                        bar.restore(['fillColor', 'strokeColor']);
                    });
                    helpers.each(activeBars, function(activeBar) {
                        if (activeBar) {
                            activeBar.fillColor = activeBar.highlightFill;
                            activeBar.strokeColor = activeBar.highlightStroke;
                        }
                    });
                    this.showTooltip(activeBars);
                });
            }

            // Declare the extension of the default point, to cater for the options passed in to the constructor
            this.BarClass = Chart.Rectangle.extend({
                strokeWidth: this.options.barStrokeWidth,
                showStroke: this.options.barShowStroke,
                ctx: this.chart.ctx
            });

            // Iterate through each of the datasets, and build this into a property of the chart
            helpers.each(data.datasets, function(dataset) {
                var datasetObject = {
                    label: dataset.label || null,
                    fillColor: dataset.fillColor,
                    strokeColor: dataset.strokeColor,
                    bars: []
                };

                this.datasets.push(datasetObject);

                helpers.each(dataset.data, function(dataPoint, index) {
                    // Add a new point for each piece of data, passing any required data to draw.
                    datasetObject.bars.push(new this.BarClass({
                        value: dataPoint,
                        label: data.labels[index],
                        datasetLabel: dataset.label,
                        strokeColor: (typeof dataset.strokeColor === 'object') ? dataset.strokeColor[index] : dataset.strokeColor,
                        fillColor: (typeof dataset.fillColor === 'object') ? dataset.fillColor[index] : dataset.fillColor,
                        highlightFill: (dataset.highlightFill) ? (typeof dataset.highlightFill === 'object') ? dataset.highlightFill[index] : dataset.highlightFill : (typeof dataset.fillColor === 'object') ? dataset.fillColor[index] : dataset.fillColor,
                        highlightStroke: (dataset.highlightStroke) ? (typeof dataset.highlightStroke === 'object') ? dataset.highlightStroke[index] : dataset.highlightStroke : (typeof dataset.strokeColor === 'object') ? dataset.strokeColor[index] : dataset.strokeColor
                    }));
                }, this);
            }, this);

            this.buildScale(data.labels);

            this.BarClass.prototype.base = this.scale.endPoint;

            this.eachBars(function(bar, index, datasetIndex) {
                helpers.extend(bar, {
                    width: this.scale.calculateBarWidth(this.datasets.length),
                    x: this.scale.calculateBarX(this.datasets.length, datasetIndex, index),
                    y: this.scale.endPoint
                });
                bar.save();
            }, this);

            this.render();
        },

        eachBars: function(callback) {
            helpers.each(this.datasets, function(dataset, datasetIndex) {
                helpers.each(dataset.bars, callback, this, datasetIndex);
            }, this);
        },

        getBarsAtEvent: function(e) {
            var barsArray = [],
                eventPosition = helpers.getRelativePosition(e),
                datasetIterator = function(dataset) {
                    barsArray.push(dataset.bars[barIndex]);
                },
                barIndex;

            for (var datasetIndex = 0; datasetIndex < this.datasets.length; datasetIndex++) {
                for (barIndex = 0; barIndex < this.datasets[datasetIndex].bars.length; barIndex++) {
                    if (this.datasets[datasetIndex].bars[barIndex].inRange(eventPosition.x, eventPosition.y)) {
                        helpers.each(this.datasets, datasetIterator);
                        return barsArray;
                    }
                }
            }

            return barsArray;
        },

        buildScale: function(labels) {
            var self = this;

            var dataTotal = function() {
                var values = [];
                self.eachBars(function(bar) {
                    values.push(bar.value);
                });
                return values;
            };

            this.scale = new this.ScaleClass({
                templateString: this.options.scaleLabel,
                height: this.chart.height,
                width: this.chart.width,
                ctx: this.chart.ctx,
                textColor: this.options.scaleFontColor,
                fontSize: this.options.scaleFontSize,
                fontStyle: this.options.scaleFontStyle,
                fontFamily: this.options.fontFamily,
                valuesCount: labels.length,
                beginAtZero: this.options.scaleBeginAtZero,
                integersOnly: this.options.scaleIntegersOnly,
                calculateYRange: function(currentHeight) {
                    helpers.extend(this, helpers.calculateScaleRange(
                        dataTotal(),
                        currentHeight,
                        this.fontSize,
                        this.beginAtZero,
                        this.integersOnly
                    ));
                },
                xLabels: labels,
                showXLabels: (this.options.showXLabels) ? this.options.showXLabels : true,
                font: helpers.fontString(this.options.scaleFontSize, this.options.scaleFontStyle, this.options.fontFamily),
                lineWidth: this.options.scaleLineWidth,
                lineColor: this.options.scaleLineColor,
                showHorizontalLines: this.options.scaleShowHorizontalLines,
                showVerticalLines: this.options.scaleShowVerticalLines,
                gridLineWidth: (this.options.scaleShowGridLines) ? this.options.scaleGridLineWidth : 0,
                gridLineColor: (this.options.scaleShowGridLines) ? this.options.scaleGridLineColor : 'rgba(0,0,0,0)',
                padding: (this.options.showScale) ? 0 : (this.options.barShowStroke) ? this.options.barStrokeWidth : 0,
                showLabels: this.options.scaleShowLabels,
                display: this.options.showScale
            });
        },

        reflow: function() {
            helpers.extend(this.BarClass.prototype, {
                y: this.scale.endPoint,
                base: this.scale.endPoint
            });
            this.scale.update(helpers.extend({
                height: this.chart.height,
                width: this.chart.width
            }));
        },

        draw: function(ease) {
            var easingDecimal = ease || 1;
            this.clear();

            this.scale.draw(easingDecimal);

            // Draw all the bars for each dataset
            helpers.each(this.datasets, function(dataset, datasetIndex) {
                helpers.each(dataset.bars, function(bar, index) {
                    if (bar.hasValue()) {
                        bar.base = this.scale.endPoint;
                        // Transition then draw
                        bar.transition({
                            x: this.scale.calculateBarX(this.datasets.length, datasetIndex, index),
                            y: this.scale.calculateY(bar.value),
                            width: this.scale.calculateBarWidth(this.datasets.length)
                        }, easingDecimal).draw();
                    }
                }, this);
            }, this);
        }
    });
}(this.Chart, this.ChartHelpers));

(function(Chart, helpers) {
    'use strict';

    var defaultConfig = {
        // Boolean - Whether we should show a stroke on each segment
        segmentShowStroke: true,

        // String - The colour of each segment stroke
        segmentStrokeColor: '#fff',

        // Number - The width of each segment stroke
        segmentStrokeWidth: 2,

        // The percentage of the chart that we cut out of the middle.
        percentageInnerCutout: 50,

        // String - A legend template
        legendTemplate: '<ul class="chart-legend <%=name.toLowerCase()%>-legend"><% for (var i=0; i<segments.length; i++){%><li><span class="legend-icon" style="background-color:<%=segments[i].fillColor%>"></span><span class="legend-text"><%if(segments[i].label){%><%=segments[i].label%><%}%></span></li><%}%></ul>'
    };

    Chart.Type.extend({
        // Passing in a name registers this chart in the Chart namespace
        name: 'Doughnut',
        // Providing a defaults will also register the defaults in the chart namespace
        defaults: defaultConfig,

        initialize: function(data) {
            // Declare segments as a static property to prevent inheriting across the Chart type prototype
            this.segments = [];
            this.outerRadius = (helpers.min([this.chart.width, this.chart.height]) - this.options.segmentStrokeWidth / 2) / 2;

            this.SegmentArc = Chart.Arc.extend({
                ctx: this.chart.ctx,
                x: this.chart.width / 2,
                y: this.chart.height / 2
            });

            // Set up tooltip events on the chart
            if (this.options.showTooltips) {
                helpers.bindEvents(this, this.options.tooltipEvents, function(evt) {
                    var activeSegments = (evt.type !== 'mouseout') ? this.getSegmentsAtEvent(evt) : [];

                    helpers.each(this.segments, function(segment) {
                        segment.restore(['fillColor']);
                    });
                    helpers.each(activeSegments, function(activeSegment) {
                        activeSegment.fillColor = activeSegment.highlightColor;
                    });
                    this.showTooltip(activeSegments);
                });
            }
            this.calculateTotal(data);

            helpers.each(data, function(datapoint, index) {
                if (!datapoint.color)
                    datapoint.color = 'hsl(' + (360 * index / data.length) + ', 100%, 50%)';
                this.addData(datapoint, index);
            }, this);

            this.render();
        },

        getSegmentsAtEvent: function(e) {
            var segmentsArray = [];
            var location = helpers.getRelativePosition(e);

            helpers.each(this.segments, function(segment) {
                if (segment.inRange(location.x, location.y))
                    segmentsArray.push(segment);
            }, this);
            return segmentsArray;
        },

        addData: function(segment, atIndex) {
            var index = atIndex !== undefined ? atIndex : this.segments.length;
            if (typeof segment.color === 'undefined') {
                segment.color = Chart.defaults.global.segmentColorDefault[index % Chart.defaults.global.segmentColorDefault.length];
                segment.highlight = Chart.defaults.global.segmentHighlightColorDefaults[index % Chart.defaults.global.segmentHighlightColorDefaults.length];
            }
            this.segments.splice(index, 0, new this.SegmentArc({
                value: segment.value,
                outerRadius: this.outerRadius,
                innerRadius: (this.outerRadius / 100) * this.options.percentageInnerCutout,
                fillColor: segment.color,
                highlightColor: segment.highlight || segment.color,
                showStroke: this.options.segmentShowStroke,
                strokeWidth: this.options.segmentStrokeWidth,
                strokeColor: this.options.segmentStrokeColor,
                startAngle: Math.PI * 1.5,
                circumference: this.calculateCircumference(segment.value),
                label: segment.label
            }));
        },

        calculateCircumference: function(value) {
            return this.total > 0 ? ((Math.PI * 2) * (value / this.total)) : 0;
        },

        calculateTotal: function(data) {
            this.total = 0;
            helpers.each(data, function(segment) {
                this.total += Math.abs(segment.value);
            }, this);
        },

        reflow: function() {
            helpers.extend(this.SegmentArc.prototype, {
                x: this.chart.width / 2,
                y: this.chart.height / 2
            });
            this.outerRadius = (helpers.min([this.chart.width, this.chart.height]) - this.options.segmentStrokeWidth / 2) / 2;
            helpers.each(this.segments, function(segment) {
                segment.update({
                    outerRadius: this.outerRadius,
                    innerRadius: (this.outerRadius / 100) * this.options.percentageInnerCutout
                });
            }, this);
        },

        draw: function(easeDecimal) {
            var animDecimal = (easeDecimal) ? easeDecimal : 1;
            this.clear();
            helpers.each(this.segments, function(segment, index) {
                segment.transition({
                    circumference: this.calculateCircumference(segment.value),
                    outerRadius: this.outerRadius,
                    innerRadius: (this.outerRadius / 100) * this.options.percentageInnerCutout
                }, animDecimal);

                segment.endAngle = segment.startAngle + segment.circumference;

                segment.draw();
                if (index === 0)
                    segment.startAngle = Math.PI * 1.5;
                // Check to see if it's the last segment, if not get the next and update the start angle
                if (index < this.segments.length - 1)
                    this.segments[index + 1].startAngle = segment.endAngle;
            }, this);
        }
    });

    Chart.types.Doughnut.extend({
        name: 'Pie',
        defaults: helpers.merge(defaultConfig, { percentageInnerCutout: 0 })
    });
}(this.Chart, this.ChartHelpers));

/**
 * https://github.com/tomsouthall/Chart.HorizontalBar.js
 */
(function(Chart, helpers) {
    'use strict';

    Chart.HorizontalRectangle = Chart.Element.extend({
        draw: function() {
            var ctx = this.ctx,
                halfHeight = this.height / 2,
                top = this.y - halfHeight,
                bottom = this.y + halfHeight,
                right = this.left - (this.left - this.x),
                halfStroke = this.strokeWidth / 2;

            // Canvas doesn't allow us to stroke inside the width so we can
            // adjust the sizes to fit if we're setting a stroke on the line
            if (this.showStroke) {
                top += halfStroke;
                bottom -= halfStroke;
                right += halfStroke;
            }

            ctx.beginPath();
            ctx.fillStyle = this.fillColor;
            ctx.strokeStyle = this.strokeColor;
            ctx.lineWidth = this.strokeWidth;

            // It'd be nice to keep this class totally generic to any rectangle and simply specify which border to miss out.
            ctx.moveTo(this.left, top);
            ctx.lineTo(right, top);
            ctx.lineTo(right, bottom);
            ctx.lineTo(this.left, bottom);
            ctx.fill();
            if (this.showStroke)
                ctx.stroke();
        },

        inRange: function(chartX, chartY) {
            return (chartX >= this.left && chartX <= this.x && chartY >= (this.y - this.height / 2) && chartY <= (this.y + this.height / 2));
        }
    });

    Chart.Type.extend({
        name: 'HorizontalBar',
        defaults: {
            // Boolean - Whether the scale should start at zero, or an order of magnitude down from the lowest value
            scaleBeginAtZero: true,

            // Boolean - Whether grid lines are shown across the chart
            scaleShowGridLines: true,

            // String - Colour of the grid lines
            scaleGridLineColor: 'rgba(0,0,0,.05)',

            // Number - Width of the grid lines
            scaleGridLineWidth: 1,

            // Boolean - Whether to show horizontal lines (except X axis)
            scaleShowHorizontalLines: true,

            // Boolean - Whether to show vertical lines (except Y axis)
            scaleShowVerticalLines: true,

            // Boolean - If there is a stroke on each bar
            barShowStroke: true,

            // Number - Pixel width of the bar stroke
            barStrokeWidth: 2,

            // Number - Spacing between each of the X value sets
            barValueSpacing: 5,

            // Number - Spacing between data sets within X values
            barDatasetSpacing: 1,

            // String - A legend template
            legendTemplate: '<ul class="chart-legend <%=name.toLowerCase()%>-legend"><% for (var i=0; i<datasets.length; i++){%><li><span class="legend-icon" style="background-color:<%=datasets[i].fillColor%>"></span><span class="legend-text"><%if(datasets[i].label){%><%=datasets[i].label%><%}%></span></li><%}%></ul>'
        },

        initialize: function(data) {
            var options = this.options;

            this.ScaleClass = Chart.Scale.extend({
                offsetGridLines: true,

                calculateBaseHeight: function() {
                    return ((this.endPoint - this.startPoint) / this.yLabels.length) - (2 * options.barValueSpacing);
                },

                calculateBarHeight: function(datasetCount) {
                    // The padding between datasets is to the right of each bar, providing that there are more than 1 dataset
                    var baseHeight = this.calculateBaseHeight() - ((datasetCount) * options.barDatasetSpacing);
                    return (baseHeight / datasetCount);
                },

                calculateXInvertXY: function(value) {
                    var scalingFactor = (this.width - Math.round(this.xScalePaddingLeft) - this.xScalePaddingRight) / (this.max - this.min);
                    return Math.round(this.xScalePaddingLeft) + (scalingFactor * (value - this.min));
                },

                calculateYInvertXY: function(index) {
                    return index * ((this.startPoint - this.endPoint) / (this.yLabels.length));
                },

                calculateBarY: function(datasetCount, datasetIndex, barIndex) {
                    // Reusable method for calculating the yPosition of a given bar based on datasetIndex & height of the bar
                    var yHeight = this.calculateBaseHeight(),
                        yAbsolute = (this.endPoint + this.calculateYInvertXY(barIndex) - (yHeight / 2)) - 5,
                        barHeight = this.calculateBarHeight(datasetCount);
                    if (datasetCount > 1)
                        yAbsolute = yAbsolute + (barHeight * (datasetIndex - 1)) - (datasetIndex * options.barDatasetSpacing) + barHeight / 2;
                    return yAbsolute;
                },

                buildCalculatedLabels: function() {
                    this.calculatedLabels = [];
                    var stepDecimalPlaces = helpers.getDecimalPlaces(this.stepValue);

                    for (var i = 0; i <= this.steps; i++) {
                        this.calculatedLabels.push(helpers.template(this.templateString, { value: (this.min + (i * this.stepValue)).toFixed(stepDecimalPlaces) }));
                    }
                },

                buildYLabels: function() {
                    this.buildYLabelCounter = (typeof this.buildYLabelCounter === 'undefined') ? 0 : this.buildYLabelCounter + 1;
                    this.buildCalculatedLabels();
                    if (this.buildYLabelCounter === 0)
                        this.yLabels = this.xLabels;
                    this.xLabels = this.calculatedLabels;
                    this.yLabelWidth = (this.display && this.showLabels) ? helpers.longestText(this.ctx, this.font, this.yLabels) + this.fontSize + 3 : 0;
                },

                calculateX: function(index) {
                    var innerWidth = this.width - (this.xScalePaddingLeft + this.xScalePaddingRight),
                        valueWidth = innerWidth / (this.steps - ((this.offsetGridLines) ? 0 : 1)),
                        valueOffset = (valueWidth * index) + this.xScalePaddingLeft;
                    if (this.offsetGridLines)
                        valueOffset += (valueWidth / 2);
                    return Math.round(valueOffset);
                },

                draw: function() {
                    var ctx = this.ctx,
                        yLabelGap = (this.endPoint - this.startPoint) / this.yLabels.length,
                        xStart = Math.round(this.xScalePaddingLeft);
                    if (this.display) {
                        ctx.fillStyle = this.textColor;
                        ctx.font = this.font;
                        helpers.each(this.yLabels, function(labelString, index) {
                            var yLabelCenter = this.endPoint - (yLabelGap * index),
                                linePositionY = Math.round(yLabelCenter),
                                drawHorizontalLine = this.showHorizontalLines;

                            yLabelCenter -= yLabelGap / 2;

                            ctx.textAlign = 'right';
                            ctx.textBaseline = 'middle';
                            if (this.showLabels)
                                ctx.fillText(labelString, xStart - 10, yLabelCenter);

                            if (index === 0 && !drawHorizontalLine)
                                drawHorizontalLine = true;
                            if (drawHorizontalLine)
                                ctx.beginPath();

                            if (index > 0) {
                                // This is a grid line in the centre, so drop that
                                ctx.lineWidth = this.gridLineWidth;
                                ctx.strokeStyle = this.gridLineColor;
                            } else {
                                // This is the first line on the scale
                                ctx.lineWidth = this.lineWidth;
                                ctx.strokeStyle = this.lineColor;
                            }

                            linePositionY += helpers.aliasPixel(ctx.lineWidth);

                            if (drawHorizontalLine) {
                                ctx.moveTo(xStart, linePositionY);
                                ctx.lineTo(this.width, linePositionY);
                                ctx.stroke();
                                ctx.closePath();
                            }

                            ctx.lineWidth = this.lineWidth;
                            ctx.strokeStyle = this.lineColor;
                            ctx.beginPath();
                            ctx.moveTo(xStart - 5, linePositionY);
                            ctx.lineTo(xStart, linePositionY);
                            ctx.stroke();
                            ctx.closePath();
                        }, this);

                        helpers.each(this.xLabels, function(label, index) {
                            var width = this.calculateX(1) - this.calculateX(0);
                            var xPos = this.calculateX(index) + helpers.aliasPixel(this.lineWidth) - (width / 2),
                                // Check to see if line/bar here and decide where to place the line
                                linePos = this.calculateX(index - (this.offsetGridLines ? 0.5 : 0)) + helpers.aliasPixel(this.lineWidth),
                                isRotated = (this.xLabelRotation > 0);

                            ctx.beginPath();

                            if (index > 0) {
                                // This is a grid line in the centre, so drop that
                                ctx.lineWidth = this.gridLineWidth;
                                ctx.strokeStyle = this.gridLineColor;
                            } else {
                                // This is the first line on the scale
                                ctx.lineWidth = this.lineWidth;
                                ctx.strokeStyle = this.lineColor;
                            }
                            ctx.moveTo(linePos, this.endPoint);
                            ctx.lineTo(linePos, this.startPoint - 3);
                            ctx.stroke();
                            ctx.closePath();

                            ctx.lineWidth = this.lineWidth;
                            ctx.strokeStyle = this.lineColor;

                            // Small lines at the bottom of the base grid line
                            ctx.beginPath();
                            ctx.moveTo(linePos, this.endPoint);
                            ctx.lineTo(linePos, this.endPoint + 5);
                            ctx.stroke();
                            ctx.closePath();

                            ctx.save();
                            ctx.translate(xPos, (isRotated) ? this.endPoint + 12 : this.endPoint + 8);
                            ctx.rotate(helpers.toRadians(this.xLabelRotation) * -1);
                            ctx.font = this.font;
                            ctx.textAlign = (isRotated) ? 'right' : 'center';
                            ctx.textBaseline = (isRotated) ? 'middle' : 'top';
                            ctx.fillText(label, 0, 0);
                            ctx.restore();
                        }, this);
                    }
                }
            });

            this.datasets = [];

            // Set up tooltip events on the chart
            if (this.options.showTooltips) {
                helpers.bindEvents(this, this.options.tooltipEvents, function(evt) {
                    var activeBars = (evt.type !== 'mouseout') ? this.getBarsAtEvent(evt) : [];

                    this.eachBars(function(bar) {
                        bar.restore(['fillColor', 'strokeColor']);
                    });
                    helpers.each(activeBars, function(activeBar) {
                        activeBar.fillColor = activeBar.highlightFill;
                        activeBar.strokeColor = activeBar.highlightStroke;
                    });
                    this.showTooltip(activeBars);
                });
            }

            // Declare the extension of the default point, to cater for the options passed in to the constructor
            this.BarClass = Chart.HorizontalRectangle.extend({
                strokeWidth: this.options.barStrokeWidth,
                showStroke: this.options.barShowStroke,
                ctx: this.chart.ctx
            });

            // Iterate through each of the datasets, and build this into a property of the chart
            helpers.each(data.datasets, function(dataset) {
                var datasetObject = {
                    label: dataset.label || null,
                    fillColor: dataset.fillColor,
                    strokeColor: dataset.strokeColor,
                    bars: []
                };
                this.datasets.push(datasetObject);

                helpers.each(dataset.data, function(dataPoint, index) {
                    // Add a new point for each piece of data, passing any required data to draw.
                    datasetObject.bars.push(new this.BarClass({
                        value: dataPoint,
                        label: data.labels[index],
                        datasetLabel: dataset.label,
                        strokeColor: dataset.strokeColor,
                        fillColor: dataset.fillColor,
                        highlightFill: dataset.highlightFill || dataset.fillColor,
                        highlightStroke: dataset.highlightStroke || dataset.strokeColor
                    }));
                }, this);
            }, this);

            this.buildScale(data.labels);

            this.BarClass.prototype.left = Math.round(this.scale.xScalePaddingLeft);

            this.eachBars(function(bar, index, datasetIndex) {
                helpers.extend(bar, {
                    x: Math.round(this.scale.xScalePaddingLeft),
                    y: this.scale.calculateBarY(this.datasets.length, datasetIndex, index),
                    height: this.scale.calculateBarHeight(this.datasets.length)
                });
                bar.save();
            }, this);

            this.render();
        },

        eachBars: function(callback) {
            helpers.each(this.datasets, function(dataset, datasetIndex) {
                helpers.each(dataset.bars, callback, this, datasetIndex);
            }, this);
        },

        getBarsAtEvent: function(e) {
            var barsArray = [],
                eventPosition = helpers.getRelativePosition(e),
                datasetIterator = function(dataset) {
                    barsArray.push(dataset.bars[barIndex]);
                },
                barIndex;

            for (var datasetIndex = 0; datasetIndex < this.datasets.length; datasetIndex++) {
                for (barIndex = 0; barIndex < this.datasets[datasetIndex].bars.length; barIndex++) {
                    if (this.datasets[datasetIndex].bars[barIndex].inRange(eventPosition.x, eventPosition.y)) {
                        helpers.each(this.datasets, datasetIterator);
                        return barsArray;
                    }
                }
            }

            return barsArray;
        },

        buildScale: function(labels) {
            var self = this;

            var dataTotal = function() {
                var values = [];
                self.eachBars(function(bar) {
                    values.push(bar.value);
                });
                return values;
            };

            this.scale = new this.ScaleClass({
                templateString: this.options.scaleLabel,
                height: this.chart.height,
                width: this.chart.width,
                ctx: this.chart.ctx,
                textColor: this.options.scaleFontColor,
                fontSize: this.options.scaleFontSize,
                fontStyle: this.options.scaleFontStyle,
                fontFamily: this.options.fontFamily,
                valuesCount: labels.length,
                beginAtZero: this.options.scaleBeginAtZero,
                integersOnly: this.options.scaleIntegersOnly,
                calculateYRange: function(currentHeight) {
                    helpers.extend(this, helpers.calculateScaleRange(
                        dataTotal(),
                        currentHeight,
                        this.fontSize,
                        this.beginAtZero,
                        this.integersOnly
                    ));
                },
                xLabels: labels,
                font: helpers.fontString(this.options.scaleFontSize, this.options.scaleFontStyle, this.options.fontFamily),
                lineWidth: this.options.scaleLineWidth,
                lineColor: this.options.scaleLineColor,
                showHorizontalLines: this.options.scaleShowHorizontalLines,
                showVerticalLines: this.options.scaleShowVerticalLines,
                gridLineWidth: (this.options.scaleShowGridLines) ? this.options.scaleGridLineWidth : 0,
                gridLineColor: (this.options.scaleShowGridLines) ? this.options.scaleGridLineColor : 'rgba(0,0,0,0)',
                padding: (this.options.showScale) ? 0 : (this.options.barShowStroke) ? this.options.barStrokeWidth : 0,
                showLabels: this.options.scaleShowLabels,
                display: this.options.showScale
            });
        },

        reflow: function() {
            helpers.extend(this.BarClass.prototype, {
                y: this.scale.endPoint,
                base: this.scale.endPoint
            });
            var newScaleProps = helpers.extend({
                height: this.chart.height,
                width: this.chart.width
            });

            this.scale.update(newScaleProps);
        },

        draw: function(ease) {
            var easingDecimal = ease || 1;
            this.clear();

            this.scale.draw(easingDecimal);

            // Draw all the bars for each dataset
            helpers.each(this.datasets, function(dataset, datasetIndex) {
                helpers.each(dataset.bars, function(bar, index) {
                    if (bar.hasValue()) {
                        bar.left = Math.round(this.scale.xScalePaddingLeft);
                        // Transition then draw
                        bar.transition({
                            x: this.scale.calculateXInvertXY(bar.value),
                            y: this.scale.calculateBarY(this.datasets.length, datasetIndex, index),
                            height: this.scale.calculateBarHeight(this.datasets.length)
                        }, easingDecimal).draw();
                    }
                }, this);
            }, this);
        }
    });
}(this.Chart, this.ChartHelpers));

(function(Chart, helpers) {
    'use strict';

    Chart.Type.extend({
        name: 'Line',
        defaults: {
            // Boolean - Whether grid lines are shown across the chart
            scaleShowGridLines: true,

            // String - Colour of the grid lines
            scaleGridLineColor: 'rgba(0,0,0,.05)',

            // Number - Width of the grid lines
            scaleGridLineWidth: 1,

            // Boolean - Whether to show horizontal lines (except X axis)
            scaleShowHorizontalLines: true,

            // Boolean - Whether to show vertical lines (except Y axis)
            scaleShowVerticalLines: true,

            // Number - Tension of the bezier curve between points
            bezierCurveTension: 0.4,

            // Boolean - Whether to show a dot for each point
            pointDot: true,

            // Number - Radius of each point dot in pixels
            pointDotRadius: 4,

            // Number - Pixel width of point dot stroke
            pointDotStrokeWidth: 1,

            // Number - amount extra to add to the radius to cater for hit detection outside the drawn point
            pointHitDetectionRadius: 10,

            // Number - Pixel width of dataset stroke
            datasetStrokeWidth: 2,

            // String - A legend template
            legendTemplate: '<ul class="chart-legend <%=name.toLowerCase()%>-legend"><% for (var i=0; i<datasets.length; i++){%><li><span class="legend-icon" style="background-color:<%=datasets[i].strokeColor%>"></span><span class="legend-text"><%if(datasets[i].label){%><%=datasets[i].label%><%}%></span></li><%}%></ul>',

            // Boolean - Whether to horizontally center the label and point dot inside the grid
            offsetGridLines: false
        },

        initialize: function(data) {
            // Declare the extension of the default point, to cater for the options passed in to the constructor
            this.PointClass = Chart.Point.extend({
                offsetGridLines: this.options.offsetGridLines,
                strokeWidth: this.options.pointDotStrokeWidth,
                radius: this.options.pointDotRadius,
                display: this.options.pointDot,
                hitDetectionRadius: this.options.pointHitDetectionRadius,
                ctx: this.chart.ctx,
                inRange: function(mouseX) {
                    return (Math.pow(mouseX - this.x, 2) < Math.pow(this.radius + this.hitDetectionRadius, 2));
                }
            });

            this.datasets = [];

            // Set up tooltip events on the chart
            if (this.options.showTooltips) {
                helpers.bindEvents(this, this.options.tooltipEvents, function(evt) {
                    var activePoints = (evt.type !== 'mouseout') ? this.getPointsAtEvent(evt) : [];
                    this.eachPoints(function(point) {
                        point.restore(['fillColor', 'strokeColor']);
                    });
                    helpers.each(activePoints, function(activePoint) {
                        activePoint.fillColor = activePoint.highlightFill;
                        activePoint.strokeColor = activePoint.highlightStroke;
                    });
                    this.showTooltip(activePoints);
                });
            }

            // Iterate through each of the datasets, and build this into a property of the chart
            helpers.each(data.datasets, function(dataset) {
                var datasetObject = {
                    label: dataset.label || null,
                    fillColor: dataset.fillColor,
                    strokeColor: dataset.strokeColor,
                    pointColor: dataset.pointColor,
                    pointStrokeColor: dataset.pointStrokeColor,
                    points: []
                };
                this.datasets.push(datasetObject);

                helpers.each(dataset.data, function(dataPoint, index) {
                    // Add a new point for each piece of data, passing any required data to draw.
                    datasetObject.points.push(new this.PointClass({
                        value: dataPoint,
                        label: data.labels[index],
                        datasetLabel: dataset.label,
                        strokeColor: dataset.pointStrokeColor,
                        fillColor: dataset.pointColor,
                        highlightFill: dataset.pointHighlightFill || dataset.pointColor,
                        highlightStroke: dataset.pointHighlightStroke || dataset.pointStrokeColor
                    }));
                }, this);

                this.buildScale(data.labels);

                this.eachPoints(function(point, index) {
                    helpers.extend(point, {
                        x: this.scale.calculateX(index),
                        y: this.scale.endPoint
                    });
                    point.save();
                }, this);
            }, this);

            this.render();
        },

        eachPoints: function(callback) {
            helpers.each(this.datasets, function(dataset) {
                helpers.each(dataset.points, callback, this);
            }, this);
        },

        getPointsAtEvent: function(e) {
            var pointsArray = [],
                eventPosition = helpers.getRelativePosition(e);
            helpers.each(this.datasets, function(dataset) {
                helpers.each(dataset.points, function(point) {
                    if (point.inRange(eventPosition.x, eventPosition.y))
                        pointsArray.push(point);
                });
            }, this);
            return pointsArray;
        },

        buildScale: function(labels) {
            var self = this;

            var dataTotal = function() {
                var values = [];
                self.eachPoints(function(point) {
                    values.push(point.value);
                });
                return values;
            };

            this.scale = new Chart.Scale({
                templateString: this.options.scaleLabel,
                height: this.chart.height,
                width: this.chart.width,
                ctx: this.chart.ctx,
                textColor: this.options.scaleFontColor,
                offsetGridLines: this.options.offsetGridLines,
                fontSize: this.options.scaleFontSize,
                fontStyle: this.options.scaleFontStyle,
                fontFamily: this.options.fontFamily,
                valuesCount: labels.length,
                beginAtZero: this.options.scaleBeginAtZero,
                integersOnly: this.options.scaleIntegersOnly,
                calculateYRange: function(currentHeight) {
                    helpers.extend(this, helpers.calculateScaleRange(
                        dataTotal(),
                        currentHeight,
                        this.fontSize,
                        this.beginAtZero,
                        this.integersOnly
                    ));
                },
                xLabels: labels,
                showXLabels: (this.options.showXLabels) ? this.options.showXLabels : true,
                font: helpers.fontString(this.options.scaleFontSize, this.options.scaleFontStyle, this.options.fontFamily),
                lineWidth: this.options.scaleLineWidth,
                lineColor: this.options.scaleLineColor,
                showHorizontalLines: this.options.scaleShowHorizontalLines,
                showVerticalLines: this.options.scaleShowVerticalLines,
                gridLineWidth: (this.options.scaleShowGridLines) ? this.options.scaleGridLineWidth : 0,
                gridLineColor: (this.options.scaleShowGridLines) ? this.options.scaleGridLineColor : 'rgba(0,0,0,0)',
                padding: (this.options.showScale) ? 0 : this.options.pointDotRadius + this.options.pointDotStrokeWidth,
                showLabels: this.options.scaleShowLabels,
                display: this.options.showScale
            });
        },

        reflow: function() {
            this.scale.update(helpers.extend({
                height: this.chart.height,
                width: this.chart.width
            }));
        },

        draw: function(ease) {
            var easingDecimal = ease || 1;
            this.clear();

            var ctx = this.chart.ctx;

            // Some helper methods for getting the next/prev points
            var hasValue = function(item) { return item.value !== null; },
                nextPoint = function(point, collection, index) { return helpers.findNextWhere(collection, hasValue, index) || point; },
                previousPoint = function(point, collection, index) { return helpers.findPreviousWhere(collection, hasValue, index) || point; };

            if (!this.scale)
                return;
            this.scale.draw(easingDecimal);

            helpers.each(this.datasets, function(dataset) {
                var pointsWithValues = helpers.where(dataset.points, hasValue);

                // Transition each point first so that the line and point drawing isn't out of sync
                // We can use this extra loop to calculate the control points of this dataset also in this loop
                helpers.each(dataset.points, function(point, index) {
                    if (point.hasValue()) {
                        point.transition({
                            y: this.scale.calculateY(point.value),
                            x: this.scale.calculateX(index)
                        }, easingDecimal);
                    }
                }, this);

                // Control points need to be calculated in a separate loop, because we need to know the current x/y of the point
                // This would cause issues because the y of the next point would be 0, so beziers would be skewed
                helpers.each(pointsWithValues, function(point, index) {
                    point.controlPoints = helpers.splineCurve(
                        previousPoint(point, pointsWithValues, index),
                        point,
                        nextPoint(point, pointsWithValues, index),
                        index > 0 && index < pointsWithValues.length - 1 ? this.options.bezierCurveTension : 0
                    );

                    // Prevent the bezier going outside of the bounds of the graph
                    // Cap puter bezier handles to the upper/lower scale bounds
                    if (point.controlPoints.outer.y > this.scale.endPoint)
                        point.controlPoints.outer.y = this.scale.endPoint;
                    else if (point.controlPoints.outer.y < this.scale.startPoint)
                        point.controlPoints.outer.y = this.scale.startPoint;

                    // Cap inner bezier handles to the upper/lower scale bounds
                    if (point.controlPoints.inner.y > this.scale.endPoint)
                        point.controlPoints.inner.y = this.scale.endPoint;
                    else if (point.controlPoints.inner.y < this.scale.startPoint)
                        point.controlPoints.inner.y = this.scale.startPoint;
                }, this);

                // Draw the line between all the points
                ctx.lineWidth = this.options.datasetStrokeWidth;
                ctx.strokeStyle = dataset.strokeColor;
                ctx.beginPath();

                helpers.each(pointsWithValues, function(point, index) {
                    if (index === 0) {
                        ctx.moveTo(point.x, point.y);
                    } else {
                        var previous = previousPoint(point, pointsWithValues, index);
                        ctx.bezierCurveTo(
                            previous.controlPoints.outer.x,
                            previous.controlPoints.outer.y,
                            point.controlPoints.inner.x,
                            point.controlPoints.inner.y,
                            point.x,
                            point.y
                        );
                    }
                }, this);

                if (this.options.datasetStroke)
                    ctx.stroke();

                if (pointsWithValues.length > 0) {
                    // Round off the line by going to the base of the chart, back to the start, then fill.
                    ctx.lineTo(pointsWithValues[pointsWithValues.length - 1].x, this.scale.endPoint);
                    ctx.lineTo(pointsWithValues[0].x, this.scale.endPoint);
                    ctx.fillStyle = dataset.fillColor;
                    ctx.closePath();
                    ctx.fill();
                }

                // Now draw the points over the line
                // A little inefficient double looping, but better than the line
                // lagging behind the point positions
                helpers.each(pointsWithValues, function(point) {
                    point.draw();
                });
            }, this);
        }
    });
}(this.Chart, this.ChartHelpers));

(function(Chart, helpers) {
    'use strict';

    Chart.Type.extend({
        // Passing in a name registers this chart in the Chart namespace
        name: 'PolarArea',
        // Providing a defaults will also register the defaults in the chart namespace
        defaults: {
            // Boolean - Show a backdrop to the scale label
            scaleShowLabelBackdrop: true,

            // String - The colour of the label backdrop
            scaleBackdropColor: 'rgba(255,255,255,0.75)',

            // Boolean - Whether the scale should begin at zero
            scaleBeginAtZero: true,

            // Number - The backdrop padding above & below the label in pixels
            scaleBackdropPaddingY: 2,

            // Number - The backdrop padding to the side of the label in pixels
            scaleBackdropPaddingX: 2,

            // Boolean - Show line for each value in the scale
            scaleShowLine: true,

            // Boolean - Stroke a line around each segment in the chart
            segmentShowStroke: true,

            // String - The colour of the stroke on each segment.
            segmentStrokeColor: '#fff',

            // Number - The width of the stroke value in pixels
            segmentStrokeWidth: 2,

            // String - A legend template
            legendTemplate: '<ul class="chart-legend <%=name.toLowerCase()%>-legend"><% for (var i=0; i<segments.length; i++){%><li><span class="legend-icon" style="background-color:<%=segments[i].fillColor%>"></span><span class="legend-text"><%if(segments[i].label){%><%=segments[i].label%><%}%></span></li><%}%></ul>'
        },

        // Initialize is fired when the chart is initialized - Data is passed in as a parameter
        // Config is automatically merged by the core of Chart.js, and is available at this.options
        initialize: function(data) {
            this.segments = [];
            // Declare segment class as a chart instance specific class, so it can share props for this instance
            this.SegmentArc = Chart.Arc.extend({
                showStroke: this.options.segmentShowStroke,
                strokeWidth: this.options.segmentStrokeWidth,
                strokeColor: this.options.segmentStrokeColor,
                ctx: this.chart.ctx,
                innerRadius: 0,
                x: this.chart.width / 2,
                y: this.chart.height / 2
            });
            this.scale = new Chart.RadialScale({
                display: this.options.showScale,
                fontStyle: this.options.scaleFontStyle,
                fontSize: this.options.scaleFontSize,
                fontFamily: this.options.fontFamily,
                fontColor: this.options.scaleFontColor,
                showLabels: this.options.scaleShowLabels,
                showLabelBackdrop: this.options.scaleShowLabelBackdrop,
                backdropColor: this.options.scaleBackdropColor,
                backdropPaddingY: this.options.scaleBackdropPaddingY,
                backdropPaddingX: this.options.scaleBackdropPaddingX,
                lineWidth: (this.options.scaleShowLine) ? this.options.scaleLineWidth : 0,
                lineColor: this.options.scaleLineColor,
                lineArc: true,
                width: this.chart.width,
                height: this.chart.height,
                xCenter: this.chart.width / 2,
                yCenter: this.chart.height / 2,
                ctx: this.chart.ctx,
                templateString: this.options.scaleLabel,
                valuesCount: data.length
            });

            this.updateScaleRange(data);

            this.scale.update();

            helpers.each(data, function(segment, index) {
                this.addData(segment, index, true);
            }, this);

            // Set up tooltip events on the chart
            if (this.options.showTooltips) {
                helpers.bindEvents(this, this.options.tooltipEvents, function(evt) {
                    var activeSegments = (evt.type !== 'mouseout') ? this.getSegmentsAtEvent(evt) : [];
                    helpers.each(this.segments, function(segment) {
                        segment.restore(['fillColor']);
                    });
                    helpers.each(activeSegments, function(activeSegment) {
                        activeSegment.fillColor = activeSegment.highlightColor;
                    });
                    this.showTooltip(activeSegments);
                });
            }

            this.render();
        },

        getSegmentsAtEvent: function(e) {
            var segmentsArray = [];
            var location = helpers.getRelativePosition(e);

            helpers.each(this.segments, function(segment) {
                if (segment.inRange(location.x, location.y))
                    segmentsArray.push(segment);
            }, this);
            return segmentsArray;
        },

        addData: function(segment, atIndex) {
            var index = atIndex || this.segments.length;
            if (typeof segment.color === 'undefined') {
                segment.color = Chart.defaults.global.segmentColorDefault[index % Chart.defaults.global.segmentColorDefault.length];
                segment.highlight = Chart.defaults.global.segmentHighlightColorDefaults[index % Chart.defaults.global.segmentHighlightColorDefaults.length];
            }

            this.segments.splice(index, 0, new this.SegmentArc({
                fillColor: segment.color,
                highlightColor: segment.highlight || segment.color,
                label: segment.label,
                value: segment.value,
                outerRadius: this.scale.calculateCenterOffset(segment.value),
                circumference: this.scale.getCircumference(),
                startAngle: Math.PI * 1.5
            }));
        },

        calculateTotal: function(data) {
            this.total = 0;
            helpers.each(data, function(segment) {
                this.total += segment.value;
            }, this);
            this.scale.valuesCount = this.segments.length;
        },

        updateScaleRange: function(datapoints) {
            var valuesArray = [];
            helpers.each(datapoints, function(segment) {
                valuesArray.push(segment.value);
            });

            helpers.extend(
                this.scale,
                helpers.calculateScaleRange(
                    valuesArray,
                    helpers.min([this.chart.width, this.chart.height]) / 2,
                    this.options.scaleFontSize,
                    this.options.scaleBeginAtZero,
                    this.options.scaleIntegersOnly
                ),
                {
                    size: helpers.min([this.chart.width, this.chart.height]),
                    xCenter: this.chart.width / 2,
                    yCenter: this.chart.height / 2
                }
            );
        },

        reflow: function() {
            helpers.extend(this.SegmentArc.prototype, {
                x: this.chart.width / 2,
                y: this.chart.height / 2
            });
            this.updateScaleRange(this.segments);
            this.scale.update();

            helpers.extend(this.scale, {
                xCenter: this.chart.width / 2,
                yCenter: this.chart.height / 2
            });

            helpers.each(this.segments, function(segment) {
                segment.update({
                    outerRadius: this.scale.calculateCenterOffset(segment.value)
                });
            }, this);
        },

        draw: function(ease) {
            var easingDecimal = ease || 1;
            // Clear & draw the canvas
            this.clear();
            helpers.each(this.segments, function(segment, index) {
                segment.transition({
                    circumference: this.scale.getCircumference(),
                    outerRadius: this.scale.calculateCenterOffset(segment.value)
                }, easingDecimal);

                segment.endAngle = segment.startAngle + segment.circumference;

                // If we've removed the first segment we need to set the first one to start at the top.
                if (index === 0)
                    segment.startAngle = Math.PI * 1.5;

                // Check to see if it's the last segment, if not get the next and update the start angle
                if (index < this.segments.length - 1)
                    this.segments[index + 1].startAngle = segment.endAngle;

                segment.draw();
            }, this);
            this.scale.draw();
        }
    });
}(this.Chart, this.ChartHelpers));

(function(Chart, helpers) {
    'use strict';

    Chart.Type.extend({
        name: 'Radar',
        defaults: {
            // Boolean - Whether to show lines for each scale point
            scaleShowLine: true,

            // Boolean - Whether we show the angle lines out of the radar
            angleShowLineOut: true,

            // Boolean - Whether to show labels on the scale
            scaleShowLabels: false,

            // Boolean - Whether the scale should begin at zero
            scaleBeginAtZero: true,

            // String - Colour of the angle line
            angleLineColor: 'rgba(0,0,0,.1)',

            // Number - Pixel width of the angle line
            angleLineWidth: 1,

            // Number - Interval at which to draw angle lines ("every Nth point")
            angleLineInterval: 1,

            // String - Point label font weight
            pointLabelFontStyle: 'normal',

            // Number - Point label font size in pixels
            pointLabelFontSize: 10,

            // String - Point label font colour
            pointLabelFontColor: '#666',

            // Boolean - Whether to show a dot for each point
            pointDot: true,

            // Number - Radius of each point dot in pixels
            pointDotRadius: 3,

            // Number - Pixel width of point dot stroke
            pointDotStrokeWidth: 1,

            // Number - amount extra to add to the radius to cater for hit detection outside the drawn point
            pointHitDetectionRadius: 10,

            // Number - Pixel width of dataset stroke
            datasetStrokeWidth: 2,

            // String - A legend template
            legendTemplate: '<ul class="chart-legend <%=name.toLowerCase()%>-legend"><% for (var i=0; i<datasets.length; i++){%><li><span class="legend-icon" style="background-color:<%=datasets[i].strokeColor%>"></span><span class="legend-text"><%if(datasets[i].label){%><%=datasets[i].label%><%}%></span></li><%}%></ul>'
        },

        initialize: function(data) {
            this.PointClass = Chart.Point.extend({
                strokeWidth: this.options.pointDotStrokeWidth,
                radius: this.options.pointDotRadius,
                display: this.options.pointDot,
                hitDetectionRadius: this.options.pointHitDetectionRadius,
                ctx: this.chart.ctx
            });

            this.datasets = [];

            this.buildScale(data);

            // Set up tooltip events on the chart
            if (this.options.showTooltips) {
                helpers.bindEvents(this, this.options.tooltipEvents, function(evt) {
                    var activePointsCollection = (evt.type !== 'mouseout') ? this.getPointsAtEvent(evt) : [];

                    this.eachPoints(function(point) {
                        point.restore(['fillColor', 'strokeColor']);
                    });
                    helpers.each(activePointsCollection, function(activePoint) {
                        activePoint.fillColor = activePoint.highlightFill;
                        activePoint.strokeColor = activePoint.highlightStroke;
                    });

                    this.showTooltip(activePointsCollection);
                });
            }

            // Iterate through each of the datasets, and build this into a property of the chart
            helpers.each(data.datasets, function(dataset) {
                var datasetObject = {
                    label: dataset.label || null,
                    fillColor: dataset.fillColor,
                    strokeColor: dataset.strokeColor,
                    pointColor: dataset.pointColor,
                    pointStrokeColor: dataset.pointStrokeColor,
                    points: []
                };

                this.datasets.push(datasetObject);

                helpers.each(dataset.data, function(dataPoint, index) {
                    // Add a new point for each piece of data, passing any required data to draw.
                    var pointPosition = this.scale.getPointPosition(index, this.scale.calculateCenterOffset(dataPoint));
                    datasetObject.points.push(new this.PointClass({
                        value: dataPoint,
                        label: data.labels[index],
                        datasetLabel: dataset.label,
                        x: pointPosition.x,
                        y: pointPosition.y,
                        strokeColor: dataset.pointStrokeColor,
                        fillColor: dataset.pointColor,
                        highlightFill: dataset.pointHighlightFill || dataset.pointColor,
                        highlightStroke: dataset.pointHighlightStroke || dataset.pointStrokeColor
                    }));
                }, this);
            }, this);

            this.render();
        },

        eachPoints: function(callback) {
            helpers.each(this.datasets, function(dataset) {
                helpers.each(dataset.points, callback, this);
            }, this);
        },

        getPointsAtEvent: function(evt) {
            var mousePosition = helpers.getRelativePosition(evt),
                fromCenter = helpers.getAngleFromPoint({
                    x: this.scale.xCenter,
                    y: this.scale.yCenter
                }, mousePosition);

            var anglePerIndex = (Math.PI * 2) / this.scale.valuesCount,
                pointIndex = Math.round((fromCenter.angle - Math.PI * 1.5) / anglePerIndex),
                activePointsCollection = [];

            // If we're at the top, make the pointIndex 0 to get the first of the array.
            if (pointIndex >= this.scale.valuesCount || pointIndex < 0)
                pointIndex = 0;

            if (fromCenter.distance <= this.scale.drawingArea)
                helpers.each(this.datasets, function(dataset) {
                    activePointsCollection.push(dataset.points[pointIndex]);
                });

            return activePointsCollection;
        },

        buildScale: function(data) {
            this.scale = new Chart.RadialScale({
                display: this.options.showScale,
                fontStyle: this.options.scaleFontStyle,
                fontSize: this.options.scaleFontSize,
                fontFamily: this.options.fontFamily,
                fontColor: this.options.scaleFontColor,
                showLabels: this.options.scaleShowLabels,
                showLabelBackdrop: this.options.scaleShowLabelBackdrop,
                backdropColor: this.options.scaleBackdropColor,
                backgroundColors: this.options.scaleBackgroundColors,
                backdropPaddingY: this.options.scaleBackdropPaddingY,
                backdropPaddingX: this.options.scaleBackdropPaddingX,
                lineWidth: (this.options.scaleShowLine) ? this.options.scaleLineWidth : 0,
                lineColor: this.options.scaleLineColor,
                angleLineColor: this.options.angleLineColor,
                angleLineWidth: (this.options.angleShowLineOut) ? this.options.angleLineWidth : 0,
                angleLineInterval: (this.options.angleLineInterval) ? this.options.angleLineInterval : 1,
                // Point labels at the edge of each line
                pointLabelFontColor: this.options.pointLabelFontColor,
                pointLabelFontSize: this.options.pointLabelFontSize,
                pointLabelFontStyle: this.options.pointLabelFontStyle,
                height: this.chart.height,
                width: this.chart.width,
                xCenter: this.chart.width / 2,
                yCenter: this.chart.height / 2,
                ctx: this.chart.ctx,
                templateString: this.options.scaleLabel,
                labels: data.labels,
                valuesCount: data.datasets[0].data.length
            });

            this.scale.setScaleSize();
            this.updateScaleRange(data.datasets);
            this.scale.buildYLabels();
        },

        updateScaleRange: function(datasets) {
            var valuesArray = (function() {
                var totalDataArray = [];
                helpers.each(datasets, function(dataset) {
                    if (dataset.data)
                        totalDataArray = totalDataArray.concat(dataset.data);
                    else
                        helpers.each(dataset.points, function(point) {
                            totalDataArray.push(point.value);
                        });
                });
                return totalDataArray;
            })();

            helpers.extend(
                this.scale,
                helpers.calculateScaleRange(
                    valuesArray,
                    helpers.min([this.chart.width, this.chart.height]) / 2,
                    this.options.scaleFontSize,
                    this.options.scaleBeginAtZero,
                    this.options.scaleIntegersOnly
                )
            );
        },

        reflow: function() {
            helpers.extend(this.scale, {
                width: this.chart.width,
                height: this.chart.height,
                size: helpers.min([this.chart.width, this.chart.height]),
                xCenter: this.chart.width / 2,
                yCenter: this.chart.height / 2
            });
            this.updateScaleRange(this.datasets);
            this.scale.setScaleSize();
            this.scale.buildYLabels();
        },

        draw: function(ease) {
            var easeDecimal = ease || 1,
                ctx = this.chart.ctx;
            this.clear();
            this.scale.draw();

            helpers.each(this.datasets, function(dataset) {
                // Transition each point first so that the line and point drawing isn't out of sync
                helpers.each(dataset.points, function(point, index) {
                    if (point.hasValue()) {
                        point.transition(this.scale.getPointPosition(index, this.scale.calculateCenterOffset(point.value)), easeDecimal);
                    }
                }, this);

                // Draw the line between all the points
                ctx.lineWidth = this.options.datasetStrokeWidth;
                ctx.strokeStyle = dataset.strokeColor;
                ctx.beginPath();
                helpers.each(dataset.points, function(point, index) {
                    if (index === 0)
                        ctx.moveTo(point.x, point.y);
                    else
                        ctx.lineTo(point.x, point.y);
                }, this);
                ctx.closePath();
                ctx.stroke();

                ctx.fillStyle = dataset.fillColor;
                ctx.fill();

                // Now draw the points over the line
                // A little inefficient double looping, but better than the line
                // lagging behind the point positions
                helpers.each(dataset.points, function(point) {
                    if (point.hasValue())
                        point.draw();
                });
            }, this);
        }
    });
}(this.Chart, this.ChartHelpers));
