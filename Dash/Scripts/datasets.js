/*!
 * Dataset column form component.
 */
(function(root, factory) {
    root.ColumnForm = factory(root.m, root.$, root.Alertify, root.Form, root.Autocomplete, root.Help);
})(this, function(m, $, Alertify, Form, Autocomplete, Help) {
    'use strict';

    /**
     * Sort columns by title.
     * @param {Object} a - First column to compare.
     * @param {Object} b - Second column to compare.
     * @returns {bool} True if column a should be before column b.
     */
    var columnTitleSort = function(a, b) {
        var aTitle = a.title.toLowerCase(), bTitle = b.title.toLowerCase();
        return aTitle > bTitle ? 1 : (bTitle > aTitle ? -1 : 0);
    };

    /**
     * Build the column form.
     * @param {Object} opts - Options for the form.
     * @returns {Object} Form instance.
     */
    function ColumnForm(opts) {
        Form.call(this, {
            container: $.get('.column-table-wrapper', opts.content),
            columns: {
                title: { type: 'str' },
                columnName: { type: 'str' },
                dataTypeId: { type: 'int' },
                derived: { type: 'str' },
                filterTypeId: { type: 'int' },
                filterQuery: { type: 'str' },
                link: { type: 'str' },
                isParam: { type: 'int' }
            },
            wantsHelp: opts.wantsHelp,
            newRecord: { id: 0, title: null, columnName: '', dataTypeId: '', derived: null, filterTypeId: '', filterQuery: null, link: null, isParam: 0, isExpanded: true }
        }, opts.columns || []);

        this.content = opts.content;
        this.dataTypes = opts.dataTypes;
        this.filterTypes = opts.filterTypes;
        this.sourceFn = opts.sourceFn;
        this.columnFn = opts.columnFn;
        this.selectedSourceFn = opts.selectedSourceFn;
    }

    ColumnForm.prototype = Object.create(Form.prototype);
    ColumnForm.prototype.constructor = ColumnForm;

    /**
     * Expand/collapse the fields for a column.
     * @param {Object} column - The column record to expand/collapse.
     */
    ColumnForm.prototype.toggleExpanded = function(column) {
        column.isExpanded = !column.isExpanded;
    };

    /**
     * Check if the database and primary source are set.
     * @returns {bool} True if a database and a primary source are provided, else false.
     */
    ColumnForm.prototype.checkStatus = function() {
        var database = $.get('.dataset-database', this.content);
        if (!(database && $.hasPositiveValue(database.value))) {
            Alertify.error($.resx('dataset.importErrorDatabaseRequired'));
            return false;
        }
        var primarySource = $.get('.primary-source-autocomplete', this.content);
        if (!(primarySource && primarySource.value)) {
            Alertify.error($.resx('dataset.importErrorPrimarySourceRequired'));
            return false;
        }
        var type = $.get('.dataset-type', this.content);
        if (type && type.value * 1 === 2) {
            Alertify.error($.resx('dataset.importErrorNoProcs'));
            return false;
        }
        return true;
    };

    /**
     * Import columns for the selected tables into the dataset.
     * @param {Event} e - Event that triggered this.
     */
    ColumnForm.prototype.importColumns = function(e) {
        var form = this.content;
        if (!($.hasClass(form, 'dataset-form') && this.checkStatus())) {
            return;
        }

        var self = this;
        var database = $.get('.dataset-database');
        $.ajax({
            method: 'GET',
            url: form.getAttribute('data-import-url'),
            data: { databaseId: database.value, sources: self.selectedSourceFn() }
        }, function(data) {
            if (!data.columns || data.columns.length === 0) {
                Alertify.error($.resx('dataset.importErrorNoColumns'));
                return;
            }

            var newColumns = {};
            data.columns.forEach(function(x) {
                newColumns[x.columnName.toLowerCase()] = x;
            });

            var newRecords = [];
            var existingColumns = [];
            self.records.forEach(function(x) {
                var colName = x.columnName.toLowerCase();
                if (newColumns[colName]) {
                    // record matches - update datatype and add to new list
                    x.dataTypeId = newColumns[colName].dataTypeId;
                    newRecords.push(x);
                    existingColumns.push(colName);
                }
            });

            data.columns.forEach(function(x) {
                // now add any new columns from server
                if (existingColumns.indexOf(x.columnName.toLowerCase()) === -1) {
                    newRecords.push(x);
                }
            });

            newRecords.sort(columnTitleSort);
            self.records = newRecords;

            Alertify.success($.resx('dataset.importSuccess'));
            $.dialogs.focusOnClose.call(e);
        });
    };

    /**
     * Open the confirmation before importing columns.
     * @param {Event} e - Event that triggered this.
     */
    ColumnForm.prototype.import = function(e) {
        if (this.checkStatus()) {
            Alertify.confirm($.resx('dataset.confirmImport'), this.importColumns.bind(this, e), $.dialogs.focusOnClose.bind(e));
        }
    };

    /**
     * Create the node for the column form.
     * @returns {Object} Mithril node containing the form.
     */
    ColumnForm.prototype.view = function() {
        var self = this;
        return m('.table-wrapper', [
            m('.columns.wrapper-row.pb-1',
                m('.col-12',
                    m('.text-right', [
                        m('button.btn.btn-info.mr-2', {
                            type: 'button', role: 'button', onclick: self.addRecord.bind(self)
                        }, $.resx('add')),
                        m('button.btn.btn-error.mr-2', {
                            type: 'button', role: 'button', onclick: self.import.bind(self)
                        }, $.resx('dataset.import')),
                        m(Help, { enabled: this.opts.wantsHelp, message: $.resx('dataset.columnsText') })
                    ])
                )
            ),
            self.records.map(function(record, index) {
                return m('.col-12.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even', key: record._index }, [
                    m('.columns', [
                        m('input', { type: 'hidden', name: 'DatasetColumn[' + index + '].Id', value: record.id }),
                        m('.col-4', self.withHelp($.resx('dataset.columnTitleText'), [
                            m('span.input-group-addon.input-group-custom', m('button.btn.btn-secondary', {
                                type: 'button', role: 'button',
                                onclick: self.toggleExpanded.bind(self, record)
                            }, m('i.dash.text-primary', { class: record.isExpanded ? 'dash-minus' : 'dash-plus' }))),
                            m('input.form-input.required', {
                                type: 'text', name: 'DatasetColumn[' + index + '].Title', class: self.withError(record.title),
                                placeholder: $.resx('dataset.columnTitle'), oninput: self.set.bind(self, index, 'title'), value: record.title
                            }),
                        ])),
                        m('.col-4', self.withHelp($.resx('dataset.columnNameText'), m(Autocomplete, {
                            name: 'DatasetColumn[' + index + '].ColumnName',
                            required: true,
                            placeholder: $.resx('dataset.columnName'),
                            value: record.columnName,
                            list: self.columnFn,
                            onSelect: self.set.bind(self, index, 'columnName')
                        }))),
                        m('.col-3', self.withHelp($.resx('dataset.dataTypeText'),
                            m('select.form-select.required', {
                                name: 'DatasetColumn[' + index + '].DataTypeId', class: self.withError(record.dataTypeId),
                                placeholder: $.resx('dataset.dataType'), oninput: self.set.bind(self, index, 'dataTypeId'), value: record.dataTypeId
                            }, self.withOptions(self.dataTypes, record.dataTypeId, 'id', 'name'))
                        )),
                        m('.col-1', self.buttonView(index, false))
                    ]),
                    m('.columns', { class: record.isExpanded ? '' : ' d-none' },
                        m('.col-10.col-mx-auto', self.withHelp($.resx('dataset.derivedText'),
                            m('input.form-input', {
                                type: 'text', name: 'DatasetColumn[' + index + '].Derived', placeholder: $.resx('dataset.derived'),
                                oninput: self.set.bind(self, index, 'derived'), value: record.derived
                            })
                        ))
                    ),
                    m('.columns', { class: record.isExpanded ? '' : ' d-none' }, [
                        m('.col-3.col-mx-auto', self.withHelp($.resx('dataset.filterTypeText'),
                            m('select.form-select', {
                                name: 'DatasetColumn[' + index + '].FilterTypeId', class: self.withError(record.filterTypeId),
                                placeholder: $.resx('dataset.filterType'), oninput: self.set.bind(self, index, 'filterTypeId'), value: record.filterTypeId
                            }, self.withOptions(self.filterTypes, record.filterTypeId, 'id', 'name'))
                        )),
                        m('.col-7', self.withHelp($.resx('dataset.queryText'),
                            m('input.form-input', {
                                type: 'text', name: 'DatasetColumn[' + index + '].FilterQuery', placeholder: $.resx('dataset.query'),
                                oninput: self.set.bind(self, index, 'filterQuery'), value: record.filterQuery, readOnly: record.filterTypeId !== 3
                            }))
                        )
                    ]),
                    m('.columns', { class: record.isExpanded ? '' : ' d-none' }, [
                        m('.col-8.col-mx-auto', self.withHelp($.resx('dataset.linkText'),
                            m('input.form-input', {
                                type: 'text', name: 'DatasetColumn[' + index + '].Link', placeholder: $.resx('dataset.link'),
                                oninput: self.set.bind(self, index, 'link'), value: record.link
                            })
                        )),
                        m('.col-1', [
                            m('label.form-checkbox', { for: 'DatasetColumn_' + index + '_.IsParam' }, [
                                m('input.custom-control-input', self.withChecked({
                                    type: 'checkbox', name: 'DatasetColumn[' + index + '].IsParam', id: 'DatasetColumn_' + index + '_.IsParam',
                                    oninput: self.set.bind(self, index, 'isParam'), value: 'true'
                                }, record.isParam)),
                                m('i.form-icon'),
                                $.resx('dataset.isParam')
                            ])
                        ]),
                        m('.col-1', m(Help, { enabled: self.opts.wantsHelp, message: $.resx('dataset.isParamText') }))
                    ])
                ]);
            })
        ]);
    };

    return ColumnForm;
});

