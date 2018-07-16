/*!
 * Wraps functionality for displaying/moving/resizing widgets and their contents.
 */
(function($, pjax, Widget) {
    'use strict';

    var _columns = 20;
    var _rows = 20;
    var _currentPositions = null;

    /**
     * Initialize the dashboard.
     * @param {Object} widgets - Widgets options.
     */
    var create = function(widgets) {
        var dashboard = $.get('#bodyContent');
        widgets = $.coalesce(widgets, []);

        var opts = makeWidgetOpts(dashboard);
        if (widgets.length) {
            widgets.forEach(function(x) { new Widget($.extend(x, opts)); });
            $.on(window, 'keydown', checkKeyPress);
            $.on(window, 'resize', $.debounce(resizeLayout, 100));
        }
    };

    /**
     * Make the extra options needed to create a widget.
     * @param {Node} dashboard - Dashboard node.
     * @returns {Object} Object with grid and layoutCallback.
     */
    var makeWidgetOpts = function(dashboard) {
        dashboard = $.coalesce(dashboard, $.get('#bodyContent'));
        return {
            grid: { columns: _columns, rows: _rows, columnWidth: dashboard.parentNode.offsetWidth / _columns, rowHeight: dashboard.parentNode.offsetHeight / _rows },
            layoutCallback: $.debounce(updatePosition, 100)
        };
    };

    /**
     * Get the config for the dashboard from the server.
     */
    $.on(document, 'dashboardLoad', function() {
        var dash = $.get('#bodyContent');
        if (!dash) {
            return;
        }
        var json = dash.getAttribute('data-json');
        if (json) {
            dash.removeAttribute('data-json');
            create(JSON.parse(json));
        }
    });

    /**
     * Fetch widget settings from server and add/reload/delete widgets as needed.
     */
    $.on(document, 'dashboardReload', function() {
        var dash = $.get('#bodyContent');
        if (!(dash && dash.hasAttribute('data-url'))) {
            return;
        }

        $.ajax({
            method: 'GET',
            url: dash.getAttribute('data-url')
        }, function(widgetOpts) {
            if (widgetOpts) {
                var widgets = getWidgets();

                widgetOpts.forEach(function(x) {
                    var widgetDate = new Date(x.widgetDateUpdated);
                    var oldWidget = $.findByKey(widgets, 'id', x.id);
                    if (!oldWidget) {
                        // newly added widget
                        new Widget($.extend(x, makeWidgetOpts()));
                    } else {
                        // existing widget - remove this widget from the list
                        widgets.splice(oldWidget._i, 1);

                        if (oldWidget.initDate < widgetDate) {
                            // this widget needs to be reloaded
                            oldWidget.reload(null, x);
                        }
                    }
                });

                if (widgets.length) {
                    // any widgets still left need to be deleted
                    widgets.forEach(function(x) { x.destroy(true); });
                }
            }
        });
    });

    /**
     * Destory dashboard.
     */
    $.on(document, 'dashboardUnload', function() {
        var dash = $.get('#bodyContent.dashboard');
        if (!(dash)) {
            return;
        }

        var widgets = getWidgets();
        if (widgets.length) {
            // any widgets still left need to be deleted
            widgets.forEach(function(x) { x.destroy(true); });
        }
    });

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
    var resizeLayout = function() {
        var grid = makeWidgetOpts().grid;
        getWidgets().forEach(function(x) {
            x.updateLayout();
            x.setupDraggie(null, grid);
        });
    };

    /**
     * Update widget position to avoid collisions after a resize or drag.
     */
    var updatePosition = function() {
        var sorted = getWidgets();
        sorted.sort(rectSort);

        var l = sorted.length, aWidget, bWidget;
        for (var i = 0; i < l; i++) {
            aWidget = sorted[i];
            aWidget.rect.updated = false;

            for (var j = 0; j < l; j++) {
                if (i === j) {
                    continue;
                }

                bWidget = sorted[j];
                if (bWidget.rect.overlaps(aWidget.rect)) {
                    if (bWidget.rect.y > aWidget.rect.y) {
                        // need to move down
                        bWidget.setLocation(bWidget.rect.x, aWidget.rect.y + aWidget.rect.height);
                    } else if (aWidget.rect.x + aWidget.rect.width + bWidget.rect.width > _columns) {
                        // need to move down
                        bWidget.setLocation(bWidget.rect.x, aWidget.rect.y + aWidget.rect.height);
                    } else {
                        // safe to move right
                        bWidget.setLocation(aWidget.rect.x + aWidget.rect.width, bWidget.rect.y);
                    }
                }
            }
        }
        saveDashboard();
    };

    /**
     * Sort widgets from top left to bottom right.
     * @param {Widget} a - First widget to compare.
     * @param {Widget} b - Second widget to compare.
     * @returns {number} Negative number if a comes first, positive number if b, zero if equal.
     */
    var rectSort = function(a, b) {
        if (a.rect.y === b.rect.y && a.rect.x === b.rect.x) {
            return a.rect.updated ? -1 : b.rect.updated ? 1 : 0;
        }
        if (a.rect.x === b.rect.x) {
            return a.rect.y - b.rect.y;
        }
        return a.rect.x - b.rect.x;
    };

    /**
     * Save dashboard settings back to server.
     */
    var saveDashboard = function() {
        var positions = getWidgets().map(function(w) {
            return {
                Id: w.opts.id || 0,
                Width: w.rect.width || 1,
                Height: w.rect.height || 1,
                X: w.rect.x || 0,
                Y: w.rect.y || 0
            };
        });

        if (_currentPositions && $.equals(_currentPositions, positions)) {
            return;
        }
        _currentPositions = positions;

        var dash = $.get('#bodyContent');
        $.ajax({
            method: 'POST',
            url: dash.getAttribute('data-save-url'),
            data: { Widgets: positions },
            block: false
        }, null);
    };

    /**
     * Toggle full screen on escape key.
     * @param {Event} evt - Key press event.
     */
    var checkKeyPress = function(evt) {
        evt = evt || window.event;
        if (evt.keyCode === 27) {
            getWidgets().filter(function(x) { return x.isFullscreen; }).forEach(function(x) { x.toggleFullScreen(); });
        }
    };

    /**
     * Set up content after page has loaded.
     */
    var pageLoaded = function() {
        pjax.connect();
        $.dialogs.processContent($.get('body'));

        $.on('#toggleContextHelpBtn', 'click', function(e) {
            e.preventDefault();
            $.ajax({
                method: 'GET',
                url: this.getAttribute('href')
            }, function(data) {
                $.toggleClass('#toggleContextHelpBtn', 'help-active', data.enabled);
            });
        });

        $.on('#menuBtn', 'click', $.toggleClass.bind(null, 'body', 'toggled', null));

        $.dispatch(document, $.events.dashboardLoad);
    };

    /**
     * Run events needed for the inital page load.
     */
    if ($.resxLoaded) {
        pageLoaded();
    } else {
        $.on(document, 'resxLoaded', pageLoaded);
    }
})(this.$, this.pjax, this.Widget);
