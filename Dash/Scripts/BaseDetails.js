/*!
 * Wraps shared functionality for report and chart detail pages.
 */
(function(root, factory) {
    root.BaseDetails = factory(root.$, root.Alertify, root.Prism);
})(this, function($, Alertify, Prism) {
    'use strict';

    /**
     * Declare base details class.
     * @param {Object} opts - Form settings
     */
    function BaseDetails(opts) {
        opts = opts || {};
        this.content = opts.content;
        this.columnList = opts.columns || [];

        $.on($.get('.view-sql', opts.content), 'click', function() {
            $.dialogs.openDialog($.get('.modal-sql', opts.content).outerHTML);
        });

        if (!opts.allowEdit) {
            this.initDate = new Date();
        }
    }

    BaseDetails.prototype = {
        /**
         * Handle the result of a query for report data.
         * @param {Object} json - Query result.
         * @returns {bool}  True if data is valid.
         */
        processJson: function(json) {
            if (json.updatedDate && this.initDate) {
                var updateDate = new Date(json.updatedDate);
                if (updateDate && updateDate > this.initDate) {
                    // report has been modified - warn the user to refresh
                    Alertify.error($.resx('reportModified'));
                    return false;
                }
            }

            if (json.dataSql) {
                this.setSql($.get('.sql-data-content', this.content), json.dataSql, json.dataError);
            }
            if (json.countSql) {
                this.setSql($.get('.sql-count-content', this.content), json.countSql, json.countError);
            }

            if ($.isNull(json.rows)) {
                Alertify.error($.resx('errorConnectingToDataSource'));
                return false;
            }
            return true;
        },

        /**
         * Show sql and error message in the correct place.
         * @param {Node} node - DOM node to update.
         * @param {string} sql - SQL statement to display.
         * @param {string} error - Error to display if any.
         */
        setSql: function(node, sql, error) {
            if (node) {
                var elem = $.get('.sql-text', node);
                if (elem) {
                    elem.textContent = sql;
                    Prism.highlightElement(elem);
                }
                elem = $.get('.sql-error', node);
                if (elem) {
                    elem.textContent = error || '';
                }
            }
        },

        /**
         * Get the list of columns for the autocomplete.
         * @returns {string[]} Returns array of columns.
         */
        getColumnList: function() {
            return this.columnList;
        }
    };

    return BaseDetails;
});
