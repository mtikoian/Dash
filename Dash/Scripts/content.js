/*!
 * Wraps content processing functionality.
 */
(function($, doT, Alertify, pjax, doTable, CollapsibleList, Autocomplete, Draggabilly, flatpickr, DashChart, CP, Widget) {
    'use strict';

    var _autocompletes = [];
    var _draggabillies = [];
    var _charts = [];
    var _colorpickers = [];
    var _dashboardEvents = null;
    var _chipFn = doT.template('<span class="chip">{{=x.text}}<button aria-label="close" class="btn-clear btn" role="button"></button><input name="{{=x.fieldName}}[]" type="hidden" value="{{=x.value}}"></span>');
    var _tagListAutocompletes = [];
    var _tagListItemRegex = /.*\(([^)]+)\)/;

    /**
     * Display context help.
     * @this Node
     */
    var contextHelp = function() {
        $.on(this, 'click', Alertify.alert.bind(null, this.getAttribute('data-message').replace(/&quot;/g, '"'), focusOnClose, focusOnClose));
    };

    /**
     * Get the correct node from the object.
     * @param {Node|Event} node - Object to check.
     * @returns {Node} Returns DOM node.
     */
    var getNode = function(node) {
        return node && node.nodeType === 1 && node.nodeName ? node : node.target;
    };

    /**
     * Add the has-changes class for a form.
     * @this {Node}
     */
    var formChanged = function() {
        $.addClass($.closest('form', this), 'has-changes');
    };

    /**
     * Destroy list of items;
     * @param {object[]} list List of objects to destroy.
     */
    var destroyList = function(list) {
        list.forEach(function(x) {
            x.destroy();
        });
        list.splice(0, list.length);
    };

    /**
     * Turn a data-options attribute into an array of values.
     * @param {Node} node Node to get data from.
     * @returns {string[]} Array of values.
     */
    var parseOptions = function(node) {
        var options = [];
        try {
            options = JSON.parse(node.getAttribute('data-options'));
        } catch (ex) {
            // let it go
        }
        return options;
    };

    /**
     * Hide content.
     * @this {Node}
     */
    var hide = function() {
        var node = $.get(this.getAttribute('data-target'));
        if (node) {
            $.on(this, 'click', $.hide.bind(null, node, false));
        }
    };

    /**
     * Conditionally disable inputs.
     * @this {Node}
     */
    var conditionallyDisable = function() {
        var node = $.get(this.getAttribute('data-target'));
        var noMatch = this.value !== this.getAttribute('data-match');
        $.disableIf(node, noMatch);
        if (noMatch) {
            node.value = '';
        }
    };

    /**
     * Conditionally disable content.
     * @this {Node}
     */
    var disable = function() {
        conditionallyDisable.call(this);
        $.on(this, 'change', conditionallyDisable);
    };

    /**
     * Display an alertify message.
     * @this {Node}
     */
    var alert = function() {
        if (this.hasAttribute('data-success')) {
            Alertify.success(this.getAttribute('data-success'));
        } else {
            Alertify.error(this.getAttribute('data-error'));
        }
    };

    /**
     * Build params objects based on a list of nodes.
     * @param {Node} node Node to get the params attribute from.
     * @returns {Object} Object with parameter values.
     */
    var buildParams = function(node) {
        var params = {};
        var data = node.getAttribute('data-params');
        if (data) {
            data.split(',').forEach(function(x) {
                var paramNode = $.get(x);
                if (paramNode) {
                    params[paramNode.id] = paramNode.value;
                }
            });
        }
        return params;
    };

    /**
     * Initialize autocomplete.
     * @this {Node}
     */
    var autocompleteLoad = function() {
        var self = this;

        // request autocomplete options from server during use
        if ((this.getAttribute('data-preload') || '').toLowerCase() !== 'true') {
            _autocompletes.push(new Autocomplete({
                selector: self,
                onSelect: formChanged.bind(self),
                source: function(search, response) {
                    var params = buildParams(self);
                    params.search = search;
                    $.ajax({
                        method: self.getAttribute('data-method') || 'GET',
                        url: self.getAttribute('data-url'),
                        data: params
                    }, function(data) {
                        if (data && data.length) {
                            response(data);
                        }
                    });
                }
            }));
            return;
        }

        // load autocomplete options from a data attribute at initialization
        if (this.hasAttribute('data-options')) {
            var options = parseOptions(this);
            _autocompletes.push(new Autocomplete({
                selector: self,
                onSelect: formChanged.bind(self),
                source: function(search, response) {
                    search = search.toLowerCase();
                    response(options.filter(function(x) {
                        return x.toLowerCase().indexOf(search) > -1;
                    }));
                }
            }));
            this.removeAttribute('data-options');
            return;
        }

        // load autocomplete options from the server at initialization
        $.ajax({
            method: self.getAttribute('data-method') || 'GET',
            url: self.getAttribute('data-url')
        }, function(data) {
            _autocompletes.push(new Autocomplete({
                selector: self,
                onSelect: formChanged.bind(self),
                sourceData: data && data.length ? data : []
            }));
        });
    };

    /**
     * Find a hidden input by value.
     * @param {Node} node Node to search inside of
     * @param {string} value Input value to search for.
     * @returns {Node} Matched node if any.
     */
    var findChip = function(node, value) {
        return $.get('input[value="' + value.replace(/"/g, '\\"') + '"]', node);
    };

    /**
     * Initialize tag list.
     * @this {Node}
     */
    var tagListLoad = function() {
        var self = this;

        var parentNode = $.closest('.form-group', self);
        if (!parentNode) {
            return;
        }
        var chipNode = $.get('.input-group-chips', parentNode);
        if (!chipNode) {
            return;
        }

        // @todo may want to add support for fetching the list from a URL instead of as data-options later
        var options = parseOptions(this);
        _tagListAutocompletes.push(new Autocomplete({
            selector: self,
            cache: false,
            onSelect: function(e, term) {
                e.preventDefault();
                formChanged.call(self);
                self.value = '';

                var matches = term.match(_tagListItemRegex);
                if (!matches) {
                    return;
                }

                if (!findChip(chipNode, matches[1])) {
                    chipNode.appendChild($.createNode(_chipFn({
                        text: matches[0], value: matches[1], fieldName: self.getAttribute('data-chip-input-name')
                    })));
                }
            },
            source: function(search, response) {
                search = search.toLowerCase();
                response(options.filter(function(x) {
                    if (x.toLowerCase().indexOf(search) === -1) {
                        return false;
                    }
                    var matches = x.match(_tagListItemRegex);
                    return !matches ? false : !findChip(chipNode, matches[1]);
                }));
            }
        }));
        this.removeAttribute('data-options');

        $.on(chipNode, 'click', function(event) {
            var target = event.target || event.srcElement;
            if (!$.hasClass(target, 'btn-clear')) {
                return;
            }
            var node = $.closest('.chip', target);
            if (node) {
                formChanged.call(self);
                node.parentNode.removeChild(node);
                self.focus();
            }
        });
    };

    /**
     * Set focus on an element after a dialog closes.
     * @param {Event} e - Event that originally triggered this.
     */
    var focusOnClose = function(e) {
        if (e && e.target) {
            e.target.focus();
        }
    };

    /**
     * Initialize a doTable instance
     * @this Node
     */
    var doTableLoad = function() {
        var node = getNode(this);
        if (node) {
            new doTable(node);
        }
    };

    /**
     * Destroy a doTable instance
     * @this Node
     */
    var doTableUnload = function() {
        var node = getNode(this);
        if (node && node.doTable) {
            node.doTable.destroy();
        }
    };

    /**
     * Initialize a chart instance
     * @this Node
     */
    var chartLoad = function() {
        var node = getNode(this);
        if (node) {
            _charts.push(new DashChart(node, true));
        }
    };

    /**
     * Toggle for exporting a chart.
     */
    var chartExportLoad = function() {
        var node = getNode(this);
        if (node) {
            $.on(node, 'click', function() {
                var chartContainer = $.get('.chart-container');
                if (chartContainer) {
                    $.get('.export-width', chartContainer).value = chartContainer.offsetWidth;
                    $.get('.export-data', chartContainer).value = _charts.length ? _charts[0].chart.toBase64Image() : null;
                    $.get('.export-form', chartContainer).submit();
                }
            }, true);
        }
    };

    /**
     * Update zIndex of column being dragged so it is on top.
     * @param {Event} event - Original mousedown or touchstart event
     */
    var startColumnDrag = function(event) {
        ($.hasClass(event.target, 'column-item') ? event.target : event.target.parentNode).style['z-index'] = 9999;
    };

    /**
     * Update column lists when the user stops dragging a column.
     * @param {Event} event - Original mouseup or touchend event
     * @param {MouseEvent|Touch} pointer - Event object that has .pageX and .pageY
     */
    var stopColumnDrag = function(event, pointer) {
        var target = $.hasClass(event.target, 'column-item') ? event.target : event.target.parentNode;
        var isLeft = (pointer.x || pointer.clientX) + target.offsetWidth / 2 < document.documentElement.clientWidth / 2;
        var newPos = Math.max(Math.round(target.offsetTop / target.offsetHeight), 0);

        $.removeClass(target, 'column-item-right');
        $.removeClass(target, 'column-item-left');
        target.removeAttribute('style');

        var leftItems = $.getAll('.column-item-left');
        leftItems.sort(columnSort);
        var rightItems = $.getAll('.column-item-right');
        rightItems.sort(columnSort);
        newPos = Math.min(newPos, isLeft ? leftItems.length : rightItems.length);

        if (isLeft) {
            $.addClass(target, 'column-item-left');
            leftItems.splice(newPos, 0, target);
        } else {
            $.addClass(target, 'column-item-right');
            rightItems.splice(newPos, 0, target);
        }

        updateColumnList(leftItems, true);
        updateColumnList(rightItems, false);
        formChanged.call(target);
    };

    /**
     * Sort columns by their vertical position.
     * @param {Object} a - Object for first column.
     * @param {Object} b - Object for second column.
     * @returns {bool} True if first column should be after second column, else false;
     */
    var columnSort = function(a, b) {
        return a.offsetTop > b.offsetTop;
    };

    /**
     * Update the position and displayOrder of columns in a list.
     * @param {Node[]} items - Array of column nodes.
     * @param {bool} isLeft - True if the left column list, else false.
     */
    var updateColumnList = function(items, isLeft) {
        items.forEach(function(x, i) {
            updateColumn(x, i, isLeft);
        });
    };

    /**
     * Update the class list and displayOrder for a column item.
     * @param {Node} element - DOM node for the column.
     * @param {number} index - New index of the column in the list.
     * @param {bool} isLeft - True if the column is in the left list, else false.
     */
    var updateColumn = function(element, index, isLeft) {
        element.className = element.className.replace(/column-item-y-([0-9]*)/i, '').trim() + ' column-item-y-' + index;
        var input = $.get('.column-grid-display-order', element);
        if (input) {
            input.value = isLeft ? 0 : index + 1;
        }
    };

    /**
     * Initialize the column selector.
     */
    var columnSelectorLoad = function() {
        var node = getNode(this);
        if (node) {
            $.getAll('.column-item', node).forEach(function(x) {
                _draggabillies.push(new Draggabilly(x).on('dragStart', startColumnDrag).on('dragEnd', stopColumnDrag));
            });
        }
    };

    /**
     * Initialize widget.
     * @this Node
     */
    var widgetLoad = function() {
        var node = getNode(this);
        if (node) {
            new Widget(node);
        }
    };

    /**
     * Destroy widget.
     * @this Node
     */
    var widgetUnload = function() {
        if (this.widget) {
            this.widget.destroy(true);
        }
    };

    /**
     * Initialize datepicker.
     * @this Node
     */
    var datepickerLoad = function() {
        var node = getNode(this);
        if (node) {
            var opts = {
                altInput: true,
                defaultDate: node.value,
                enableTime: true,
                enableSeconds: true,
                time_24hr: true,
                wrap: true
            };
            var lang = $.get('body').getAttribute('data-lang');
            if (lang !== 'en') {
                opts.locale = lang;
            }
            flatpickr(node.parentNode, opts);
        }
    };

    /**
     * Destroy datepickers on this page.
     * @this Node
     */
    var datepickerUnload = function() {
        if (this._flatpickr) {
            this._flatpickr.destroy();
        }
    };

    /**
     * Initialize colorpicker.
     * @this Node
     */
    var colorpickerLoad = function() {
        var node = getNode(this);
        if (node) {
            var picker = new CP(node, false);
            picker.on('change', function(color) {
                this.source.value = '#' + color;
            });

            var update = function() {
                picker.set(this.value).enter();
            };
            picker.source.oncut = update;
            picker.source.onpaste = update;
            picker.source.onkeyup = update;
            picker.source.oninput = update;

            var btn = $.get('button', node.parentNode);
            // @todo how does this work with touch events?
            $.on(node, 'focus', function() { picker.enter(); }, false);
            $.on(btn, 'blur', function(e) {
                if (e.relatedTarget !== node)
                    picker.exit();
            });
            $.on(node, 'blur', function(e) {
                if (e.relatedTarget !== btn)
                    picker.exit();
            });
            $.on(btn, 'click', function() { picker[picker.visible ? 'exit' : 'enter'](); }, false);

            _colorpickers.push(picker);
        }
    };

    /**
     * Initialize content replacer.
     * @this Node
     */
    var contentReplaceLoad = function() {
        $.on(this, 'change', function() {
            loading();
            $.ajax({
                method: this.getAttribute('data-method') || 'GET',
                url: this.getAttribute('data-url'),
                data: buildParams(this)
            }, function(html) {
                var node = $.createNode(html);
                if (node.id) {
                    var existingNode = $.get('#' + node.id);
                    if (existingNode) {
                        processToggles(existingNode, true);
                        processToggles(node);
                        existingNode.parentNode.replaceChild(node, existingNode);
                    }
                }
                done();
            }, function() {
                done();
            });
        });
    };

    /**
     * Focus on the first error or input.
     * @param {Node} node - Parent node to search in.
     */
    var autofocus = function(node) {
        if (!node) {
            return;
        }
        var elems = $.getAll('input[autofocus]', node).filter($.isVisible);
        if (!elems.length) {
            elems = $.getAll('input:not([type="hidden"]):not([disabled]):not([readonly]), select:not([disabled]):not([readonly])', node).filter($.isVisible);
        }
        if (!elems.length) {
            elems = $.getAll('button:not([disabled]):not([data-toggle]), a:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])', node).filter($.isVisible);
        }
        if (elems.length) {
            elems[0].focus();
        }
    };

    /**
     * Replace the value of the data-target node with the data-value from this. Used for providing defaults via a dropdown.
     */
    var inputReplace = function() {
        if (!(this.hasAttribute('data-target') && this.hasAttribute('data-value'))) {
            return;
        }
        var target = $.get(this.getAttribute('data-target'));
        if (!target || $.isNull(target.value)) {
            return;
        }
        if (target.value !== this.getAttribute('data-value')) {
            target.value = this.getAttribute('data-value');
            formChanged.call(target);
        }
    };

    /**
     * Set up click event for nav menu.
     */
    var menuLoad = function() {
        $.on(this, 'click', function() {
            $.toggleClass('body', 'toggled');
            $.trigger(null, 'resize');
        });
    };

    /**
     * Initialize the dashboard.
     * @this Node
     */
    var dashboardLoad = function() {
        if (_dashboardEvents) {
            return;
        }
        _dashboardEvents = {
            keydown: dashboardCheckKeyPress,
            resize: $.debounce(dashboardResizeLayout, 200)
        };
        $.on(window, 'keydown', _dashboardEvents.keydown);
        $.on(window, 'resize', _dashboardEvents.resize);
    };

    /**
     * Destory dashboard.
     */
    var dashboardUnload = function() {
        if (_dashboardEvents) {
            $.off(window, 'keydown', _dashboardEvents.keydown);
            $.off(window, 'resize', _dashboardEvents.resize);
            _dashboardEvents = null;
        }
    };

    /**
     * Get the widget objects for the dashboard.
     * @returns {Widget[]} Array of widgets.
     */
    var getWidgets = function() {
        return $.getAll('.grid-item').map(function(x) { return x.widget; });
    };

    /**
     * Update widget tables on window resize.
     */
    var dashboardResizeLayout = function() {
        getWidgets().forEach(function(x) {
            x.setupDraggie();
        });
    };

    /**
     * Toggle full screen on escape key.
     * @param {Event} evt - Key press event.
     */
    var dashboardCheckKeyPress = function(evt) {
        evt = (evt || window.event);
        if (evt.keyCode === 27) {
            getWidgets().filter(function(x) {
                return x.isFullscreen;
            }).forEach(function(x) {
                x.toggleFullScreen();
            });
        } else if (evt.key === 'F5' && !evt.ctrlKey) {
            getWidgets().forEach(function(x) {
                x.refresh();
            });
            evt.preventDefault();
        }
    };

    /**
     * Selectors and callback function to create events.
     */
    var _toggles = {
        'alertify': { onLoad: alert, onUnload: null },
        'autocomplete': { onLoad: autocompleteLoad, onUnload: destroyList.bind(null, _autocompletes) },
        'chart': { onLoad: chartLoad, onUnload: destroyList.bind(null, _charts) },
        'chart-export': { onLoad: chartExportLoad, onUnload: null },
        'collapsible-list': { onLoad: function() { new CollapsibleList(this); }, onUnload: null },
        'colorpicker': { onLoad: colorpickerLoad, onUnload: destroyList.bind(null, _colorpickers) },
        'column-selector': { onLoad: columnSelectorLoad, onUnload: destroyList.bind(null, _draggabillies) },
        'content-replace': { onLoad: contentReplaceLoad, onUnload: null },
        'context-help': { onLoad: contextHelp, onUnload: null },
        'dashboard': { onLoad: dashboardLoad, onUnload: dashboardUnload },
        'datepicker': { onLoad: datepickerLoad, onUnload: datepickerUnload },
        'disable': { onLoad: disable, onUnload: null },
        'dotable': { onLoad: doTableLoad, onUnload: doTableUnload },
        'hide': { onLoad: hide, onUnload: null },
        'input-replace': { onLoad: function() { $.on(this, 'click', inputReplace); }, onUnload: null },
        'nav-menu': { onLoad: menuLoad, onUnload: null },
        'tag-list': { onLoad: tagListLoad, onUnload: destroyList.bind(null, _tagListAutocompletes) },
        'widget': { onLoad: widgetLoad, onUnload: widgetUnload }
    };

    /**
     * Process data-toggles for a node.
     * @param {Node} node - Node to add events to.
     * @param {bool} isUnload - True if unloading, false if loading
     */
    var processToggles = function(node, isUnload) {
        var elems = $.getAll('[data-toggle]', node);
        if (node.hasAttribute('data-toggle')) {
            elems.push(node);
        }
        elems.forEach(function(x) {
            var toggle = x.getAttribute('data-toggle');
            if (_toggles[toggle]) {
                var func = isUnload ? _toggles[toggle].onUnload : _toggles[toggle].onLoad;
                if (func) {
                    func.call(x);
                }
            }
        });
    };

    /**
     * Process node content adding events.
     * @param {Node} node - Node to add events to.
     * @param {bool} isUnload - True if unloading, false if loading
     */
    var processContent = function(node, isUnload) {
        node = node instanceof Event ? null : node;
        if (!node) {
            return;
        }
        processToggles(node, isUnload);
        autofocus(node);
    };

    /**
     * Process node content handling load events.
     * @param {Node} node - Node to add events to.
     */
    var load = function(node) {
        processContent(node, false);
    };

    /**
     * Process node content handling unload events.
     * @param {Node} node - Node to remove events from.
     */
    var unload = function(node) {
        processContent(node, true);
    };

    /**
     * Closure to set up the loading splash screen and return the node for it.
     */
    var _loadingDiv = (function() {
        var div = $.get('#loader');
        $.on(div, 'keydown', function(e) {
            if ($.hasClass('#loader', 'd-none')) {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            return false;
        });
        return div;
    })();

    /**
     * Show loading indicator.
     */
    var loading = function() {
        $.show(_loadingDiv);
    };

    /**
     * Show refresh message and spinner.
     */
    var forceRefresh = function() {
        $.show('#reloader');
        Alertify.error($.get('body').getAttribute('data-error-refresh') || 'There was an error. Please refresh the page.');
    };

    /**
     * Hide loading indicator.
     */
    var done = function() {
        $.hide(_loadingDiv);
    };

    $.content = {
        done: done,
        forceRefresh: forceRefresh,
        load: load,
        loading: loading,
        unload: unload,
    };

    /**
     * Run events needed for the inital page load.
     */
    // If document is already loaded, run method
    if (document.readyState === 'complete') {
        pjax.init();
    } else {
        // Otherwise, wait until document is loaded
        $.on(document, 'DOMContentLoaded', pjax.init);
    }
})(this.$, this.doT, this.Alertify, this.pjax, this.doTable, this.CollapsibleList, this.Autocomplete, this.Draggabilly, this.flatpickr, this.DashChart, this.CP, this.Widget);
