/*!
 * Wraps share form functionality. Used for report and chart.
 */
(function(root, factory) {
    root.ShareForm = factory(root.m, root.$, root.Form);
})(this, function(m, $, Form) {
    'use strict';

    /**
     * Build the share form.
     * @param {Object} opts - Options for the form.
     * @returns {Object} Form instance.
     */
    function ShareForm(opts) {
        var container = $.get('.share-table-wrapper', opts.content);
        var formOpts = JSON.parse(container.getAttribute('data-json'));
        container.removeAttribute('data-json');

        Form.call(this, {
            container: container,
            columns: {
                id: { type: 'int' },
                userId: { type: 'int' },
                roleId: { type: 'int' }
            },
            appendRecord: true,
            wantsHelp: formOpts.wantsHelp,
            newRecord: { id: null, userId: 0, roleId: 0 }
        }, formOpts.shares || []);

        this.users = formOpts.userList || [];
        this.roles = formOpts.roleList || [];
        this.formName = opts.formName;
    }

    ShareForm.prototype = Object.create(Form.prototype);
    ShareForm.prototype.constructor = ShareForm;

    /**
     * Build the user dropdown attributes.
     * @param {Object} share - Share to build the dropdown for.
     * @param {number} index - Index of the share record.
     * @returns {Object} Dropdown attribute object.
     */
    ShareForm.prototype.userSelectAttr = function(share, index) {
        var res = {
            name: this.formName + '[' + index + '].UserId', class: 'form-control custom-select' + ($.hasPositiveValue(share.userId) || $.hasPositiveValue(share.roleId) ? '' : ' mform-control-error'),
            placeholder: $.resx('selectUser'), onchange: this.set.bind(this, index, 'userId'), value: share.userId
        };
        if ($.hasPositiveValue(share.roleId)) {
            res['disabled'] = true;
        }
        return res;
    };

    /**
     * Build the role dropdown attributes.
     * @param {Object} share - Share to build the dropdown for.
     * @param {number} index - Index of the share record.
     * @returns {Object} Dropdown attribute object.
     */
    ShareForm.prototype.roleSelectAttr = function(share, index) {
        var res = {
            name: this.formName + '[' + index + '].RoleId', class: 'form-control custom-select' + ($.hasPositiveValue(share.userId) || $.hasPositiveValue(share.roleId) ? '' : ' mform-control-error'),
            placeholder: $.resx('selectRole'), onchange: this.set.bind(this, index, 'roleId'), value: share.roleId
        };
        if ($.hasPositiveValue(share.userId)) {
            res['disabled'] = true;
        }
        return res;
    };

    /**
     * Create the view for the share form.
     * @returns {Object} Mithril node containing the share form.
     */
    ShareForm.prototype.view = function() {
        var self = this;
        return m('.table-wrapper', [
            m('.row', [
                m('.col-5', m('h5', $.resx('user'))),
                m('.col-5', m('h5', $.resx('role'))),
                m('.col-2')
            ]),
            self.records.map(function(share, index) {
                return m('.row.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even', key: share._index }, [
                    m('input', { type: 'hidden', name: self.formName + '[' + index + '].Id', value: share.id }),
                    m('.col-5',
                        m('select', self.userSelectAttr(share, index), self.withOptions(self.users, share.userId, 'id', 'fullName'))
                    ),
                    m('.col-5',
                        m('select', self.roleSelectAttr(share, index), self.withOptions(self.roles, share.roleId, 'id', 'name'))
                    ),
                    m('.col-2', self.buttonView(index, false))
                ]);
            }),
            m('.row.pt-1', [
                m('.col-6.offset-6', [
                    m('.float-right', [
                        m('button.btn.btn-info.mr-2', { type: 'button', role: 'button', onclick: self.addRecord.bind(self) }, $.resx('add')),
                        m('button.btn.btn-warning', {
                            type: 'button', role: 'button', disabled: !self.hasRecords(),
                            onclick: self.hasRecords() ? self.deleteAllRecords.bind(self) : $.noop
                        }, $.resx('deleteAll'))
                    ])
                ])
            ])
        ]);
    };

    return ShareForm;
});
