/*!
 * Wraps shared functionality for report and chart detail pages.
 */
(function(root, factory) {
    root.BaseDetails = factory(root.$, root.Alertify);
})(this, function($, Alertify) {
    'use strict';

    /**
     * Declare base details class.
     * @param {Object} opts - Form settings
     */
    function BaseDetails(opts) {
        opts = opts || {};
        this.content = opts.content;
        this.columnList = opts.columns || [];

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
            newRecord: { id: 0, userId: 0, roleId: 0 }
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
            name: this.formName + '[' + index + '].UserId', class: 'form-select' + ($.hasPositiveValue(share.userId) || $.hasPositiveValue(share.roleId) ? '' : ' mform-control-error'),
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
            name: this.formName + '[' + index + '].RoleId', class: 'form-select' + ($.hasPositiveValue(share.userId) || $.hasPositiveValue(share.roleId) ? '' : ' mform-control-error'),
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
            m('.columns', [
                m('.col-5', m('h5', $.resx('user'))),
                m('.col-5', m('h5', $.resx('role'))),
                m('.col-2')
            ]),
            self.records.map(function(share, index) {
                return m('.columns.wrapper-row', { class: index % 2 === 1 ? 'odd' : 'even', key: share._index }, [
                    m('input', { type: 'hidden', name: self.formName + '[' + index + '].Id', value: share.id }),
                    m('.col-5.pr-2',
                        m('select', self.userSelectAttr(share, index), self.withOptions(self.users, share.userId, 'id', 'fullName'))
                    ),
                    m('.col-5',
                        m('select', self.roleSelectAttr(share, index), self.withOptions(self.roles, share.roleId, 'id', 'name'))
                    ),
                    m('.col-2', self.buttonView(index, false))
                ]);
            }),
            m('.columns.pt-1', [
                m('.col-12', [
                    m('.text-right', [
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
