/*!
 * ColorPicker mithril component.
 */
(function(root, factory) {
    root.ColorPicker = factory(root.m, root.$);
})(this, function(m, $) {
    'use strict';

    var _keys = {
        ESC: 27,
        DOWN: 40,
        UP: 38,
        LEFT: 37,
        RIGHT: 39
    };

    var ColorPicker = {
        /**
         * Initialize the component.
         * @param {Object} vnode - Mithril virtual node
         */
        oninit: function(vnode) {
            var attrs = vnode.attrs;
            this.opts = {
                value: attrs.value,
                name: attrs.name,
                active: false,
                disabled: attrs.disabled,
                onSelect: attrs.onSelect,
                onCancel: attrs.onCancel,
                container: null,
                selectedIndex: -1,
                colors: ['#4D4D4D', '#F44E3B', '#0000FF', '#FE9200', '#FCDC00', '#00FF00', '#A4DD00', '#68CCCA',
                    '#73D8FF', '#AEA1FF', '#FDA1FF', '#D33115', '#E27300', '#FCC400', '#B0BC00', '#68BC00',
                    '#16A5A5', '#009CE0', '#7B64FF', '#FA28FF', '#9F0500', '#C45100', '#FB9E00',
                    '#808900', '#194D33', '#0C797D', '#0062B1', '#653294', '#AB149E'
                ]
            };
            if (this.opts.value) {
                this.opts.selectedIndex = this.opts.colors.indexOf(this.opts.value);
            }
        },

        /**
         * Grab the DOM containing node after it is created.
         * @param {Object} vnode - Mithril virtual node
         */
        oncreate: function(vnode) {
            this.opts.container = vnode.dom;
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
         * Check special keys and respond accordingly.
         * @param {Event} e - Keypress event.
         */
        onKeyDown: function(e) {
            if (this.opts.disabled) {
                return;
            }

            if (this.opts.active) {
                if (e.keyCode === _keys.ESC) {
                    this.close();
                    e.preventDefault();
                    e.stopPropagation();
                } else if (e.keyCode === _keys.DOWN || e.keyCode === _keys.LEFT) {
                    this.selectIndex(Math.min(this.opts.selectedIndex + 1, this.opts.colors.length - 1));
                } else if (e.keyCode === _keys.UP || e.keyCode === _keys.RIGHT) {
                    this.selectIndex(Math.max(this.opts.selectedIndex - 1, 0));
                }
            }
        },

        /**
         * Select an item from the list by index.
         * @param {Event} e - Event that triggered this
         * @param {Number} index - Index of the color being selected
         */
        selectColor: function(e, index) {
            var target = e && e.target ? e.target : e;
            if (target) {
                this.opts.value = target.getAttribute('data-value') || target.parentNode.getAttribute('data-value');
                target.scrollIntoView();

                if (this.opts.onSelect) {
                    this.opts.onSelect(this.opts.value);
                }
            }
            if (index) {
                this.opts.selectedIndex = index;
            }
        },


        /**
         * Select an item from the list by index.
         * @param {number} index - Filtered list item index.
         */
        selectIndex: function(index) {
            var swatches = $.getAll('.swatch', this.opts.container);
            if (swatches && swatches[index]) {
                this.opts.selectedIndex = index;
                swatches[index].focus();
                if ($.hasClass(swatches[index], 'swatch-selectable')) {
                    this.selectColor(swatches[index], index);
                }
            }
        },

        /**
         * Set value and close the picker.
         */
        close: function() {
            this.opts.active = false;
            var trigger = $.get('.colorpicker-trigger', this.opts.container);
            if (trigger) {
                trigger.focus();
            }
        },

        /**
         * Show the swatch list.
         */
        showEditor: function() {
            if (this.opts.disabled) {
                return;
            }
            this.opts.active = !this.opts.active;
        },

        /**
         * Create HTML to display component.
         * @returns {Object} Mithril vnode
         */
        view: function() {
            var self = this;
            return m('.mithril-colorpicker', {
                class: this.opts.active ? 'active' : '', onkeydown: this.onKeyDown.bind(this)
            }, m('button.btn.btn-secondary.colorpicker-trigger', {
                type: 'button', role: 'button', disabled: this.opts.disabled,
                onclick: this.showEditor.bind(this),
                style: 'background-color: ' + this.opts.value
                }, [m.trust('&nbsp;'), m('i.dash.colorpicker-indicator', { class: this.opts.active ? 'dash-sort-up' : 'dash-sort-down' }) ]),
            this.opts.active && m('.editor', [
                m('.swatches',
                    m('button.btn.btn-secondary.swatch', {
                        type: 'button', role: 'button', onclick: this.close.bind(this)
                    }, m('i.dash.dash-cancel')),
                    this.opts.colors.map(function(x) {
                        return m('button.btn.btn-secondary.swatch.swatch-selectable', {
                            class: self.opts.value === x ? 'active' : '',
                            type: 'button', role: 'button', 'data-value': x,
                            style: 'background-color: ' + x + '; color: ' + x, onclick: self.selectColor.bind(self)
                        }, m('i.dash.dash-cancel'));
                    })
                )
            ]),
            m('input', { type: 'hidden', name: this.opts.name, value: this.opts.value }));
        }
    };

    return ColorPicker;
});