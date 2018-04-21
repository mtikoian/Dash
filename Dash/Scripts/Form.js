/*!
 * Mithril based form component. For repetitive form elements.
 */
(function(root, factory) {
    root.Form = factory(root.m, root.$, root.Alertify, root.Help);
})(this, function(m, $, Alertify, Help) {
    'use strict';

    /**
     * Declare form class.
     * @param {Object} opts - Form settings
     * @param {Object[]} records - Array of objects the form will display/update.
     */
    function Form(opts, records) {
        opts = opts || {};
        this.opts = $.extend({
            container: null,
            id: null,
            columns: null,
            wantsHelp: false,
            appendRecord: false,
            resx: {
                deleteRecord: $.resx('delete'),
                confirmDelete: $.resx('confirmDelete'),
                confirmDeleteAll: $.resx('confirmDeleteAll'),
                areYouSure: $.resx('areYouSure'),
                moveUp: $.resx('moveUp'),
                moveDown: $.resx('moveDown'),
                help: $.resx('help')
            }
        }, opts);

        this.container = $.get(this.opts.container);
        this.records = $.isArray(records) ? records.map(function(x, i) { x._index = i; return x; }) : [];
        this.addedCount = 0;
        this.changed = false;
    }

    Form.prototype = {
        /**
         * Get the value of an element.
         * @param {*} e - Event that triggered the value change, or the new value.
         * @returns {string} Value of the element.
         */
        targetVal: function(e) {
            return e && e.target ? e.target.value : e;
        },

        /**
         * Set the value of a field on a record.
         * @param {number} index - Record index.
         * @param {string} field - Name of field to set.
         * @param {Event} e - Event that triggered the change.
         * @returns {bool} True if value changed, else false.
         */
        set: function(index, field, e) {
            var val = this.targetVal(e);
            if (this.opts.columns[field].type === 'int') {
                val = $.isNull(val) ? null : val * 1;
            }

            if ($.isFunction(this.opts.columns[field].setter)) {
                this.opts.columns[field].setter.call(this, index, field, e);
            } else {
                if (this.records[index][field] !== val) {
                    this.records[index][field] = val;
                    this.changed = true;
                    return true;
                }
            }
            return false;
        },

        addRecord: function() {
            --this.addedCount;
            var obj = $.clone(this.opts.newRecord);
            obj._index = this.addedCount;
            if (this.opts.appendRecord) {
                this.records.push(obj);
            } else {
                this.records.unshift(obj);
            }
            this.changed = true;
        },

        /**
         * Delete a record.
         * @param {number} index - Record index to delete.
         * @param {Event} e - Event that triggered the delete.
         */
        deleteRecord: function(index, e) {
            var self = this;
            if ($.isFunction(this.opts.deleteFn)) {
                this.opts.deleteFn.call(this, index, e);
            } else {
                // timeout prevents hitting enter on the button to trigger this confirmation from confirming it immediately
                setTimeout(function() {
                    Alertify.confirm(self.opts.resx.areYouSure, function(e) {
                        self.records.splice(index, 1);
                        if ($.isFunction(self.opts.afterDeleteFn)) {
                            self.opts.afterDeleteFn.call(self, index, e);
                        }
                        self.changed = true;
                        self.run();
                    });
                }, 100);
            }
        },

        deleteAllRecords: function() {
            var self = this;
            setTimeout(function() {
                Alertify.confirm(self.opts.resx.areYouSure, function() {
                    self.records = [];
                    self.changed = true;
                    self.run();
                });
            }, 100);
        },

        /**
         * Move a record up in the record list.
         * @param {number} index - Index of record to move up.
         */
        moveUp: function(index) {
            var t = this.records[index - 1];
            if (t) {
                this.records[index - 1] = this.records[index];
                this.records[index] = t;
            }
        },

        /**
         * Move a record down in the record list.
         * @param {number} index - Index of record to move down.
         */
        moveDown: function(index) {
            var t = this.records[index + 1];
            if (t) {
                this.records[index + 1] = this.records[index];
                this.records[index] = t;
            }
        },

        hasRecords: function() {
            return this.records && this.records.length > 0;
        },

        /**
         * Trigger the click event if the enter or space key is hit.
         * @param {Event} e - Event that triggered this.
         */
        keyInput: function(e) {
            if (e && e.keyCode && (e.keyCode === 13 || e.keyCode === 32)) {
                e.target.onclick.call(this, e);
            }
        },

        /**
         * Create the mithril node to show help wrapped around the provided content.
         * @param {string} helpBody - Content of the help dialog.
         * @param {Object} innerContent - Mithril node for the input that help is for.
         * @returns {Object} Mithril node for input-group if help is enabled, else innerContent.
         */
        withHelp: function(helpBody, innerContent) {
            return m(Help, { message: helpBody, enabled: this.opts.wantsHelp }, innerContent);
        },

        /**
         * Return the error class if the value isn't valid.
         * @param {string|number} value - Value to check against.
         * @param {bool} requirePositive - Require the value be a positive number.
         * @param {bool} isDate - Check if the value is a valid date.
         * @returns {string}  Error class name if value isn't set, else empty string.
         */
        withError: function(value, requirePositive, isDate) {
            requirePositive = $.coalesce(requirePositive, false);
            isDate = $.coalesce(isDate, false);
            var result = isDate ? $.fecha.parse(value, this.opts.dateFormat) : $.hasValue(value) && (!requirePositive || $.hasPositiveValue(value));
            return result ? '' : ' mform-control-error';
        },

        /**
         * Add a disabled attribute to the input attributes if isDisabled=true.
         * @param {Object} attrs - Attribute object to update.
         * @param {bool} isDisabled - Is the input diabled or not.
         * @returns {Object} Updated attribute object.
         */
        withDisabled: function(attrs, isDisabled) {
            if (isDisabled) {
                attrs.disabled = true;
            }
            return attrs;
        },

        /**
         * Add a checked attribute to the input attributes if isChecked=true.
         * @param {Object} attrs - Attribute object to update.
         * @param {bool} isChecked - Is the input checked or not.
         * @returns {Object} Updated attribute object.
         */
        withChecked: function(attrs, isChecked) {
            if (isChecked) {
                attrs.checked = true;
            }
            return attrs;
        },

        /**
         * Build list of options nodes.
         * @param {Object[]} arr - Array of objects to build options for.
         * @param {*} selectedValue - Value of the selected option. Can be a string, number, or array of either.
         * @param {string} valueKey - Object key that stores the value of the option.
         * @param {string} textKey - Object key that stores the text for the option.
         * @returns {Object[]} Array of mithril option nodes.
         */
        withOptions: function(arr, selectedValue, valueKey, textKey) {
            if (!$.isArray(arr)) {
                return null;
            }
            return arr.map(function(x) {
                var attr = { value: x[valueKey] === 0 ? '' : x[valueKey] };
                if (x[valueKey] === 0) {
                    attr.disabled = true;
                }
                if (x[valueKey] === selectedValue || ($.isArray(selectedValue) && selectedValue.indexOf(x[valueKey]) > -1)) {
                    attr.selected = true;
                }
                return m('option', attr, x[textKey]);
            });
        },

        /**
         * Create the mithril node for the record buttons (move up/move down/delete).
         * @param {number} index - Index of the record to create buttons for.
         * @param {bool} includeMove - Optionally include the move up/down buttons.
         * @returns {Object} Mithril node with buttons.
         */
        buttonView: function(index, includeMove) {
            var btns = [];
            if ($.coalesce(includeMove, false)) {
                btns.push(m('button.btn.btn-sm.btn-secondary', {
                    type: 'button', role: 'button', disabled: index < 1,
                    onclick: this.moveUp.bind(this, index), title: this.opts.resx.moveUp
                }, m('i.dash.dash-up-big.dash-lg.dash-fw'))
                );
                btns.push(m('button.btn.btn-sm.btn-secondary', {
                    type: 'button', role: 'button', disabled: index === this.records.length - 1,
                    onclick: this.moveDown.bind(this, index), title: this.opts.resx.moveDown
                }, m('i.dash.dash-down-big.dash-lg.dash-fw'))
                );
            }
            btns.push(m('button.btn.btn-sm.btn-secondary.confirm-delete-row-button', {
                type: 'button', role: 'button', onclick: this.deleteRecord.bind(this, index), title: this.opts.resx.deleteRecord
            }, m('i.dash.dash-trash.dash-lg.text-error')));
            return m('.btn-toolbar.float-right', btns);
        },

        destroy: function() {
            m.mount(this.container, null);
        },

        /**
         * Build the view that actually shows the form. This is meant to be overridden.
         * @returns {Object}  Mithril DIV node.
         */
        view: function() {
            return m('div');
        },

        /**
         * Render the view.
         */
        run: function() {
            var self = this;
            m.mount(self.container, {
                view: self.view.bind(self)
            });
        }
    };

    return Form;
});
