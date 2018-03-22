/*!
 * Dialog mithril component.
 */
(function(root, factory) {
    root.Dialog = factory(root.m, root.$, root.Alertify);
})(this, function(m, $, Alertify) {
    'use strict';

    var _buttons = {
        'ok': 'Okay',
        'cancel': 'Cancel',
        'close': 'Close'
    };

    var _keys = {
        ENTER: 13,
        ESC: 27
    };

    /**
     * Convert a value for a field to the correct data type.
     * @param {Node} field - Input that we are converting value for.
     * @param {string} val - Value to convert.
     * @returns {string|number|bool} Returns correctly casted value.
     */
    var tryGetValue = function(field, val) {
        if ((field.name.substring(0, 2) === 'Is' || field.name.substring(0, 5) === 'Allow') && (field.value === 'true' || field.value === 'false')) {
            return field.value.toLowerCase() === 'true';
        } else if (field.type.toLowerCase() === 'number' || !($.isNull(val) || val.length == 0 || isNaN(val))) {
            return parseInt(val);
        }
        return val;
    };

    /**
     * Declare Dialog class.
     * @param {Object} opts - Dialog settings
     */
    function Dialog(opts) {
        this.opts = $.extend({
            id: null,
            title: null,
            basic: true,
            buttons: _buttons,
            content: null,
            target: null,
            onOkay: null,
            onCancel: null,
            onShow: null,
            parent: null
        }, opts || {});
        this.elements = {
            container: null,
            content: null
        };
        this.changed = false;
        this.run();
    }

    Dialog.prototype = {
        /**
         * Generates the components HTML.
         * @returns {Object} Mithril virtual node
         */
        view: function() {
            return m('.rd-dialog', { onkeydown: this.checkKey.bind(this), tabindex: 0 }, [
                m('.rd-commands', [
                    m('button.btn.btn-secondary.rd-close', { type: 'button', role: 'button', onclick: this.onCancel.bind(this) },
                        m('i.dash.dash-cancel.text-danger', { title: this.opts.buttons.close })
                    )
                ]),
                m('.rd-header.dialog-header', this.opts.title),
                m('.rd-content', { class: this.opts.basic ? 'rd-no-footer' : '' }, m.trust(this.opts.content)),
                this.opts.basic ? null : m('.rd-footer', [
                    m('.rd-buttons', [
                        m('button.rd-button.btn.btn-primary', {
                            type: 'button', role: 'button', onclick: this.onOkay.bind(this)
                        }, this.opts.buttons.ok),
                        m('button.rd-button.btn.btn-warning', {
                            type: 'button', role: 'button', onclick: this.onCancel.bind(this)
                        }, this.opts.buttons.cancel)
                    ])
                ])
            ]);
        },

        /**
         * Runs after the dialog is first created by mithril.
         * @param {Object} vnode - Mithril virtual node.
         */
        oncreate: function(vnode) {
            if (this.elements.content) {
                return;
            }
            var node = $.get('.rd-content', vnode.dom);
            if (!(node && node.firstElementChild)) {
                return;
            }
            node = node.firstElementChild;
            this.opts.title = node.getAttribute('data-title');
            this.opts.basic = node.hasAttribute('data-basic-dialog');
            m.redraw();

            var self = this;
            $.on(node, 'change', function() {
                self.changed = true;
            });
            this.elements.content = node;
            setTimeout(this.onShow.bind(this), 25);
        },

        /**
         * Run the component and render the dialog.
         */
        run: function() {
            this.elements.container = document.createElement('div');
            this.elements.container.id = 'dialog' + this.opts.id;
            $.addClass(this.elements.container, 'rd-dialog-container');
            document.body.appendChild(this.elements.container);
            m.mount(this.elements.container, {
                view: this.view.bind(this),
                oncreate: this.oncreate.bind(this),
            });

            $.dialogs.processContent(this.elements.content);
            setTimeout(this.checkEvent.bind(this, this.elements.content, 'data-event'), 25);
        },

        /**
         * Get the dialog id.
         * @returns {number} ID of dialog.
         */
        getId: function() {
            return this.opts.id;
        },

        /**
         * Get the dialog container node.
         * @returns {Node} DOM node of dialog container.
         */
        getContainer: function() {
            return this.elements.container;
        },

        /**
         * Get the dialog content node.
         * @returns {Node} DOM node of dialog content.
         */
        getContent: function() {
            return this.elements.content;
        },

        /**
         * Get the dialog target node.
         * @returns {Node} DOM node to target on close.
         */
        getTarget: function() {
            return this.opts.target;
        },

        /**
         * Find a form node inside the dialog.
         * @returns {Node} Form node if exists else null.
         */
        findForm: function() {
            return $.matches(this.elements.content, 'form.dash-form') ? this.elements.content : $.get('form.dash-form', this.elements.content);
        },

        /**
         * Find element to focus on when dialog is displayed.
         */
        onShow: function() {
            if ($.isFunction(this.opts.onShow)) {
                if (!this.opts.onShow.call(this)) {
                    return;
                }
            }
            document.title = this.opts.title;
            $.dialogs.focusNode(this.elements.content);
        },

        /**
         * User okayed dialog. Save changes; close dialog.
         */
        onOkay: function() {
            if ($.isFunction(this.opts.onOkay)) {
                if (!this.opts.onOkay.call(this)) {
                    return;
                }
            }

            var form = this.findForm();
            if (!form) {
                this.destroy();
                return;
            }
            if (!this.validateForm()) {
                return;
            }

            var self = this;
            var formData = this.serializeForm();
            $.ajax({
                method: form.hasAttribute('data-method') ? form.getAttribute('data-method') : 'POST',
                url: form.getAttribute('action'),
                data: formData,
                token: formData.__RequestVerificationToken
            }, function(responseData) {
                var target = self.opts.target;
                var parentDlg = $.dialogs.findDialogById(self.opts.parent);
                if (responseData.parentTarget && parentDlg) {
                    target = parentDlg.getTarget();
                }

                self.destroy();
                if (responseData.closeParent && parentDlg) {
                    parentDlg.destroy();
                }
                if (responseData.dialogUrl) {
                    $.dialogs.sendAjaxRequest(responseData.dialogUrl, 'GET', target);
                }
            });
        },

        /**
         * User canceled dialog. Check for changes; close dialog.
         * @param {Event} e - Event that triggered the cancel.
         */
        onCancel: function(e) {
            if ($.isFunction(this.opts.onCancel)) {
                if (!this.opts.onCancel.call(this)) {
                    return;
                }
            }

            if (!this.changed || !this.findForm()) {
                this.destroy();
                return;
            }

            Alertify.confirm($.resx('discardChanges'), this.destroy.bind(this), function() {
                if (e.target) {
                    e.target.focus();
                }
            });
        },

        /**
         * Trigger okay or cancel action based on keydown.
         * @param {KeyboardEvent} e - Keydown event that triggered this.
         */
        checkKey: function(e) {
            if (e.keyCode === _keys.ESC) {
                this.onCancel(e);
            }
            if (e.keyCode === _keys.ENTER && !this.opts.basic) {
                this.onOkay(e);
            }
        },

        /**
         * Dispatch event named in attribute `attrName` if it exists.
         * @param {Node} node - Node to look for an event attribute in.
         * @param {string} attrName - Name of the attribute to check for the event in.
         */
        checkEvent: function(node, attrName) {
            if (!node || !node.hasAttribute(attrName)) {
                return;
            }
            var ev = node.getAttribute(attrName);
            if ($.events.hasOwnProperty(ev)) {
                document.dispatchEvent($.events[ev]);
            }
        },

        /**
         * Convert form data into an object.
         * @returns {Object} Form data.
         */
        serializeForm: function() {
            var form = this.findForm();
            if (!form) {
                return {};
            }

            var field, data = {};
            var len = form.elements.length;
            for (var i = 0; i < len; i++) {
                field = form.elements[i];
                if (field.name && !field.disabled && field.type !== 'file' && field.type !== 'reset' && field.type !== 'submit' && field.type !== 'button') {
                    if (field.type === 'select' && field.hasAttribute('multiple')) {
                        data[field.name] = Array.apply(null, form.elements[i].options).filter(function(x) {
                            return x.selected;
                        }).map(function(x) {
                            return tryGetValue(field, x.value);
                        });
                    } else if (field.type === 'checkbox') {
                        if (field.checked) {
                            if (data.hasOwnProperty(field.name) || $.hasClass(field, 'custom-control-input-multiple')) {
                                if (!$.isArray(data[field.name])) {
                                    data[field.name] = $.isNull(data[field.name]) || data[field.name] === false ? [] : [data[field.name]];
                                }
                                data[field.name].push(tryGetValue(field, field.value));
                            } else {
                                data[field.name] = tryGetValue(field, field.value);
                            }
                        } else if (!data.hasOwnProperty(field.name)) {
                            data[field.name] = $.hasClass(field, 'custom-control-input-multiple') ? [] : false;
                        }
                    } else if (field.type !== 'radio' || field.checked) {
                        data[field.name] = tryGetValue(field, field.value);
                    }
                }
            }
            return data;
        },

        /**
         * Set tab status when validating form.
         * @param {Node} el - Node that has a validation error.
         */
        setTabStatus: function(el) {
            var tab = $.closest('.tab-pane', el);
            if (tab) {
                // add error class to tab
                var id = tab.getAttribute('aria-labelledby');
                if (id) {
                    if ($.hasClass(el, 'mform-control-error')) {
                        $.addClass($.get('#' + id), 'tab-validation-error');
                    } else {
                        $.removeClass($.get('#' + id), 'tab-validation-error');
                    }
                }
            }
        },

        /**
         * Run the form validator and display an error if needed.
         * @returns {bool} Return true if form is valid, else false.
         */
        validateForm: function() {
            var form = this.findForm();
            form.dispatchEvent($.events.formValidate);

            var tabs = $.getAll('.nav-tabs.nav-item.nav-link', form);
            tabs.forEach(function(x) {
                $.removeClass(x, 'tab-validation-error');
            });

            var mErrors = $.getAll('.mform-control-error', form);
            if (mErrors.length) {
                mErrors.forEach(this.setTabStatus);
            }

            if (mErrors.length || $.getAll('.form-control-error', form).length) {
                Alertify.error($.resx('fixIt'));
                return false;
            }

            return true;
        },

        /**
         * Clean up our mess.
         */
        destroy: function() {
            this.checkEvent(this.elements.content, 'data-close-event');

            var tableNode = $.get('.dash-table', this.elements.content);
            if (tableNode) {
                tableNode.table.destroy();
            }

            m.mount(this.elements.container, null);
            document.body.removeChild(this.elements.container);
            $.dialogs.removeDialog(this.opts.id);
            $.dialogs.refreshTable();
        }
    };

    return Dialog;
});   
