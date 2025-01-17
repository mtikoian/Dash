﻿/*!
 * Alertify library
 * https://github.com/alertifyjs/alertify.js
 */
(function(root, $) {
    'use strict';

    /**
     * Hide an element using a transition if available.
     * @param {Node} el - Element to hide.
     */
    var _hideElement = function(el) {
        if (!el)
            return;

        $.removeClass(el, 'show');
        $.hide(el);

        if (el.parentNode) {
            var removeThis = function() {
                el && el.parentNode && el.parentNode.removeChild(el);
            };
            $.on(el, 'transitionend', removeThis);
            // Fallback for no transitions.
            setTimeout(removeThis, 500);
        }
    };

    /**
     * Alertify private object.
     * @type {Object}
     */
    var _alertify = {
        parent: document.body,
        defaultOkLabel: 'Okay',
        okLabel: 'Okay',
        defaultCancelLabel: 'Cancel',
        cancelLabel: 'Cancel',
        maxLogItems: 4,
        delay: 5000,
        logContainerClass: 'alertify-logs bottom left',
        dialogs: {
            buttons: {
                holder: '<nav>{{buttons}}</nav>',
                ok: '<button class="ok btn btn-primary" tabindex="1">{{ok}}</button>',
                cancel: '<button class="cancel btn btn-warning" tabindex="2">{{cancel}}</button>'
            },
            input: '<div class="m-2"><input type="text" class="form-input" required autofocus></div>',
            message: '<p class="msg">{{message}}</p>'
        },

        /**
         * Build the proper message box.
         * @param {Object} item - Current object in the queue.
         * @return {string} An HTML string of the message box.
         */
        build: function(item) {
            var btnTxt = this.dialogs.buttons.ok;
            var html = '<div class="dialog"><div class="dialog-content">' + this.dialogs.message.replace('{{message}}', item.message);

            if (item.type === 'confirm' || item.type === 'prompt')
                btnTxt = this.dialogs.buttons.ok + this.dialogs.buttons.cancel;
            if (item.type === 'prompt')
                html += this.dialogs.input;

            html = (html + this.dialogs.buttons.holder + '</div></div>')
                .replace('{{buttons}}', btnTxt)
                .replace('{{ok}}', this.okLabel)
                .replace('{{cancel}}', this.cancelLabel);

            return html;
        },

        /**
         * Close the log messages.
         * @param {Object} elem - HTML Element of log message to close.
         * @param {number} wait - [optional] Time (in ms) to wait before automatically hiding the message, if 0 never hide.
         */
        close: function(elem, wait) {
            $.on(elem, 'click', _hideElement.bind(null, elem));

            wait = !isNaN(+wait) ? +wait : this.delay;
            if (wait < 0)
                _hideElement(elem);
            else if (wait > 0)
                setTimeout(_hideElement.bind(null, elem), wait);
        },

        /**
         * Create a dialog box.
         * @param {string}   message - The message passed from the callee.
         * @param {string}   type - Type of dialog to create.
         * @param {Function} onOkay - [Optional] Callback function when clicked okay.
         * @param {Function} onCancel - [Optional] Callback function when cancelled.
         * @return {Object} Promise for the dialog.
         */
        dialog: function(message, type, onOkay, onCancel) {
            return this.setup({
                type: type,
                message: message,
                onOkay: onOkay,
                onCancel: onCancel
            });
        },

        /**
         * Show a new log message box.
         * @param {string} message - The message passed from the callee.
         * @param {string} type - [Optional] Optional type of log message.
         * @param {number} click - [Optional] Click event handler callback.
         */
        log: function(message, type, click) {
            var existing = $.getAll('.alertify-logs > div');
            if (existing) {
                var diff = existing.length - this.maxLogItems;
                if (diff >= 0)
                    for (var i = 0, _i = diff + 1; i < _i; i++)
                        this.close(existing[i], -1);
            }
            this.notify(message, type, click);
        },

        /**
         * Create the log container element.
         * @return {Node} HTML node to contain the log.
         */
        setupLogContainer: function() {
            var elLog = $.get('.alertify-logs');
            var className = this.logContainerClass;
            if (!elLog) {
                elLog = document.createElement('div');
                elLog.className = className;
                this.parent.appendChild(elLog);
            }

            // Make sure it's positioned properly.
            if (elLog.className !== className)
                elLog.className = className;

            return elLog;
        },

        /**
         * Add new log message.
         * If a type is passed, a class name "{type}" will get added.
         * This allows for custom look and feel for various types of notifications.
         * @param {string} message - The message passed from the callee.
         * @param {string} type - [Optional] Type of log message.
         * @param {number} click - [Optional] Click event handler callback.
         */
        notify: function(message, type, click) {
            var elLog = this.setupLogContainer();
            var log = document.createElement('div');
            log.className = (type || 'default');
            log.innerHTML = message;

            // Add the click handler, if specified.
            if ($.isFunction(click))
                $.on(log, 'click', click);

            elLog.appendChild(log);
            setTimeout(function() {
                $.addClass(log, 'show');
            }, 10);

            this.close(log, type === 'error' ? 0 : this.delay);
        },

        /**
         * Initiate all the required pieces for the dialog box.
         * @param {Object} item - Options for creating the dialog.
         * @returns {Promise} Promise to create the dialog.
         */
        setup: function(item) {
            var el = document.createElement('div');
            el.className = 'alertify d-none';
            el.innerHTML = this.build(item);

            var btnOK = $.get('.ok', el);
            var btnCancel = $.get('.cancel', el);
            var input = $.get('input', el);
            var label = $.get('label', el);
            var self = this;

            // Set default value/placeholder of input
            if (input) {
                // Set the label, if available, for MDL, etc.
                if (label)
                    label.textContent = '';
                else
                    input.placeholder = '';
                input.value = '';
            }

            /**
             * Create event handlers for a dialog.
             * @param {Object} resolve - Promise resolve function.
             */
            function setupHandlers(resolve) {
                if (!$.isFunction(resolve))
                    // promises are not available so resolve is a no-op
                    resolve = function() { };

                if (btnOK) {
                    $.on(btnOK, 'click', function(ev) {
                        if ($.isFunction(item.onOkay)) {
                            if (input)
                                item.onOkay(input.value, ev);
                            else
                                item.onOkay(ev);
                        }
                        if (input)
                            resolve({ buttonClicked: 'ok', inputValue: input.value, event: ev });
                        else
                            resolve({ buttonClicked: 'ok', event: ev });
                        _hideElement(el);
                        self.reset();
                    });
                }

                if (btnCancel) {
                    $.on(btnCancel, 'click', function(ev) {
                        if ($.isFunction(item.onCancel))
                            item.onCancel(ev);
                        resolve({ buttonClicked: 'cancel', event: ev });
                        _hideElement(el);
                        self.reset();
                    });
                }

                if (input)
                    $.on(input, 'keydown', function(ev) {
                        if (btnOK && ev.which === 13)
                            btnOK.click();
                    });

                $.on(window, 'keydown', function(ev) {
                    if (ev.which === 27)
                        if (btnCancel)
                            btnCancel.click();
                        else if (btnOK)
                            btnOK.click();
                });
            }

            var promise = new Promise(setupHandlers);

            this.parent.appendChild(el);
            setTimeout(function() {
                $.show(el);
                if (input && item.type && item.type === 'prompt') {
                    input.select();
                    input.focus();
                } else if (btnOK) {
                    btnOK.focus();
                }
            }, 100);

            return promise;
        },

        /**
         * Reset the dialog to default settings.
         */
        reset: function() {
            this.parent = document.body;
            this.okLabel = this.defaultOkLabel;
            this.cancelLabel = this.defaultCancelLabel;
        }
    };

    var Alertify = {
        alert: function(message, onOkay, onCancel) {
            return _alertify.dialog(message, 'alert', onOkay, onCancel) || this;
        },
        confirm: function(message, onOkay, onCancel) {
            return _alertify.dialog(message, 'confirm', onOkay, onCancel) || this;
        },
        prompt: function(message, onOkay, onCancel) {
            return _alertify.dialog(message, 'prompt', onOkay, onCancel) || this;
        },
        success: function(message, click) {
            _alertify.log(message, 'success', click);
            return this;
        },
        error: function(message, click) {
            _alertify.log(message, 'error', click);
            return this;
        },
        dismissAll: function() {
            _alertify.setupLogContainer().innerHTML = '';
            return this;
        },
        updateResources: function(okay, cancel) {
            okay = okay ? okay : _alertify.defaultOkLabel;
            cancel = cancel ? cancel : _alertify.defaultCancelLabel;
            _alertify.defaultOkLabel = okay;
            _alertify.okLabel = okay;
            _alertify.defaultCancelLabel = cancel;
            _alertify.cancelLabel = cancel;
        }
    };

    root.Alertify = Alertify;
}(this, this.$));
