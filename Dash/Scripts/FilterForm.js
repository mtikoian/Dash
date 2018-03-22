/*!
 * Wraps report filter form functionality.
 */
(function(root, factory) {
    root.FilterForm = factory(root.m, root.$, root.Form, root.Help, root.Alertify, root.DatePicker);
})(this, function(m, $, Form, Help, Alertify, DatePicker) {
    'use strict';

    /**
     * Build the filter form.
     * @param {Object} opts - Options for the form.
     * @returns {Object} Form instance.
     */
    function FilterForm(opts) {
        Form.call(this, {
            container: $.get('.filter-table-wrapper', opts.content),
            columns: {
                id: { type: 'int' },
                columnId: { type: 'int' },
                operatorId: { type: 'int' },
                criteria: { type: 'str' },
                criteria2: { type: 'str' }
            },
            appendRecord: true,
            allowEdit: opts.allowEdit,
            wantsHelp: opts.wantsHelp,
            newRecord: { id: null, columnId: '', operatorId: '', criteria: null, criteria2: null },
            dateFormat: opts.dateFormat
        }, opts.filters || []);

        this.isProc = opts.isProc;
        this.saveFiltersUrl = opts.saveFiltersUrl;
        this.filterOperators = opts.filterOperators || [];
        this.filterOperatorIds = opts.filterOperatorIds || {};
        this.filterTypes = opts.filterTypes || {};
        this.dateOperators = opts.dateOperators || [];
        this.lookups = opts.lookups || [];
        this.dataTable = opts.dataTable;
        this.columnFn = opts.columnFn;
    }

    FilterForm.prototype = Object.create(Form.prototype);
    FilterForm.prototype.constructor = FilterForm;

    /**
     * Build the options select list for the boolean filter.
     */
    FilterForm.prototype.boolOptions = [
        m('option', { value: '' }, $.resx('select')),
        m('option', { value: 1 }, $.resx('report.true')),
        m('option', { value: 0 }, $.resx('report.false'))
    ];

    /**
     * Set the columnId for a filter and unset the criteria.
     * @param {number} index - Index of the filter to update.
     * @param {Event} e - Event that triggered showing the picker.
     */
    FilterForm.prototype.setColumnId = function(index, e) {
        var val = this.targetVal(e) * 1;
        if (this.records[index].columnId !== val) {
            this.records[index].columnId = val;
            this.records[index].operatorId = '';
            this.records[index].criteria = null;
            this.records[index].criteria2 = null;
            this.run();
        }
    };

    /**
     * Set the operatorId for a filter and unset the criteria.
     * @param {number} index - Index of the filter to update.
     * @param {Event} e - Event that triggered showing the picker.
     */
    FilterForm.prototype.setOperator = function(index, e) {
        this.set(index, 'operatorId', e);
    };

    /**
     * Set the criteria for a filter.
     * @param {number} index - Index of the filter to update.
     * @param {Event} e - Event that triggered the update.
     */
    FilterForm.prototype.setCriteria = function(index, e) {
        var node = e.target;
        if (node && node.nodeName === 'SELECT' && node.hasAttribute('multiple') && node.options) {
            this.records[index].criteriaJson = Array.apply(null, node.options).filter(function(x) { return x.selected; }).map(function(x) { return x.value || x.text; });
        }
        this.set(index, 'criteria', e);
    };

    /**
     * Set the criteria for a date filter.
     * @param {number} index - Index of the filter to update.
     * @param {string} field - Name of field to set.
     * @param {Date} val - Date value to save.
     */
    FilterForm.prototype.setDate = function(index, field, val) {
        this.set(index, field, $.fecha.format(val, 'YYYY-MM-DD HH:mm'));
    };

    /**
     * Save filters to the server then reload the table data.
     */
    FilterForm.prototype.saveFilters = function() {
        if ($.getAll('.mform-control-error', this.container).length > 0) {
            Alertify.error($.resx('fixIt'));
            return;
        }

        var self = this;
        $.ajax({
            method: 'PUT',
            url: self.saveFiltersUrl,
            data: self.records
        }, function(data) {
            if (data) {
                self.changed = false;
                if ($.isArray(data.filters)) {
                    data.filters.forEach(function(x, i) { self.records[i].id = x.id; });
                }
            }
            if (self.dataTable) {
                self.dataTable.loadData();
            }
        });
    };

    /**
     * Build the criteria input(s) for a filter.
     * @param {number} index - Index of the filter to build.
     * @returns {Object} Mithril node containing the criteria input(s).
     */
    FilterForm.prototype.criteriaView = function(index) {
        var filter = this.records[index];
        var column = $.hasPositiveValue(filter.columnId) ? $.findByKey(this.columnFn(), 'id', filter.columnId) : null;
        if (column === null || !$.hasPositiveValue(filter.operatorId)) {
            return m('input.form-control', { disabled: true, placeholder: $.resx('report.filterCriteria') });
        }

        var criteria = null;
        var attrs = {
            name: 'ReportFilter[' + index + '].Criteria',
            id: 'ReportFilter_' + index + '_Criteria',
            class: 'form-control required' + this.withError(filter.criteria),
            placeholder: $.resx('report.filterCriteria'),
            oninput: this.setCriteria.bind(this, index)
        };
        if (!this.opts.allowEdit) {
            attrs.disabled = true;
        }

        if (column.filterTypeId !== this.filterTypes.select) {
            attrs.value = filter.criteria;
        }
        var isRange = filter.operatorId === this.filterOperatorIds.range;

        switch (column.filterTypeId) {
            case this.filterTypes.boolean:
                attrs.class += ' custom-select';
                criteria = m('select', attrs, this.boolOptions);
                break;
            case this.filterTypes.date:
                if (filter.operatorId === this.filterOperatorIds.dateInterval) {
                    attrs.class += ' custom-select';
                    criteria = m('select', attrs, this.withOptions(this.dateOperators, filter.criteria * 1, 'id', 'name'));
                } else {
                    criteria = m(DatePicker, {
                        name: attrs.name, required: true, date: filter.criteria, disabled: !this.opts.allowEdit,
                        onchange: this.setDate.bind(this, index, 'criteria'), format: this.dateFormat,
                        class: isRange ? 'col' : null
                    });
                }
                break;
            case this.filterTypes.select:
                if (filter.operatorId !== this.filterOperatorIds.equal) {
                    attrs.multiple = true;
                } else {
                    attrs.class += ' custom-select';
                }

                try {
                    if (filter.criteria.substring(0, 1) === '[') {
                        filter.criteriaJson = JSON.parse(filter.criteria);
                    }
                } catch (e) {
                    // do nothing
                }

                if (!filter.criteriaJson) {
                    attrs.value = filter.criteria;
                }

                var lookup = $.isArray(this.lookups[filter.columnId]) ? this.lookups[filter.columnId] : [];
                criteria = m('select', attrs, this.withOptions(lookup, filter.criteriaJson, 'value', 'text'));
                break;
            default:
                attrs.type = column.filterTypeId === this.filterTypes.numeric ? 'number' : 'text';
                criteria = m('input', attrs);
        }

        if (!isRange) {
            return criteria;
        }

        var isDatePicker = column.filterTypeId === this.filterTypes.date && filter.operatorId !== this.filterOperatorIds.dateInterval;
        var attrs2 = {
            name: 'ReportFilter[' + index + '].Criteria2',
            id: 'ReportFilter_' + index + '_Criteria2',
            class: 'form-control required' + this.withError(filter.criteria2, null, column.filterTypeId === this.filterTypes.date),
            placeholder: $.resx('report.filterCriteria2'),
            onchange: isDatePicker ? this.setDate.bind(this, index, 'criteria2') : this.set.bind(this, index, 'criteria2'),
            value: filter.criteria2,
            type: column.filterTypeId === this.filterTypes.numeric ? 'number' : 'text'
        };
        if (!this.opts.allowEdit) {
            attrs2.disabled = true;
        }
        var criteriaArr = [criteria];
        if (isDatePicker) {
            criteriaArr.push(m(DatePicker, {
                name: attrs2.name, required: true, date: filter.criteria2, disabled: !this.opts.allowEdit,
                onchange: this.setDate.bind(this, index, 'criteria2'), format: this.dateFormat, class: 'col'
            }));
        } else {
            criteriaArr.push(m('input', attrs2));
        }
        return criteriaArr;
    };

    /**
     * Build the operator dropdown for a filter.
     * @param {number} index - Index of the filter to build.
     * @param {Object} column - Column that the filter is for.
     * @returns {Object} Mithril node containing the operator select.
     */
    FilterForm.prototype.operatorView = function(index, column) {
        return m('select.form-control.required.custom-select', this.withDisabled({
            name: 'ReportFilter[' + index + '].OperatorId', id: 'ReportFilter_' + index + '_OperatorId',
            class: column ? this.withError(this.records[index].operatorId) : null,
            placeholder: $.resx('report.filterOperator'), onchange: this.setOperator.bind(this, index), value: this.records[index].operatorId
        }, !column || !this.opts.allowEdit),
            this.withOptions(column && $.hasPositiveValue(column.filterTypeId) ? this.filterOperators[column.filterTypeId] : [{ id: 0, name: $.resx('report.filterOperator') }],
                this.records[index].operatorId, 'id', 'name')
        );
    };

    /**
     * Build the filters.
     * @returns {Object[]} Array of Mithril nodes containing the filters.
     */
    FilterForm.prototype.filterView = function() {
        if (!this.columnFn()) {
            return null;
        }

        var self = this;
        return this.records.map(function(x, index) {
            var column = $.hasPositiveValue(x.columnId) ? $.findByKey(self.columnFn(), 'id', x.columnId) : null;

            var attrs = {
                name: 'ReportFilter[' + index + '].ColumnId', class: 'form-control custom-select required' + self.withError(x.columnId, true),
                placeholder: $.resx('report.filterColumn'), onchange: self.setColumnId.bind(self, index), value: x.columnId
            };
            if (!self.opts.allowEdit) {
                attrs.disabled = true;
            }

            var filterColumns = self.isProc ? self.columnFn().filter(function(x) { return x.isParam; }) : self.columnFn();

            return m('.row.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even' }, [
                m('input', { type: 'hidden', name: 'ReportFilter[' + index + '].Id', value: x.id }),
                m('input', { type: 'hidden', name: 'ReportFilter[' + index + '].DisplayOrder', value: index }),
                m('.col-4',
                    self.withHelp($.resx('report.filterColumnText'), m('select', attrs, self.withOptions(filterColumns, x.columnId, 'id', 'title')))
                ),
                m('.col-2',
                    self.withHelp($.resx('report.filterOperatorText'), self.operatorView.call(self, index, column))
                ),
                m('.col-5',
                    self.withHelp($.resx('report.filterCriteriaText'), self.criteriaView.call(self, index))
                ),
                m('.col-1', self.opts.allowEdit ? self.buttonView.call(self, index, false) : null)
            ]);
        });
    };

    /**
    * Create the view for the share form.
    * @returns {Object} Mithril node containing the share form.
    */
    FilterForm.prototype.view = function() {
        return [
            this.filterView(),
            this.opts.allowEdit ? m('.row.pt-1', [
                m('.col-6', m('button.btn.btn-primary', {
                    type: 'button', role: 'button', onclick: this.saveFilters.bind(this)
                }, $.resx('save'))),
                m('.col-6', [
                    m('.float-right', [
                        m('button.btn.btn-info.mr-2', {
                            type: 'button', role: 'button', onclick: this.addRecord.bind(this),
                        }, $.resx('add')),
                        m('button.btn.btn-warning.mr-2', {
                            type: 'button', role: 'button', disabled: !this.hasRecords(),
                            onclick: this.hasRecords() ? this.deleteAllRecords.bind(this) : $.noop
                        }, $.resx('deleteAll')),
                        m(Help, { enabled: this.opts.wantsHelp, message: $.resx('report.filterText') })
                    ])
                ])
            ]) : null
        ];
    };

    return FilterForm;
});
