/*!
 * Help mithril component. Shows input groups with help button.
 */
(function(root, factory) {
    root.Help = factory(root.m, root.$, root.Alertify);
})(this, function(m, $, Alertify) {
    'use strict';

    var Help = {
        /**
         * Initialize the component.
         * @param {Object} vnode - Mithril virtual node
         */
        oninit: function(vnode) {
            var attrs = vnode.attrs;
            this.opts = {
                enabled: attrs.enabled,
                message: attrs.message
            };
        },

        /**
         * Show help dialog.
         * @param {Event} e - Event that triggered the dialog to open.
         */
        showHelp: function(e) {
            Alertify.alert(this.opts.message, $.dialogs.focusOnClose.bind(e), $.dialogs.focusOnClose.bind(e));
        },

        /**
         * Create HTML to display content with help add on.
         * @param {Object} vnode - Mithril virtual node
         * @returns {Object} Mithril vnode
         */
        view: function(vnode) {
            if (!this.opts.enabled) {
                return $.isArray(vnode.children) ? m('.input-group', vnode.children) : vnode.children;
            }
            if (!(vnode.children && vnode.children.length)) {
                return m('span', m('button.btn.btn-secondary.dash-context-help', {
                    type: 'button', role: 'button', onclick: this.showHelp.bind(this)
                }, m('i.dash.dash-help')));
            }
            return m('.input-group',
                vnode.children.concat(m('span.input-group-addon.input-group-custom', m('button.btn.btn-secondary.dash-context-help', {
                    type: 'button', role: 'button', onclick: this.showHelp.bind(this)
                }, m('i.dash.dash-help'))))
            );
        }
    };

    return Help;
});
