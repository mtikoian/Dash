/*!
 * Dialog mithril component.
 */
(function(root, factory) {
    root.Dialog = factory(root.m, root.$, root.Alertify, root.Table);
})(this, function(m, $, Alertify, Table) {
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
        var fieldName = field.name.split('.').pop();
        var typeHint = (field.getAttribute('typehint') || '').toLowerCase(); // might be able to remove this typeHint stuff later when cron is working correctly
        if (typeHint === 'text') {
            return val;
        } else if (typeHint === 'bool' || (fieldName.substring(0, 2) === 'Is' || fieldName.substring(0, 5) === 'Allow') && (field.value.toLowerCase() === 'true' || field.value.toLowerCase() === 'false')) {
            return field.value.toLowerCase() === 'true';
        } else if (typeHint === 'number' || field.type.toLowerCase() === 'number' || field.name.slice(-2) === 'Id' || !($.isNull(val) || val.length == 0 || isNaN(val))) {
            return val.length ? parseInt(val) : null;
        }
        return val;
    };

    /**
     * Try to set a value in an object.
     * @param {Object} obj - Object to add value to.
     * @param {Node} field - Input that we are converting value for.
     * @param {string} name - Name of property.
     * @param {string} val - Value to set.
     * @returns {Object} Returns correctly updated object.
     */
    var trySetValue = function(obj, field, name, val) {
        if (obj.hasOwnProperty(name) || $.hasClass(field, 'custom-control-input-multiple')) {
            if (!$.isArray(obj[name])) {
                obj[name] = $.isNull(obj[name]) ? [] : [obj[name]];
            }
            if (!$.isNull(val)) {
                obj[name].push(val);
            }
        } else if (!$.isNull(val)) {
            obj[name] = val;
        }
        return obj;
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
                m('.rd-header.columns', [
                    m('.col-8.mt-1.text-no-select.dialog-header', this.opts.title),
                    m('.col-4.text-right', m('button.btn.btn-secondary.rd-close', { type: 'button', role: 'button', onclick: this.onCancel.bind(this) },
                        m('i.dash.dash-cancel.text-error', { title: this.opts.buttons.close })
                    ))
                ]),
                m('.rd-content', { class: this.opts.basic ? 'rd-no-footer' : '' }, this.contentView()),
                this.opts.basic ? null : m('.rd-footer', [
                    m('button.btn.btn-primary', {
                        type: 'button', role: 'button', onclick: this.onOkay.bind(this)
                    }, this.opts.buttons.ok),
                    m('button.btn.btn-warning', {
                        type: 'button', role: 'button', onclick: this.onCancel.bind(this)
                    }, this.opts.buttons.cancel)
                ])
            ]);
        },

        contentView: function() {
            if (!this.opts.content.component) {
                return m.trust(this.opts.content);
            }

            this.opts.title = this.opts.content.title;
            this.opts.basic = this.opts.content.basic;
            if (this.opts.content.component.toLowerCase() === 'table') {
                return m('.col-12', m(Table, this.opts.content.data));
            }
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
            if ($.isNull(this.opts.title)) {
                this.opts.title = node.getAttribute('data-title');
                this.opts.basic = node.hasAttribute('data-basic-dialog');
                m.redraw();
            }
            var self = this;
            $.on(node, 'change', function() {
                self.changed = true;
            });
            this.elements.content = node;
            setTimeout(this.onShow.bind(this), 25);
        },

        run: function() {
            this.elements.container = document.createElement('div');
            this.elements.container.id = 'dialog' + this.opts.id;
            $.addClass(this.elements.container, 'rd-dialog-container');
            document.body.appendChild(this.elements.container);
            m.mount(this.elements.container, {
                view: this.view.bind(this),
                oncreate: this.oncreate.bind(this),
            });

            if (!this.opts.content.component) {
                $.dialogs.processContent(this.elements.content);
                setTimeout(this.checkEvent.bind(this, this.elements.content, 'data-event'), 25);
            }
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
                $.dispatch(document, $.events[ev]);
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
            var bracketRegEx = /\[([^\]]+)\]/;
            for (var i = 0; i < len; i++) {
                field = form.elements[i];
                if (!field.name || field.disabled || ['file', 'reset', 'submit', 'button'].indexOf(field.type) > -1) {
                    continue;
                }

                var value = null;
                if (field.type === 'select' && field.hasAttribute('multiple')) {
                    value = Array.apply(null, form.elements[i].options).filter(function(x) {
                        return x.selected;
                    }).map(function(x) {
                        return tryGetValue(field, x.value);
                    });
                } else if (field.type === 'checkbox') {
                    if (field.checked) {
                        value = tryGetValue(field, field.value);
                    }
                } else if (field.type !== 'radio' || field.checked) {
                    value = tryGetValue(field, field.value);
                }

                var pieces = field.name.split('.');
                var name = field.name;
                if (pieces.length > 1) {
                    var matches = bracketRegEx.exec(pieces[0]);
                    name = matches.length > 1 ? pieces[0].replace(matches[0], '') : pieces[0];
                    if (!data.hasOwnProperty(name)) {
                        data[name] = [];
                    }
                    if (matches.length > 1) {
                        if (!data[name].hasOwnProperty(matches[1])) {
                            data[name][matches[1]] = {};
                        }
                        data[name][matches[1]] = trySetValue(data[name][matches[1]], field, pieces[1], value);
                    } else {
                        data[name][matches[0]] = trySetValue(data[name][matches[0]], field, pieces[1], value);
                    }
                } else {
                    data = trySetValue(data, field, name, value);
                }
            }
            return data;
        },

        /**
         * Check form state and display an error if needed.
         * @returns {bool} Return true if form is valid, else false.
         */
        validateForm: function() {
            var form = this.findForm();
            if (!form.checkValidity()) {
                form.reportValidity(); // triggers browser validation ui
                Alertify.error($.resx('fixIt'));
                return false;
            }
            if ($.getAll('.mform-control-error', form).length) {
                Alertify.error($.resx('fixIt'));
                return false;
            }

            return true;
        },

        destroy: function() {
            this.checkEvent(this.elements.content, 'data-close-event');
            $.dispatch($.get('.dash-table', this.elements.content), $.events.tableDestroy);
            m.mount(this.elements.container, null);
            document.body.removeChild(this.elements.container);
            $.dialogs.removeDialog(this.opts.id);
            $.dialogs.refreshTable();
        }
    };

    return Dialog;
});

