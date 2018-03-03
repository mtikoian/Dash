/*!
 * Wraps report group form functionality.
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
            newRecord: { id: null, columnId: '', displayOrder: 0 }
        }, opts.groups || []);

        this.isProc = opts.isProc;
        this.saveGroupsUrl = opts.saveGroupsUrl;
        this.aggregator = opts.aggregatorId === 0 ? '' : opts.aggregatorId;
        this.aggregators = opts.aggregators || [];
        this.dataTable = opts.dataTable;
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
            data: { groupAggregator: this.aggregator === '' ? 0 : this.aggregator, groups: this.records }
        }, function(data) {
            if (data) {
                if ($.isArray(data.groups)) {
                    data.groups.forEach(function(x, i) { self.records[i].id = x.id; });
                }
            }
            if (self.dataTable) {
                self.dataTable.loadData();
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
                name: 'ReportGroup[' + index + '].ColumnId', class: 'form-control custom-select required' + self.withError(x.columnId, true),
                placeholder: $.resx('report.groupColumn'), onchange: self.set.bind(self, index, 'columnId'), value: x.columnId
            };
            if (!self.opts.allowEdit) {
                attrs.disabled = true;
            }
            return m('.row.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even' }, [
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
            name: 'Report.Aggregator', class: 'form-control custom-select', placeholder: $.resx('report.aggregator'),
            onchange: this.setAggregator.bind(this), value: this.aggregator
        };
        if (!this.opts.allowEdit) {
            attrs.disabled = true;
        }
        return m('.col-12.table-wrapper', m('.row', [
            m('.col-2.mt-1', [
                m('.row.form-group', [
                    m('select', attrs, this.withOptions(this.aggregators, this.aggregator, 'id', 'name'))
                ]),
                this.opts.allowEdit ? m('.row.mt-1', m('button.btn.btn-primary', {
                    type: 'button', role: 'button', onclick: this.saveGroups.bind(this)
                }, $.resx('save'))) : null
            ]),
            m('.col-9.offset-1.mt-1', [
                this.groupView(),
                this.opts.allowEdit ? m('.row.pt-1', [
                    m('.col-6.offset-6', [
                        m('.btn-toolbar.float-right', [
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
                ]) : null
            ])
        ]));
    };

    return GroupForm;
});