/*!
 * Dataset join form component.
 */
(function(root, factory) {
    root.JoinForm = factory(root.m, root.$, root.Form, root.Autocomplete, root.Help);
})(this, function(m, $, Form, Autocomplete, Help) {
    'use strict';

    /**
     * Build the join form.
     * @param {Object} opts - Options for the form.
     * @returns {Object} Form instance.
     */
    function JoinForm(opts) {
        Form.call(this, {
            container: $.get('.join-table-wrapper', opts.content),
            columns: {
                joinTypeId: { type: 'int' },
                tableName: { type: 'str' },
                keys: { type: 'str' }
            },
            wantsHelp: opts.wantsHelp,
            newRecord: { id: 0, tableName: '', joinTypeId: 1, keys: null },
            afterDeleteFn: opts.columnUpdateFn
        }, opts.joins || []);

        this.joinTypes = opts.joinTypes;
        this.sourceFn = opts.sourceFn;
        this.columnUpdateFn = opts.columnUpdateFn;
    }

    JoinForm.prototype = Object.create(Form.prototype);
    JoinForm.prototype.constructor = JoinForm;

    /**
     * Set the table name for the join record, and update the column list.
     * @param {number} index - Index of the record to update.
     * @param {string} value - New table name value
     */
    JoinForm.prototype.setJoinTable = function(index, value) {
        this.set(index, 'tableName', value);
        this.columnUpdateFn();
    };

    /**
     * Create the view for the joins tab.
     * @returns {Object} Mithril node containing the join form.
     */
    JoinForm.prototype.view = function() {
        var self = this;

        if (self.isProc) {
            return m('.table-wrapper.p-t-1.col-12', m('.card.bg-info.text-white', m('.card-body', $.resx('dataset.errorProcNoJoins'))));
        }

        return m('.table-wrapper', [
            m('.columns.wrapper-row.pb-1',
                m('.col-12', m('.text-right', [
                    m('button.btn.btn-info.mr-2', { type: 'button', role: 'button', onclick: self.addRecord.bind(self) }, $.resx('add')),
                    m(Help, { enabled: this.opts.wantsHelp, message: $.resx('dataset.joinsText') })
                ]))
            ),
            self.records.map(function(record, index) {
                return m('.columns.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even', key: record._index }, [
                    m('input', { type: 'hidden', name: 'DatasetJoin[' + index + '].Id', value: record.id }),
                    m('input', { type: 'hidden', name: 'DatasetJoin[' + index + '].JoinOrder', value: index }),
                    m('.col-3', self.withHelp($.resx('dataset.joinTableText'), m(Autocomplete, {
                        name: 'DatasetJoin[' + index + '].TableName',
                        required: true,
                        placeholder: $.resx('dataset.joinTableName'),
                        value: record.tableName,
                        list: self.sourceFn,
                        onSelect: self.setJoinTable.bind(self, index),
                        onCancel: self.setJoinTable.bind(self, index),
                    }))),
                    m('.col-2', self.withHelp($.resx('dataset.joinTypeText'),
                        m('select.form-select.required', {
                            name: 'DatasetJoin[' + index + '].JoinTypeId', class: self.withError(record.joinTypeId),
                            placeholder: $.resx('dataset.joinType'), oninput: self.set.bind(self, index, 'joinTypeId'), value: record.joinTypeId
                        }, self.withOptions(self.joinTypes, record.joinTypeId, 'id', 'name'))
                    )),
                    m('.col-5', self.withHelp($.resx('dataset.joinKeysText'),
                        m('input.form-input.required', {
                            type: 'text', name: 'DatasetJoin[' + index + '].Keys', class: self.withError(record.keys),
                            placeholder: $.resx('dataset.joinKeys'), oninput: self.set.bind(self, index, 'keys'), value: record.keys
                        }))
                    ),
                    m('.col-2', self.buttonView.call(self, index, true))
                ]);
            })
        ]);
    };

    return JoinForm;
});