/*!
 * Wraps dialog functionality.
 */
(function(m, $, Alertify, Dialog, Table, Tab, CollapsibleList, DatePicker) {
    'use strict';

    /**
     * Set focus on an element after a dialog closes.
     * @this {Event} Event that originally opened the dialog.
     */
    var focusOnClose = function() {
        if (this && this.target) {
            this.target.focus();
        }
    };

    /**
     * Selectors and callback function to create events.
     */
    var _contentActions = [
        { selector: '[data-toggle="tab"]', action: function() { new Tab(this); } },
        { selector: '.dash-ajax', action: function() { $.on(this, 'click', handleAjaxRequest); } },
        { selector: '.dash-table', action: function() { tableLoad(this); } },
        { selector: '.dash-form', action: function() { $.on(this, 'submit', function(e) { e.preventDefault(); }, true); } },
        {
            selector: '.dash-context-help', action: function() {
                $.on(this, 'click', function(e) {
                    Alertify.alert(this.getAttribute('data-message').replace(/&quot;/g, '"'), focusOnClose.bind(e), focusOnClose.bind(e));
                });
            }
        },
        { selector: '.dash-collapsible-list', action: function() { new CollapsibleList(this); } },
        { selector: '.dash-input-replace', action: function() { $.on(this, 'click', inputReplace); } },
    ];

    var _dialogs = [];

    /**
     * Find a dialog in the internal list by ID.
     * @param {number} id - Dialog ID
     */
    var findDialogById = function(id) {
        var dlgs = _dialogs.filter(function(x) {
            return x.getId() === id;
        });
        return dlgs.length ? dlgs[0] : null;
    };

    /**
     * Get active dialog.
     * @param {Object} Return currently open dialog object.
     */
    var getActiveDialog = function() {
        if (!_dialogs.length) {
            return null;
        }
        return findDialogById(Math.max.apply(Math, _dialogs.map(function(x) { return x.getId(); })));
    };

    /**
     * Get active dialog content.
     * @param {Object} Return content node of currently open dialog object.
     */
    var getActiveContent = function() {
        var dlg = getActiveDialog();
        return dlg ? dlg.getContent() : null;
    };

    var hasOpenDialog = function() {
        return _dialogs.length > 0;
    };

    /**
     * Remove a dialog from internal list.
     * @param {number} id - Dialog ID
     */
    var removeDialog = function(id) {
        _dialogs = _dialogs.filter(function(x) {
            return x.getId() !== id;
        });
        var activeDialog = getActiveDialog();
        if (activeDialog) {
            setTimeout(activeDialog.onShow.bind(activeDialog), 25);
        } else {
            // back to dashboard so set title
            var dashboard = $.get('#bodyContent');
            if (dashboard) {
                document.title = dashboard.getAttribute('data-title');
            }
        }
    };

    /**
     * Handle action from a click on an ajax link.
     * @param {Event} e - Event that triggered the request.
     */
    var handleAjaxRequest = function(e) {
        if (!(e && e.target)) {
            return;
        }
        e.preventDefault();
        e.target.blur();

        var obj = e.target;
        while (obj !== document.body && obj.parentNode && !$.hasClass(obj, 'dash-ajax')) {
            obj = obj.parentNode;
        }
        if (!obj || !(obj.hasAttribute('href') || obj.hasAttribute('data-href')) || $.hasClass(obj, 'disabled')) {
            return;
        }

        var url = obj.getAttribute('href') || obj.getAttribute('data-href');
        var method = obj.getAttribute('data-method') || 'GET';
        var message = obj.getAttribute('data-message');
        var target = obj.getAttribute('target');

        if ($.hasClass(obj, 'dash-confirm')) {
            Alertify.dismissAll();
            Alertify.confirm(message, sendAjaxRequest.bind(this, url, method, obj), function() { e.target.focus(); });
        } else if ($.hasClass(obj, 'dash-prompt')) {
            Alertify.dismissAll();
            Alertify.prompt(message, checkPrompt.bind(this, url, method, obj));
        } else if (target) {
            window.open(url, target);
        } else {
            sendAjaxRequest(url, method, obj);
        }
    };

    /**
     * Send a request to the server and display the results
     * @param {string} url - URL to send request for.
     * @param {string} method - Request method type.
     * @param {Node} target - Node that requested the data.
     * @param {Event} e - Event that triggered the request.
     * @param {string} promptValue - If coming from a prompt dialog, the value entered.
     */
    var sendAjaxRequest = function(url, method, target, e, promptValue) {
        if (!url) {
            return;
        }

        if (promptValue) {
            if (url.indexOf('?') > -1) {
                url += '&Prompt=' + encodeURIComponent(promptValue);
            } else {
                url += '?Prompt=' + encodeURIComponent(promptValue);
            }
        }
        $.ajax({
            method: method || 'GET',
            url: url
        }, function(responseData) {
            if (!(responseData.content || responseData.component)) {
                refreshTable();
                return;
            }

            if (target && target.hasAttribute('data-update-target')) {
                var targetSelector = target.getAttribute('data-update-target');
                var updateObj;
                if (targetSelector.substr(1) === '#') {
                    updateObj = $.get(targetSelector);
                } else {
                    var dialog = getActiveDialog();
                    if (dialog) {
                        updateObj = $.get(targetSelector, dialog.getContainer());
                    }
                }
                if (updateObj) {
                    if (responseData.html) {
                        updateObj.innerHTML = responseData.content;
                        processContent(updateObj);
                    } else {
                        $.setText(updateObj, responseData.content);
                    }
                }
            } else {
                var newNode = $.createNode(responseData.content);
                if (newNode && newNode.id) {
                    var targetNode = $.get('#' + newNode.id);
                    if (targetNode) {
                        // @todo do some sort of destroy logic here
                        targetNode.parentNode.replaceChild(newNode, targetNode);
                        processContent(newNode);
                    }
                    return;
                }

                openDialog($.isNull(responseData.component) ? responseData.content : responseData, target);
            }
        });
    };

    /**
     * Open a new dialog.
     * @param {string} content - HTML content for the dialog
     * @param {Node} target - Node that triggered the dialog to open.
     * @param {Function} onOkay - Function to run if the user clicks ok.
     * @param {Function} onCancel - Function to run if the user clicks cancel.
     * @param {Function} onShow - Function to run after the dialog loads.
     */
    var openDialog = function(content, target, onOkay, onCancel, onShow) {
        Alertify.dismissAll();

        // get max dialog id, and increment
        var id = _dialogs.length ? Math.max.apply(Math, _dialogs.map(function(x) { return x.getId(); })) + 1 : 1;
        var activeDlg = getActiveDialog();
        _dialogs.push(new Dialog({
            id: id, content: content, target: target, onOkay: onOkay, onCancel: onCancel, onShow: onShow,
            buttons: { 'ok': $.resx('okay'), 'cancel': $.resx('cancel'), 'close': $.resx('close') },
            parent: activeDlg ? activeDlg.getId() : null
        }));
    };

    /**
     * Check that a prompt value was supplied.
     * @param {string} url - URL to send request for.
     * @param {string} method - Request method type.
     * @param {Node} target - Node that requested the data.
     * @param {string} promptValue - If coming from a prompt dialog, the value entered.
     * @param {Event} e - Event that triggered the request.
     */
    var checkPrompt = function(url, method, target, promptValue, e) {
        if (!$.hasValue(promptValue)) {
            Alertify.error($.resx('errorNameRequired'));
            return false;
        }
        sendAjaxRequest.call(this, url, method, target, e, promptValue);
    };

    /**
     * Initialize a table instance
     * @param {Node} node - Node containing the data for the table.
     */
    var tableLoad = function(node) {
        var json = node.getAttribute('data-json');
        if (json) {
            var opts = JSON.parse(json);
            m.mount(node.parentElement, {
                view: function() {
                    return m(Table, opts);
                }
            });
            /*
            // @todo when destroying content that contains a table, i need to unmount it.
            m.mount(node, null);
            */
        }
    };

    /**
     * Refresh data for the table instance in the active dialog.
     */
    var refreshTable = function() {
        var content = getActiveContent();
        if (!content) {
            return;
        }
        $.dispatch($.hasClass(content, 'dash-table') ? content : $.get('.dash-table', content), $.events.tableRefresh);
    };

    /**
     * Focus on the first error or input.
     * @param {Node} node - Parent node to search in.
     */
    var focusNode = function(node) {
        if (!node) {
            return null;
        }
        var elems = $.getAll('.form-control-error:not([type="hidden"]):not([disabled]):not([readonly]), .mform-control-error:not([type="hidden"]):not([disabled]):not([readonly])', node).filter($.isVisible);
        if (!elems.length) {
            elems = $.getAll('input:not([type="hidden"]):not([disabled]):not([readonly]), select:not([disabled]):not([readonly])', node).filter($.isVisible);
        }
        if (!elems.length) {
            elems = $.getAll('button:not([disabled]), a:not([disabled]), [tabindex]:not([disabled]):not([tabindex="-1"])', node).filter($.isVisible);
        }
        if (!elems.length) {
            var dlg = $.closest('.rd-dialog', node);
            if (dlg) {
                elems = $.getAll('.rd-close', dlg).filter($.isVisible);
            }
        }
        if (elems.length) {
            elems[0].focus();
        }
    };

    /**
     * Replace the value of the data-target node with the data-value from this. Used for providing defaults via a dropdown.
     */
    var inputReplace = function() {
        if (this.hasAttribute('data-target') && this.hasAttribute('data-value')) {
            var target = $.get('#' + this.getAttribute('data-target'));
            if (target && !$.isNull(target.value)) {
                target.value = this.getAttribute('data-value');
            }
        }
    };

    /**
     * Process node content adding events.
     * @param {Node} node - Node to add events to.
     */
    var processContent = function(node) {
        node = $.isEvent(node) ? null : node;
        if (!node) {
            return;
        }

        // process all the content actions
        var elems;
        _contentActions.forEach(function(act) {
            elems = $.getAll(act.selector, node);
            if ($.matches(node, act.selector)) {
                elems.push(node);
            }
            elems.forEach(function(x) {
                act.action.call(x);
            });
        });

        if (node.nodeName === 'BODY') {
            var lang = node.getAttribute('data-lang');
            if (lang && lang !== 'en') {
                DatePicker.localize({ locale: lang });
            }
            focusNode(node);
        }
    };

    $.dialogs = {
        openDialog: openDialog,
        findDialogById: findDialogById,
        focusNode: focusNode,
        focusOnClose: focusOnClose,
        getActiveContent: getActiveContent,
        getActiveDialog: getActiveDialog,
        handleAjaxRequest: handleAjaxRequest,
        hasOpenDialog: hasOpenDialog,
        processContent: processContent,
        refreshTable: refreshTable,
        removeDialog: removeDialog,
        sendAjaxRequest: sendAjaxRequest
    };
})(this.m, this.$, this.Alertify, this.Dialog, this.Table, this.Tab, this.CollapsibleList, this.DatePicker);
