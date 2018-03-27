/*!
 * Wraps dialog functionality.
 */
(function($, Alertify, Dialog, Table, Dropdown, Tab, CollapsibleList, Validator, DatePicker) {
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
        { selector: '[data-toggle="dropdown"]', action: function() { new Dropdown(this); } },
        { selector: '[data-toggle="tab"], [data-toggle="pill"]', action: function() { new Tab(this); } },
        { selector: '.dash-table', action: function() { tableLoad(this); } },
        { selector: '.dash-ajax', action: function() { $.on(this, 'click', handleAjaxRequest); } },
        { selector: '.dash-form', action: function() { $.on(this, 'submit', function(e) { e.preventDefault(); }, true); } },
        {
            selector: '.dash-context-help', action: function() {
                $.on(this, 'click', function(e) {
                    Alertify.alert(this.getAttribute('data-message').replace(/&quot;/g, '"'), focusOnClose.bind(e), focusOnClose.bind(e));
                });
            }
        },
        { selector: '.dash-collapsible-list', action: function() { new CollapsibleList(this); } },
        {
            selector: '[data-toggle="validator"]', action: function() {
                new Validator(this, { match: $.resx('errorMatch'), minlength: $.resx('errorMinLength') });
            }
        },
        { selector: '.dash-input-replace', action: function() { $.on(this, 'click', inputReplace); } }
    ];

    /**
     * Store list of open dialogs.
     */
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

    /**
     * Check if any dialogs are open.
     */
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
            document.title = $.get('#dashboard').getAttribute('data-title');
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

        $.ajax({
            method: method || 'GET',
            url: url,
            data: promptValue ? { Prompt: promptValue } : null
        }, function(responseData) {
            if (!responseData.content) {
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
                openDialog(responseData.content, target);
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
     * Initialize an table instance
     * @param {Node} node - Node containing the data url for the table settings.
     */
    var tableLoad = function(node) {
        var json = node.getAttribute('data-json');
        if (json) {
            var opts = JSON.parse(json);
            opts.content = node;
            node.table = new Table(opts);
            node.removeAttribute('data-json');
        } else {
            $.ajax({
                method: 'GET',
                url: node.getAttribute('data-url')
            }, function(opts) {
                node.table = new Table(opts);
            });
        }
    };

    /**
     * Refresh data for the table instance in the active dialog.
     */
    var refreshTable = function() {
        var node = $.get('.dash-table', getActiveContent());
        if (node && node.table) {
            node.table.refresh();
        }
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
        node = node instanceof Event ? null : node instanceof Table ? node.table : node;
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

    /**
     * Set up the functions that are exposed publicly.
     */
    $.dialogs = {
        openDialog: openDialog,
        findDialogById: findDialogById,
        focusNode: focusNode,
        focusOnClose: focusOnClose,
        getActiveContent: getActiveContent,
        getActiveDialog: getActiveDialog,
        hasOpenDialog: hasOpenDialog,
        processContent: processContent,
        refreshTable: refreshTable,
        removeDialog: removeDialog,
        sendAjaxRequest: sendAjaxRequest
    };
})(this.$, this.Alertify, this.Dialog, this.Table, this.Dropdown, this.Tab, this.CollapsibleList, this.Validator, this.DatePicker);
