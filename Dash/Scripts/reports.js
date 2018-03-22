/*!
 * Wraps functionality for displaying reports.
 */
(function($, Draggabilly, ShareForm, ReportDetails) {
    'use strict';

    var _reports = {};
    var _shares = {};

    /**
     * Update zIndex of column being dragged so it is on top.
     * @param {Event} event - Original mousedown or touchstart event
     */
    var startDrag = function(event) {
        var target = $.hasClass(event.target, 'column-item') ? event.target : event.target.parentNode;
        target.style['z-index'] = 9999;
    };

    /**
     * Update column lists when the user stops dragging a column.
     * @param {Event} event - Original mouseup or touchend event
     * @param {MouseEvent|Touch} pointer - Event object that has .pageX and .pageY
     */
    var stopDrag = function(event, pointer) {
        var target = $.hasClass(event.target, 'column-item') ? event.target : event.target.parentNode;
        var isLeft = pointer.x + target.offsetWidth / 2 < document.documentElement.clientWidth / 2;
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

        updateList(leftItems, true);
        updateList(rightItems, false);
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
    var updateList = function(items, isLeft) {
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
            if (isLeft) {
                input.value = 0;
            } else {
                input.value = index + 1;
            }
        }
    };

    /**
     * Initialize the report column selector.
     */
    $.on(document, 'columnSelectorLoad', function() {
        $.getAll('.column-item').forEach(function(x) {
            new Draggabilly(x).on('dragStart', startDrag).on('dragEnd', stopDrag);
        });
    });

    /**
     * Request settings to display a report and call the method to initialize it.
     */
    $.on(document, 'reportLoad', function() {
        var form = $.dialogs.getActiveContent();
        if (!$.hasClass(form, 'report-form')) {
            return;
        }

        $.ajax({
            method: 'GET',
            url: form.getAttribute('data-url')
        }, function(data) {
            var dlg = $.dialogs.getActiveDialog();
            data.content = dlg.getContent();
            _reports[dlg.getId()] = new ReportDetails(data);
        });
    });

    /**
     * Clean up when closing the report dialog.
     */
    $.on(document, 'reportUnload', function() {
        var dlg = $.dialogs.getActiveDialog();
        var report = _reports[dlg.getId()];
        if (report) {
            report.destroy();
        }
        delete _reports[dlg.getId()];
        document.dispatchEvent($.events.dashboardReload);
    });

    /**
     * Load the settings to display the report share form.
     */
    $.on(document, 'reportShareLoad', function() {
        var form = $.dialogs.getActiveContent();
        if (!$.hasClass(form, 'report-share-form')) {
            return;
        }

        var dlg = $.dialogs.getActiveDialog();
        _shares[dlg.getId()] = new ShareForm({ content: dlg.getContent(), formName: 'ReportShare' });
        _shares[dlg.getId()].run();
    });

    /**
     * Clean up when the report share dialog closes.
     */
    $.on(document, 'reportShareUnload', function() {
        var dlg = $.dialogs.getActiveDialog();
        var share = _shares[dlg.getId()];
        if (share) {
            share.destroy();
        }
        delete _shares[dlg.getId()];
    });
})(this.$, this.Draggabilly, this.ShareForm, this.ReportDetails);
