/*!
 * Wraps functionality for displaying/moving/resizing widgets and their contents.
 */
(function($, Widget) {
    'use strict';

    var _columns = 20;
    var _rows = 20;
    var _currentPositions = null;
    var _windowEvents = null;

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

            var widgets = $.coalesce(JSON.parse(json), []);
            var opts = makeWidgetOpts(dash);
            if (widgets.length) {
                widgets.forEach(function(x) { new Widget($.extend(x, opts)); });
                _windowEvents = {
                    keydown: checkKeyPress,
                    resize: $.debounce(resizeLayout, 100)
                };
                $.on(window, 'keydown', _windowEvents.keydown);
                $.on(window, 'resize', _windowEvents.resize);
            }
        }
    }, true);

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
        $.off(window, 'keydown', _windowEvents.keydown);
        $.off(window, 'resize', _windowEvents.resize);
        _windowEvents = null;
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
})(this.$, this.Widget);
