/*!
 * ColorPicker mithril component.
 */
(function(root, factory) {
    root.ColorPicker = factory(root.m, root.$);
})(this, function(m, $) {
    'use strict';

    var _keys = { ESC: 27 };

    var ColorPicker = {
        oninit: function(vnode) {
            var attrs = vnode.attrs;
            this.value = attrs.value;
            if (this.value && this.value.substring(0, 1) !== '#') {
                this.value = '#' + this.value;
            }
            this.color = $.colors.hex2rgb(this.value);
            this.opts = {
                name: attrs.name,
                active: false,
                disabled: attrs.disabled,
                onSelect: attrs.onSelect,
                container: null
            };
        },

        oncreate: function(vnode) {
            this.opts.container = vnode.dom;
        },

        onupdate: function(vnode) {
            if (vnode.attrs) {
                this.opts.name = vnode.attrs.name;
            }
        },

        onKeyDown: function(e) {
            if (this.opts.disabled) {
                return;
            }

            if (this.opts.active && e.keyCode === _keys.ESC) {
                this.close();
                e.preventDefault();
                e.stopPropagation();
            }
        },

        close: function() {
            this.opts.active = false;
            var trigger = $.get('.colorpicker-trigger', this.opts.container);
            if (trigger) {
                trigger.focus();
            }
        },

        showEditor: function() {
            if (this.opts.disabled) {
                return;
            }
            this.opts.active = !this.opts.active;
        },

        setRed: function(value) {
            this.color.r = value * 1;
            this.setColor();
        },

        setGreen: function(value) {
            this.color.g = value * 1;
            this.setColor();
        },

        setBlue: function(value) {
            this.color.b = value * 1;
            this.setColor();
        },

        setColor: function() {
            this.value = $.colors.rgb2hex(this.color);
            if (this.opts.onSelect) {
                this.opts.onSelect(this.value);
            }
        },

        view: function() {
            return m('.mithril-colorpicker', {
                class: this.opts.active ? 'active' : '', onkeydown: this.onKeyDown.bind(this)
            }, [m('button.btn.btn-secondary.colorpicker-trigger', {
                type: 'button', role: 'button', disabled: this.opts.disabled,
                onclick: this.showEditor.bind(this),
                style: 'background-color: ' + this.value
            }, [m.trust('&nbsp;'), m('i.dash.colorpicker-indicator', { class: this.opts.active ? 'dash-sort-up' : 'dash-sort-down' })]),
            this.opts.active && m('.editor',
                m('div', [
                    m('input.range-red', { type: 'range', min: 0, max: 255, oninput: m.withAttr('value', this.setRed.bind(this)), value: this.color.r }),
                    m('input.range-green', { type: 'range', min: 0, max: 255, oninput: m.withAttr('value', this.setGreen.bind(this)), value: this.color.g }),
                    m('input.range-blue', { type: 'range', min: 0, max: 255, oninput: m.withAttr('value', this.setBlue.bind(this)), value: this.color.b })
                ])
            ),
            m('input', { type: 'hidden', name: this.opts.name, value: this.value })]);
        }
    };

    return ColorPicker;
});
