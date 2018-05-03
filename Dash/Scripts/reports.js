/*!
 * Report filter form component.
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
            newRecord: { id: 0, columnId: 0, operatorId: 0, criteria: null, criteria2: null },
            dateFormat: opts.dateFormat
        }, opts.filters || []);

        this.reportId = opts.reportId;
        this.isProc = opts.isProc;
        this.saveFiltersUrl = opts.saveFiltersUrl;
        this.filterOperators = opts.filterOperators || [];
        this.filterOperatorIds = opts.filterOperatorIds || {};
        this.filterTypes = opts.filterTypes || {};
        this.dateOperators = opts.dateOperators || [];
        this.lookups = opts.lookups || [];
        this.refreshFn = opts.refreshFn;
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
            this.records[index].operatorId = 0;
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
            data: {
                Id: self.reportId,
                Filters: $.toPascalKeys(self.records)
            }
        }, function(data) {
            if (data) {
                self.changed = false;
                if ($.isArray(data.filters)) {
                    data.filters.forEach(function(x, i) { self.records[i].id = x.id; });
                }
            }
            if (self.refreshFn) {
                self.refreshFn();
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
            return m('input.form-input', { disabled: true, placeholder: $.resx('report.filterCriteria') });
        }

        var criteria = null;
        var attrs = {
            name: 'ReportFilter[' + index + '].Criteria',
            id: 'ReportFilter_' + index + '_Criteria',
            class: 'form-input required' + this.withError(filter.criteria),
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
                attrs.class = 'form-select required' + this.withError(filter.criteria);
                criteria = m('select', attrs, this.boolOptions);
                break;
            case this.filterTypes.date:
                if (filter.operatorId === this.filterOperatorIds.dateInterval) {
                    attrs.class = 'form-select required' + this.withError(filter.criteria);
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
                    attrs.class = 'form-select required' + this.withError(filter.criteria);
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
            class: 'form-input required' + this.withError(filter.criteria2, null, column.filterTypeId === this.filterTypes.date),
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
        return m('select.form-select.required', {
            name: 'ReportFilter[' + index + '].OperatorId', id: 'ReportFilter_' + index + '_OperatorId',
            disabled: !column || !this.opts.allowEdit,
            class: column ? this.withError(this.records[index].operatorId, true) : null,
            placeholder: $.resx('report.filterOperator'), onchange: this.setOperator.bind(this, index), value: this.records[index].operatorId
        }, this.withOptions(column && $.hasPositiveValue(column.filterTypeId) ?
            this.filterOperators[column.filterTypeId] : [{ id: 0, name: $.resx('report.filterOperator') }], this.records[index].operatorId, 'id', 'name')
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

            // @todo this isn't setting the `placeholder` default value correctly anymore it seems
            var attrs = {
                name: 'ReportFilter[' + index + '].ColumnId', class: 'form-select required' + self.withError(x.columnId, true),
                placeholder: $.resx('report.filterColumn'), onchange: self.setColumnId.bind(self, index), value: x.columnId
            };
            if (!self.opts.allowEdit) {
                attrs.disabled = true;
            }

            var filterColumns = self.isProc ? self.columnFn().filter(function(x) { return x.isParam; }) : self.columnFn();

            return m('.columns.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even' }, [
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
            this.opts.allowEdit ? m('.columns.pt-1', [
                m('.col-6', m('button.btn.btn-primary', {
                    type: 'button', role: 'button', onclick: this.saveFilters.bind(this)
                }, $.resx('save'))),
                m('.col-6', [
                    m('.text-right', [
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

/*!
 * Report group form component.
 */
