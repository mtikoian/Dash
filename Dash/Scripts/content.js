﻿/*!
 * Wraps content processing functionality.
 */
(function(m, $, Alertify, pjax, Table, Tab, CollapsibleList, DatePicker, Autocomplete, Draggabilly) {
    'use strict';

    var autocompletes = [];
    var draggabillies = [];

    /**
     * Display context help.
     * @this {Node} Node the event is being bound to.
     */
    var contextHelp = function() {
        $.on(this, 'click', Alertify.alert.bind(null, this.getAttribute('data-message').replace(/&quot;/g, '"'), focusOnClose, focusOnClose));
    };

    /**
     * Hide content.
     * @this {Node} Node the event is being bound to.
     */
    var hide = function() {
        var node = $.get(this.getAttribute('data-target'));
        if (node) {
            $.on(this, 'click', $.hide.bind(null, node, false));
        }
    };

    var conditionallyDisable = function() {
        var n = $.get(this.getAttribute('data-target'));
        if (this.value == this.getAttribute('data-match')) {
            n.removeAttribute('disabled');
        } else {
            n.value = '';
            n.setAttribute('disabled', true);
        }
    };

    /**
     * Conditionally disable content.
     * @this {Node} Node the event is being bound to.
     */
    var disable = function() {
        $.onChange(this, conditionallyDisable, true);
    };

    /**
     * Initialize autocomplete.
     * @this {Node} Node the event is being bound to.
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
                    autocompletes.push(new Autocomplete({ selector: self, sourceData: data }));
                } else {
                    // error - @todo what do i do here?
                    autocompletes.push(new Autocomplete({ selector: self, sourceData: null }));
                }
            });
        } else {
            autocompletes.push(new Autocomplete({
                selector: self, source: function(search, response) {
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
     * @this {Node} Node the event is being bound to.
     */
    var autocompleteUnload = function() {
        autocompletes.forEach(function(x) {
            x.destroy();
        });
        autocompletes = [];
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
     * Initialize a table instance
     * @this {Node} Node the event is being bound to.
     */
    var tableLoad = function() {
        var node = $.isNode(this) ? this : this.target;
        if (node) {
            var json = node.getAttribute('data-json');
            if (json) {
                var opts = JSON.parse(json);
                m.mount(node.parentElement, {
                    view: function() {
                        return m(Table, opts);
                    }
                });
            }
        }
    };

    /**
     * Destroy a table instance
     * @this {Node} Node for the table to destroy.
     */
    var tableUnload = function() {
        var node = $.isNode(this) ? this : this.target;
        if (node) {
            $.dispatch(node, $.events.tableDestroy);
            m.mount(node, null);
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
        var node = $.isNode(this) ? this : this.target;
        if (node) {
            $.forEach($.getAll('.column-item', node), function(x) {
                draggabillies.push(new Draggabilly(x).on('dragStart', startColumnDrag).on('dragEnd', stopColumnDrag));
            });
        }
    };

    /**
     * Destroy the column selector.
     */
    var columnSelectorUnload = function() {
        $.forEach(draggabillies, function(x) {
            x.destroy();
        });
        draggabillies = [];
    };

    /**
     * Focus on the first error or input.
     * @param {Node} node - Parent node to search in.
     */
    var autofocus = function(node) {
        if (!node) {
            return null;
        }
        var elems = $.getAll('input[autofocus]', node).filter($.isVisible);
        if (!elems.length) {
            elems = $.getAll('.form-control-error:not([type="hidden"]):not([disabled]):not([readonly]), .mform-control-error:not([type="hidden"]):not([disabled]):not([readonly])', node).filter($.isVisible);
        }
        if (!elems.length) {
            elems = $.getAll('input:not([type="hidden"]):not([disabled]):not([readonly]), select:not([disabled]):not([readonly])', node).filter($.isVisible);
        }
        if (!elems.length) {
            elems = $.getAll('button:not([disabled]), a:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])', node).filter($.isVisible);
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
                target.value = this.getAttribute('data-value');
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
     * Selectors and callback function to create events.
     */
    var _contentActions = [
        {
            selector: '[data-toggle="nav-menu"]',
            onLoad: menuLoad
        },
        {
            selector: '[data-toggle="tab"]',
            onLoad: function() { new Tab(this); }
        },
        {
            selector: '[data-toggle="table"]',
            onLoad: tableLoad,
            onUnload: tableUnload
        },
        {
            selector: '[data-toggle="context-help"]',
            onLoad: contextHelp
        },
        {
            selector: '[data-toggle="collapsible-list"]',
            onLoad: function() { new CollapsibleList(this); }
        },
        {
            selector: '[data-toggle="input-replace"]',
            onLoad: function() { $.on(this, 'click', inputReplace); }
        },
        {
            selector: '[data-toggle="hide"]',
            onLoad: hide
        },
        {
            selector: '[data-toggle="disable"]',
            onLoad: disable
        },
        {
            selector: '[data-toggle="autocomplete"]',
            onLoad: autocompleteLoad,
            onUnload: autocompleteUnload
        },
        {
            selector: '[data-toggle="sortable"]',
            onLoad: sortableLoad,
            onUnload: sortableUnload
        }
    ];

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

        // process all the content actions
        var elems;
        _contentActions.filter(function(x) {
            return isUnload ? x.hasOwnProperty('onUnload') : x.hasOwnProperty('onLoad');
        }).forEach(function(act) {
            elems = $.getAll(act.selector, node);
            if ($.matches(node, act.selector)) {
                elems.push(node);
            }
            elems.forEach(function(x) {
                if (isUnload) {
                    act.onUnload.call(x);
                } else {
                    act.onLoad.call(x);
                }
            });
        });

        if (node.nodeName === 'BODY') {
            var lang = node.getAttribute('data-lang');
            if (lang && lang !== 'en') {
                DatePicker.localize({ locale: lang });
            }
        }

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
     * Set up content after page has loaded.
     */
    var pageLoaded = function() {
        pjax.init();
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
     * Hide loading indicator.
     */
    var done = function() {
        $.hide(_loadingDiv);
    };

    $.content = {
        done: done,
        focusOnClose: focusOnClose,
        load: load,
        loading: loading,
        unload: unload
    };

    /**
     * Run events needed for the inital page load.
     */
    if ($.resxLoaded) {
        pageLoaded();
    } else {
        $.on(document, 'resxLoaded', pageLoaded);
    }

})(this.m, this.$, this.Alertify, this.pjax, this.Table, this.Tab, this.CollapsibleList, this.DatePicker, this.Autocomplete, this.Draggabilly);