/*!
 * Dataset form component.
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

/*!
 * Dataset form functionality.
 */
(function($, Dataset) {
    'use strict';

    /**
     * Store references to the dataset form mithril modules and value lists.
     * @type {Object}
     */
    var _datasets = {};

    /**
     * Initialize the dataset form when the datasetFormLoad event fires.
     */
    $.on(document, 'datasetFormLoad', function(e) {
        var form = $.isNode(e) ? e : e.target;
        if (!$.hasClass(form, 'dataset-form')) {
            return;
        }

        var dataset = $.get('.dataset-id', form);
        $.ajax({
            method: 'GET',
            url: form.getAttribute('data-url'),
            data: dataset ? { id: dataset.value } : null
        }, function(data) {
            data.content = form;
            _datasets[dataset.value] = new Dataset(data);
        });
    }, true);

    /**
     * Destroy the form when the dialog closes.
     */
    $.on(document, 'datasetFormUnload', function(e) {
        if (!_datasets) {
            return;
        }
        var form = $.isNode(e) ? e : e.target;
        if (!$.hasClass(form, 'dataset-form')) {
            return;
        }

        var datasetId = $.get('.dataset-id', form).value;
        var dataset = _datasets[datasetId];
        if (dataset) {
            dataset.destroy();
        }
        delete _datasets[datasetId];
    }, true);
})(this.$, this.Dataset);
