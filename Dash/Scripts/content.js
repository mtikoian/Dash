/*!
 * Wraps content processing functionality.
 */
(function($, Alertify, pjax, doTable, CollapsibleList, Autocomplete, Draggabilly, flatpickr, DashChart, ColorPicker, Widget) {
    'use strict';

    var _autocompletes = [];
    var _draggabillies = [];
    var _charts = [];
    var _colorpickers = [];
    var _dashboardEvents = null;

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
        return $.isNode(node) ? node : node.target;
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
        $.onChange(this, conditionallyDisable, true);
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
     * Initialize autocomplete.
     * @this {Node}
     */
    var autocompleteLoad = function() {
        // @todo maybe add a way to include source list in original html response instead of requiring another request

        var preload = ['true', 'True'].indexOf(this.getAttribute('data-preload')) > -1;
        var self = this;
        if (preload) {
            $.ajax({
                method: self.getAttribute('data-method') || 'GET',
                url: self.getAttribute('data-url')
            }, function(data) {
                if (data && data.length) {
                    _autocompletes.push(new Autocomplete({ selector: self, sourceData: data }));
                } else {
                    // error - @todo what do i do here?
                    _autocompletes.push(new Autocomplete({ selector: self, sourceData: null }));
                }
            });
        } else {
            _autocompletes.push(new Autocomplete({
                selector: self,
                source: function(search, response) {
                    var params = { search: search };
                    if (self.hasAttribute('data-params')) {
                        self.getAttribute('data-params').split(',').forEach(function(x) {
                            var node = $.get(x);
                            if (node) {
                                params[node.id] = node.value;
                            }
                        });
                    }

                    $.ajax({
                        method: self.getAttribute('data-method') || 'GET',
                        url: self.getAttribute('data-url'),
                        data: params
                    }, function(data) {
                        if (data && data.length) {
                            response(data);
                        } else {
                            // error - @todo what do i do here?
                        }
                    });
                }
            }));
        }
    };

    /**
     * Destroy autocompletes on this page.
     * @this Node
     */
    var autocompleteUnload = function() {
        _autocompletes.forEach(function(x) {
            x.destroy();
        });
        _autocompletes = [];
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
     * Destroy a chart instance
     * @this Node
     */
    var chartUnload = function() {
        _charts.forEach(function(x) {
            x.destroy();
        });
        _charts = [];
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
                    // @todo this is sloppy - probably need a data- attribute of some sort to help narrow querySelector down
                    $.get('.export-width').value = chartContainer.offsetWidth;
                    $.get('.export-data').value = _charts.length ? _charts[0].chart.toBase64Image() : null;
                    $.get('.export-form').submit();
                }
            }, true);
        }
    };

    /**
     * Update zIndex of column being dragged so it is on top.
     * @param {Event} event - Original mousedown or touchstart event
     */
    var startColumnDrag = function(event) {
        var target = $.hasClass(event.target, 'column-item') ? event.target : event.target.parentNode;
        target.style['z-index'] = 9999;
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
        $.addClass($.closest('form', target), 'has-changes');
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
        $.forEach(items, function(x, i) {
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
            if (isLeft) {
                input.value = 0;
            } else {
                input.value = index + 1;
            }
        }
    };

    /**
     * Initialize the column selector.
     */
    var columnSelectorLoad = function() {
        var node = getNode(this);
        if (node) {
            $.forEach($.getAll('.column-item', node), function(x) {
                _draggabillies.push(new Draggabilly(x).on('dragStart', startColumnDrag).on('dragEnd', stopColumnDrag));
            });
        }
    };

    /**
     * Destroy the column selector.
     */
    var columnSelectorUnload = function() {
        $.forEach(_draggabillies, function(x) {
            x.destroy();
        });
        _draggabillies = [];
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
                // @todo update this to be able to handle languages more gracefully
                opts.locale = 'Spanish';
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
            // @todo probably want to replace this with a better looking picker later, but it'll do for now
            var newNode = $.createNode('<div id="colorpickerContainer" class="cp-fancy"></div>');
            node.parentNode.insertBefore(newNode, node.nextSibling);
            var picker = new ColorPicker(newNode, function(hex) {
                node.value = hex;
            });
            picker.setHex(node.value);

            $.on(node, 'change', function() {
                picker.setHex(this.value);
            });

            _colorpickers.push(picker);
        }
    };

    /**
     * Destroy colorpickers on this page.
     * @this Node
     */
    var colorpickerUnload = function() {
        $.forEach(_colorpickers, function(x) {
            x.destroy();
        });
        _colorpickers = [];
    };

    /**
     * Initialize content replacer.
     * @this Node
     */
    var contentReplaceLoad = function() {
        $.onChange(this, function() {
            var params = {};
            if (this.hasAttribute('data-params')) {
                this.getAttribute('data-params').split(',').forEach(function(x) {
                    var node = $.get(x);
                    if (node) {
                        params[node.id] = node.value;
                    }
                });
            }

            loading();
            $.ajax({
                method: this.getAttribute('data-method') || 'GET',
                url: this.getAttribute('data-url'),
                data: params
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
        }, false);
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
        if (!elems.length) {
            var dlg = $.closest('.rd-dialog', node);
            if (dlg) {
                elems = $.getAll('.rd-close', dlg).filter($.isVisible);
            }
        }
        if (elems.length) {
            elems[0].focus();
        }
    };

    /**
     * Replace the value of the data-target node with the data-value from this. Used for providing defaults via a dropdown.
     */
    var inputReplace = function() {
        if (this.hasAttribute('data-target') && this.hasAttribute('data-value')) {
            var target = $.get(this.getAttribute('data-target'));
            if (target && !$.isNull(target.value)) {
                if (target.value !== this.getAttribute('data-value')) {
                    target.value = this.getAttribute('data-value');
                    // update form has-changes for pjax confirmation handling
                    var form = $.closest('FORM', target);
                    if (form) {
                        $.addClass(form, 'has-changes');
                    }
                }
            }
        }
    };

    /**
     * Set up click event for nav menu.
     */
    var menuLoad = function() {
        $.on(this, 'click', function() {
            $.toggleClass('body', 'toggled', null);
            $.dispatch(window, new Event('resize'));
        });
    };

    /**
     * Initialize the dashboard.
     * @this Node
     */
    var dashboardLoad = function() {
        var node = getNode(this);
        if (node) {
            _dashboardEvents = {
                keydown: dashboardCheckKeyPress,
                resize: $.debounce(dashboardResizeLayout, 200)
            };
            $.on(window, 'keydown', _dashboardEvents.keydown);
            $.on(window, 'resize', _dashboardEvents.resize);
        }
    };

    /**
     * Destory dashboard.
     */
    var dashboardUnload = function() {
        if (_dashboardEvents) {
            $.off(window, 'keydown', _dashboardEvents.keydown);
            $.off(window, 'resize', _dashboardEvents.resize);
        }
        _dashboardEvents = null;
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
            x.updateLayout();
            x.setupDraggie();
        });
    };

    /**
     * Toggle full screen on escape key.
     * @param {Event} evt - Key press event.
     */
    var dashboardCheckKeyPress = function(evt) {
        evt = evt || window.event;
        if (evt.keyCode === 27) {
            getWidgets().filter(function(x) { return x.isFullscreen; }).forEach(function(x) { x.toggleFullScreen(); });
        }
    };

    /**
     * Selectors and callback function to create events.
     */
    var _toggles = {
        'nav-menu': {
            onLoad: menuLoad,
            onUnload: null
        },
        'dotable': {
            onLoad: doTableLoad,
            onUnload: doTableUnload
        },
        'context-help': {
            onLoad: contextHelp,
            onUnload: null
        },
        'collapsible-list': {
            onLoad: function() { new CollapsibleList(this); },
            onUnload: null
        },
        'input-replace': {
            onLoad: function() { $.on(this, 'click', inputReplace); },
            onUnload: null
        },
        'hide': {
            onLoad: hide,
            onUnload: null
        },
        'disable': {
            onLoad: disable,
            onUnload: null
        },
        'alertify': {
            onLoad: alert,
            onUnload: null
        },
        'autocomplete': {
            onLoad: autocompleteLoad,
            onUnload: autocompleteUnload
        },
        'column-selector': {
            onLoad: columnSelectorLoad,
            onUnload: columnSelectorUnload
        },
        'content-replace': {
            onLoad: contentReplaceLoad
        },
        'datepicker': {
            onLoad: datepickerLoad,
            onUnload: datepickerUnload
        },
        'chart': {
            onLoad: chartLoad,
            onUnload: chartUnload
        },
        'chart-export': {
            onLoad: chartExportLoad,
            onUnload: null
        },
        'colorpicker': {
            onLoad: colorpickerLoad,
            onUnload: colorpickerUnload
        },
        'widget': {
            onLoad: widgetLoad,
            onUnload: widgetUnload
        },
        'dashboard': {
            onLoad: dashboardLoad,
            onUnload: dashboardUnload
        }
    };

    /**
     * Process data-toggles for a node.
     * @param {Node} node - Node to add events to.
     * @param {bool} isUnload - True if unloading, false if loading
     */
    var processToggles = function(node, isUnload) {
        // process all the toggles
        var elems = $.getAll('[data-toggle]', node);
        if ($.matches(node, '[data-toggle]')) {
            elems.push(node);
        }
        $.forEach(elems, function(x) {
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
        node = $.isEvent(node) ? null : node;
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
            // @todo maybe add a way to cancel a pending request using escape?

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
        focusOnClose: focusOnClose,
        forceRefresh: forceRefresh,
        load: load,
        loading: loading,
        unload: unload,
    };

    /**
     * Run events needed for the inital page load.
     */
    $.ready(pjax.init);

})(this.$, this.Alertify, this.pjax, this.doTable, this.CollapsibleList, this.Autocomplete, this.Draggabilly, this.flatpickr, this.DashChart, this.ColorPicker, this.Widget);
