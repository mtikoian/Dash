/*!
 * Wraps dataset column form functionality.
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
                    m('.columns', { class: record.isExpanded ? '' : ' hidden' },
                        m('.col-10.col-mx-auto', self.withHelp($.resx('dataset.derivedText'),
                            m('input.form-input', {
                                type: 'text', name: 'DatasetColumn[' + index + '].Derived', placeholder: $.resx('dataset.derived'),
                                oninput: self.set.bind(self, index, 'derived'), value: record.derived
                            })
                        ))
                    ),
                    m('.columns', { class: record.isExpanded ? '' : ' hidden' }, [
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
                    m('.columns', { class: record.isExpanded ? '' : ' hidden' }, [
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
