/*!
 * Autocomplete mithril component.
 */
(function(root, factory) {
    root.Autocomplete = factory(root.m, root.$);
})(this, function(m, $) {
    'use strict';

    var _keys = {
        DOWN: 40,
        ENTER: 13,
        ESC: 27,
        TAB: 9,
        UP: 38
    };

    /**
     * Escape a string for use with a regex.
     * @param {string} s - String to escape.
     * @returns Escaped string.
     */
    var regExpEscape = function(s) {
        return s.replace(/[-\\^$*+?.()|[\]{}]/g, '\\$&');
    };

    var Autocomplete = {
        /**
         * Initialize the component.
         * @param {Object} vnode - Mithril virtual node
         */
        oninit: function(vnode) {
            var attrs = vnode.attrs;
            this.opts = {
                value: attrs.value,
                valueChanged: false,
                name: attrs.name,
                placeholder: attrs.placeholder,
                active: false,
                required: attrs.required,
                disabled: attrs.disabled,
                onSelect: attrs.onSelect,
                onCancel: attrs.onCancel,
                class: attrs.class,
                list: attrs.list || [],
                filteredList: [],
                container: null,
                selectedIndex: -1
            };
        },

        /**
         * Update the opts in case the component name is changed.
         * @param {Object} vnode - Mithril virtual node
         * @param vnode
         */
        onupdate: function(vnode) {
            if (vnode.attrs) {
                this.opts.name = vnode.attrs.name;
            }
        },

        /**
         * Grab the DOM containing node after it is created.
         * @param {Object} vnode - Mithril virtual node
         */
        oncreate: function(vnode) {
            this.opts.container = vnode.dom;
            this.opts.container.autocomplete = this;
        },

        /**
         * Update the list of items.
         * @param {string[]} list - Array of strings to show.
         */
        setList: function(list) {
            this.close();
            this.opts.list = list;
            this.opts.filteredList = [];
        },

        /**
         * Check special keys and respond accordingly.
         * @param {Event} e - Keypress event.
         */
        onKeyDown: function(e) {
            if (this.opts.disabled) {
                return;
            }

            if (this.opts.active) {
                if (e.keyCode === _keys.ENTER) {
                    var selected = this.opts.filteredList[this.opts.selectedIndex];
                    if (selected) {
                        this.selectItem(selected);
                    } else {
                        this.cancel(true);
                    }
                    e.preventDefault();
                    e.stopPropagation();
                } else if (e.keyCode === _keys.ESC) {
                    this.cancel(true);
                    e.preventDefault();
                    e.stopPropagation();
                } else if (e.keyCode === _keys.DOWN) {
                    this.selectIndex(Math.min(this.opts.selectedIndex + 1, this.opts.filteredList.length - 1));
                } else if (e.keyCode === _keys.UP) {
                    this.selectIndex(Math.max(this.opts.selectedIndex - 1, 0));
                }
            }
        },

        /**
         * Handle the input's value changing and show the list.
         * @param {Event} e - Event that triggered the change.
         */
        onInput: function(e) {
            if (this.opts.value !== e.target.value) {
                this.opts.valueChanged = true;
                this.opts.value = e.target.value;
            }
            if (this.opts.value.length > 1) {
                this.opts.active = true;
                this.opts.selectedIndex = 0;
                var val = this.opts.value.toLowerCase();
                this.opts.filteredList = ($.isFunction(this.opts.list) ? this.opts.list() : this.opts.list).filter(function(x) {
                    return x.toLowerCase().indexOf(val) > -1;
                });
            } else {
                this.opts.filteredList = [];
            }
        },

        /**
         * Close autocomplete dropdown on lost focus.
         */
        onBlur: function() {
            if (this.opts.active || this.opts.valueChanged) {
                this.cancel(false);
            }
        },

        /**
         * Select an item from the list by index.
         * @param {number} index - Filtered list item index.
         */
        selectIndex: function(index) {
            this.opts.selectedIndex = index;
            var ul = $.get('ul', this.opts.container);
            if (ul) {
                ul.children[index].scrollIntoView();
            }
        },

        /**
         * Select an item by text value.
         * @param {string} item - Text to select.
         * @param {Event} e - Event that triggered this.
         */
        selectItem: function(item, e, focus) {
            if ($.isFunction(this.opts.onSelect)) {
                this.opts.onSelect.call(null, item);
            }
            this.opts.value = item;
            this.close($.isNull(focus) ? true : focus);
            if (e && e.preventDefault) {
                e.preventDefault();
            }
        },

        /**
         * Discard value and close the autocomplete.
         * @param {bool} focus - Focus on the original control after closing if true.
         */
        cancel: function(focus) {
            if (this.opts.valueChanged) {
                this.opts.value = '';
            }
            if ($.isFunction(this.opts.onCancel)) {
                this.opts.onCancel();
            }
            this.close(focus);
        },

        /**
         * Set value and close the autocomplete.
         * @param {bool} focus - Focus on the original control after closing if true.
         */
        close: function(focus) {
            this.opts.active = false;
            this.opts.selectedIndex = -1;
            this.opts.valueChanged = false;
            if (focus) {
                var input = $.get('input', this.opts.container);
                if (input) {
                    input.focus();
                }
            }
        },

        /**
         * Highlight val inside item.
         * @param {string} text - Text to highlight in.
         * @param {string} val - Substring to highlight.
         * @returns Marked up string.
         */
        highlightItem: function(text, val) {
            return val === '' ? text : text.replace(RegExp(regExpEscape(val.trim()), 'gi'), '<mark>$&</mark>');
        },

        /**
         * Create HTML to display component.
         * @returns {Object} Mithril vnode
         */
        view: function() {
            var self = this;
            return m('.mithril-autocomplete-container.autocomplete', { class: self.opts.active ? 'autocomplete-active' : '' },
                m('input.form-control', {
                    type: 'text',
                    name: self.opts.name,
                    placeholder: self.opts.placeholder,
                    class: (self.opts.class || '') + (self.opts.required && !$.hasValue(self.opts.value) ? ' mform-control-error' : ''),
                    autocomplete: 'off',
                    'aria-autocomplete': 'list',
                    onkeydown: self.onKeyDown.bind(self),
                    oninput: self.onInput.bind(self),
                    value: self.opts.value,
                    onblur: self.onBlur.bind(self)
                }),
                self.opts.active && m('ul', self.opts.filteredList.map(function(x, index) {
                    return m('li', {
                        'aria-selected': self.opts.selectedIndex == index,
                        onmousedown: self.selectItem.bind(self, x)
                    }, m.trust(self.highlightItem(x, self.opts.value)));
                }))
            );
        }
    };

    return Autocomplete;
});