(function(root, factory) {
    root.GroupForm = factory(root.m, root.$, root.Form, root.Help, root.Alertify);
})(this, function(m, $, Form, Help, Alertify) {
    'use strict';

    /**
     * Build the group form.
     * @param {Object} opts - Options for the form.
     * @returns {Object} Form instance.
     */
    function GroupForm(opts) {
        Form.call(this, {
            container: $.get('.group-table-wrapper', opts.content),
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
            newRecord: { id: 0, columnId: 0, displayOrder: 0 }
        }, opts.groups || []);

        this.reportId = opts.reportId;
        this.isProc = opts.isProc;
        this.saveGroupsUrl = opts.saveGroupsUrl;
        this.aggregator = opts.aggregatorId === 0 ? '' : opts.aggregatorId;
        this.aggregators = opts.aggregators || [];
        this.refreshFn = opts.refreshFn;
        this.columnFn = opts.columnFn;
    }

    GroupForm.prototype = Object.create(Form.prototype);
    GroupForm.prototype.constructor = GroupForm;

    /**
     * Save groups to the server then reload the table data.
     */
    GroupForm.prototype.saveGroups = function() {
        if ($.getAll('.mform-control-error', this.container).length > 0) {
            Alertify.error($.resx('fixIt'));
            return;
        }

        var self = this;
        $.ajax({
            method: 'PUT',
            url: self.saveGroupsUrl,
            data: {
                Id: self.reportId,
                GroupAggregator: self.aggregator === '' ? 0 : self.aggregator * 1,
                Groups: $.toPascalKeys(self.records)
            }
        }, function(data) {
            if (data) {
                if ($.isArray(data.groups)) {
                    data.groups.forEach(function(x, i) { self.records[i].id = x.id; });
                }
            }
            if (self.refreshFn) {
                self.refreshFn();
            }
        });
    };

    /**
     * Set the aggregator property.
     * @param {Event} e - Event that triggered the function.
     */
    GroupForm.prototype.setAggregator = function(e) {
        this.aggregator = this.targetVal(e);
    };

    /**
     * Build the groups.
     * @returns {Object[]} Array of Mithril nodes containing the groups.
     */
    GroupForm.prototype.groupView = function() {
        if (!this.columnFn()) {
            return null;
        }

        var self = this;
        return this.records.map(function(x, index) {
            x.displayOrder = index;
            var attrs = {
                name: 'ReportGroup[' + index + '].ColumnId', class: 'form-select required' + self.withError(x.columnId, true),
                placeholder: $.resx('report.groupColumn'), onchange: self.set.bind(self, index, 'columnId'), value: x.columnId
            };
            if (!self.opts.allowEdit) {
                attrs.disabled = true;
            }
            return m('.columns.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even' }, [
                m('input', { type: 'hidden', name: 'ReportGroup[' + index + '].Id', value: x.id }),
                m('input', { type: 'hidden', name: 'ReportGroup[' + index + '].DisplayOrder', value: index }),
                m('.col-10', [
                    m('select', attrs, self.withOptions(self.columnFn(), x.columnId, 'id', 'title'))
                ]),
                m('.col-2', self.opts.allowEdit ? self.buttonView.call(self, index, true) : null)
            ]);
        });
    };

    /**
     * Build the group form.
     * @returns {Object} Mithril node containing the form.
     */
    GroupForm.prototype.view = function() {
        if (this.isProc) {
            return m('.table-wrapper.p-t-1.col-12', m('.card.card-info', m('.card-body', $.resx('report.errorProcNoGroups'))));
        }

        var attrs = {
            name: 'Report.Aggregator', class: 'form-select', placeholder: $.resx('report.aggregator'),
            onchange: this.setAggregator.bind(this), value: this.aggregator
        };
        if (!this.opts.allowEdit) {
            attrs.disabled = true;
        }
        return m('.col-12.table-wrapper', m('.columns', [
            m('.col-2.mt-1', [
                m('.form-group', [
                    m('select', attrs, this.withOptions(this.aggregators, this.aggregator, 'id', 'name'))
                ]),
                this.opts.allowEdit ? m('.mt-1', m('button.btn.btn-primary', {
                    type: 'button', role: 'button', onclick: this.saveGroups.bind(this)
                }, $.resx('save'))) : null
            ]),
            m('.col-9.col-ml-auto.mt-1', [
                this.groupView(),
                this.opts.allowEdit ? m('.container.pt-1', m('.columns', [
                    m('.col-12', [
                        m('.btn-toolbar.text-right', [
                            m('button.btn.btn-info.mr-1', {
                                type: 'button', role: 'button', onclick: this.addRecord.bind(this)
                            }, $.resx('add')),
                            m('button.btn.btn-warning', {
                                type: 'button', role: 'button', disabled: !this.hasRecords(),
                                onclick: this.hasRecords() ? this.deleteAllRecords.bind(this) : $.noop
                            }, $.resx('deleteAll')),
                            m(Help, { enabled: this.opts.wantsHelp, message: $.resx('report.groupText') })
                        ])
                    ])
                ])) : null
            ])
        ]));
    };

    return GroupForm;
});

/*!
 * Report form component.
 */
