/*!
 * Wraps dataset join form functionality.
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
