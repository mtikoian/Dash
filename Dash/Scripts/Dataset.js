/*!
 * Wraps dataset form functionality.
 */
(function(root, factory) {
    root.Dataset = factory(root.m, root.$, root.Alertify, root.Form, root.Autocomplete, root.Help, root.JoinForm, root.ColumnForm);
})(this, function(m, $, Alertify, Form, Autocomplete, Help, JoinForm, ColumnForm) {
    'use strict';

    /**
     * Declare Dataset class.
     * @param {Object} opts - Dataset settings
     */
    function Dataset(opts) {
        opts = opts || {};
        if (opts.columns) {
            opts.columns.forEach(function(x) { x.isExpanded = false; });
        }

        this.sourceList = null;
        this.columnList = null;
        this.formChanged = false;
        this.selectedSourceList = [];
        this.content = opts.content;

        this.checkType(false);
        this.loadSourceList();
        this.loadColumnList();

        this.joinForm = new JoinForm({
            content: opts.content,
            wantsHelp: opts.wantsHelp,
            joins: opts.joins,
            joinTypes: opts.joinTypes,
            sourceFn: this.getSourceList.bind(this),
            columnUpdateFn: this.loadColumnList.bind(this)
        });
        this.joinForm.run();

        this.columnForm = new ColumnForm({
            content: opts.content,
            wantsHelp: opts.wantsHelp,
            columns: opts.columns,
            dataTypes: opts.dataTypes,
            filterTypes: opts.filterTypes,
            sourceFn: this.getSourceList.bind(this),
            columnFn: this.getColumnList.bind(this),
            selectedSourceFn: this.getSelectedSourceList.bind(this)
        });
        this.columnForm.run();

        $.onChange($.get('.dataset-database', this.content), this.loadSourceList.bind(this), false);
        $.onChange($.get('.dataset-type', this.content), this.checkType.bind(this), false);

        var self = this;
        var primary = $.get('.dataset-primary-source', this.content);
        if (primary) {
            m.mount(primary, {
                view: function() {
                    return m(Help, { message: $.resx('dataset.primarySourceHelp'), enabled: opts.wantsHelp }, m(Autocomplete, {
                        name: 'PrimarySource',
                        class: 'primary-source-autocomplete',
                        value: primary.getAttribute('data-value'),
                        required: true,
                        list: self.getSourceList.bind(self),
                        onSelect: self.updateColumnList.bind(self),
                        onCancel: self.updateColumnList.bind(self)
                    }));
                }
            });
        }
    }

    Dataset.prototype = {
        /**
         * Build a list of all tables used by this dataset.
         * @returns {bool} Returns true if list has changed, else false.
         */
        updateSelectedSources: function() {
            var list = [];
            var primarySource = $.get('.primary-source-autocomplete', this.content);
            if (primarySource && primarySource.value) {
                list.push(primarySource.value);
            }
            if (this.joinForm) {
                this.joinForm.records.map(function(x) { list.push(x.tableName); });
            }
            list = list.filter(function(x) {
                return !$.isNull(x) && x.length > 0;
            });
            if (!$.equals(list, this.selectedSourceList)) {
                this.selectedSourceList = list;
                return true;
            }
            return false;
        },

        /**
         * Check if the dataset is using a proc instead of tables.
         * @param {bool} updateList - If true update the table/proc list.
         */
        checkType: function(updateList) {
            var type = $.get('.dataset-type', this.content);
            if (!type) {
                return;
            }
            if ($.coalesce(updateList, true)) {
                this.loadSourceList();
            }
            if (this.joinForm) {
                this.joinForm.isProc = type.value * 1 === 2;
                this.joinForm.run();
            }
        },

        /**
         * Fetch a list of available columns from the server. Autocompletes will update automatically.
         */
        loadColumnList: function() {
            var url = this.content.getAttribute('data-column-url');
            var database = $.get('.dataset-database', this.content);
            if (!(url && database && $.hasPositiveValue(database.value))) {
                this.columnList = [];
                return;
            }

            if (this.updateSelectedSources()) {
                // only run if sources are different from last request to avoid unnecessary traffic
                var self = this;
                $.ajax({
                    method: 'POST',
                    url: url,
                    data: { DatabaseId: database.value * 1, Tables: self.selectedSourceList }
                }, function(columns) {
                    self.columnList = columns && columns.length ? columns : [];
                });
            }
        },

        /**
         * Fetch a list of tables from the server. Autocompletes will update automatically.
         */
        loadSourceList: function() {
            var url = this.content.getAttribute('data-table-url');
            var database = $.get('.dataset-database', this.content);
            var type = $.get('.dataset-type', this.content);
            if (!(url && database && type)) {
                return;
            }
            if (!$.hasPositiveValue(database.value) || !$.hasPositiveValue(type.value)) {
                this.sourceList = [];
                return;
            }

            var self = this;
            $.ajax({
                method: 'GET',
                url: url,
                data: { databaseId: database.value, typeId: type.value }
            }, function(tables) {
                self.sourceList = tables && tables.length ? tables : [];
                self.loadColumnList();
            });
        },

        /**
         * Get the list of table for the autocomplete.
         * @returns {string[]} Returns array of tables.
         */
        getSourceList: function() {
            return this.sourceList;
        },

        /**
         * Get the list of selected tables.
         * @returns {string[]} Returns array of tables.
         */
        getSelectedSourceList: function() {
            return this.selectedSourceList;
        },

        /**
         * Get the list of columns for the autocomplete.
         * @returns {string[]} Returns array of columns.
         */
        getColumnList: function() {
            return this.columnList;
        },

        /**
         * Refresh the column list.
         */
        updateColumnList: function() {
            // need timeout delay so the field value is updated before we load the list
            setTimeout(this.loadColumnList.bind(this), 10);
        },

        /**
         * Clean up our mess.
         */
        destroy: function() {
            $.destroy(this.columnForm);
            $.destroy(this.joinForm);

            var primary = $.get('.dataset-primary-source', this.content);
            if (primary) {
                m.mount(primary, null);
            }
        }
    };

    return Dataset;
});