(function(root, factory) {
    root.ReportDetails = factory(root.m, root.$, root.Alertify, root.BaseDetails, root.FilterForm, root.GroupForm, root.Table);
})(this, function(m, $, Alertify, BaseDetails, FilterForm, GroupForm, Table) {
    'use strict';

    /**
     * Declare ReportDetails class.
     * @param {Object} opts - Report settings
     */
    function ReportDetails(opts) {
        opts = opts || {};

        BaseDetails.call(this, opts);

        if (!(opts.reportColumns && opts.reportColumns.length)) {
            return;
        }

        this.content = opts.content;
        this.isProc = opts.loadAllData;

        var self = this;
        var saveUrl = opts.saveColumnsUrl;
        var saveStorageFunc = $.debounce(!opts.allowEdit ? $.noop : function(settings) {
            if ($.isNull(self.previousColumnWidths) || !$.equals(settings.columnWidths, self.previousColumnWidths)) {
                $.ajax({
                    method: 'PUT',
                    url: saveUrl,
                    data: {
                        Id: opts.reportId,
                        Columns: $.toPascalKeys(settings.columnWidths),
                        ReportWidth: settings.width * 1
                    },
                    block: false
                });
                self.previousColumnWidths = settings.columnWidths;
            }
        }, 250);

        var callback = this.processJson.bind(this);
        m.mount($.get('.report-data-container', opts.content), {
            view: function() {
                return m(Table, {
                    url: opts.dataUrl,
                    requestMethod: 'POST',
                    requestParams: { Id: opts.reportId, Save: true },
                    searchable: false,
                    loadAllData: opts.loadAllData,
                    editable: opts.allowEdit,
                    headerButtons: [{ type: 'a', attributes: { class: 'btn btn-primary mr-2', href: opts.exportUrl, target: '_blank' }, label: $.resx('export') }],
                    itemsPerPage: opts.rowLimit,
                    currentStartItem: 0,
                    sorting: opts.sortColumns || [],
                    storageFunction: saveStorageFunc,
                    width: opts.width,
                    columns: opts.reportColumns || [],
                    displayDateFormat: opts.dateFormat,
                    displayCurrencyFormat: opts.currencyFormat,
                    dataCallback: callback,
                    errorCallback: callback
                });
            }
        });
        this.previousColumnWidths = opts.reportColumns.map(function(x) { return { field: x.field, width: x.width * 1.0 }; });

        this.filterForm = new FilterForm({
            reportId: opts.reportId,
            content: opts.content,
            filters: opts.filters,
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
            dateFormat: opts.dateFormat,
            saveFiltersUrl: opts.saveFiltersUrl,
            filterOperators: opts.filterOperators,
            filterOperatorIds: opts.filterOperatorIds,
            filterTypes: opts.filterTypes,
            dateOperators: opts.dateOperators,
            lookups: opts.lookups,
            isProc: opts.isProc,
            refreshFn: this.refresh.bind(this),
            columnFn: this.getColumnList.bind(this)
        });
        this.filterForm.run();

        this.groupForm = new GroupForm({
            reportId: opts.reportId,
            content: opts.content,
            groups: opts.groups,
            allowEdit: opts.allowEdit,
            wantsHelp: opts.wantsHelp,
            saveGroupsUrl: opts.saveGroupsUrl,
            aggregatorId: opts.aggregatorId,
            aggregator: opts.aggregator,
            aggregators: opts.aggregators,
            isProc: opts.isProc,
            refreshFn: this.refresh.bind(this),
            columnFn: this.getColumnList.bind(this)
        });
        this.groupForm.run();
    }

    ReportDetails.prototype = Object.create(BaseDetails.prototype);
    ReportDetails.prototype.constructor = BaseDetails;

    /**
     * Handle the result of a query for report data.
     * @param {Object} json - Query result.
     * @returns {bool}  True if data is valid.
     */
    ReportDetails.prototype.processJson = function(json) {
        if (json.updatedDate && this.initDate) {
            var updateDate = new Date(json.updatedDate);
            if (updateDate && updateDate > this.initDate) {
                // report has been modified - warn the user of the refresh
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
    };

    ReportDetails.prototype.refresh = function() {
        $.dispatch($.get('.dash-table', this.content), $.events.tableRefresh);
    };

    ReportDetails.prototype.destroy = function() {
        $.dispatch($.get('.dash-table', this.content), $.events.tableDestroy);
        $.destroy(this.filterForm);
        $.destroy(this.groupForm);
    };

    return ReportDetails;
});

/*!
 * Functionality for displaying report forms.
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
        $.dispatch(document, $.events.dashboardReload);
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
        _shares[dlg.getId()] = new ShareForm({ content: dlg.getContent(), formName: 'Shares' });
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
