/*!
 * Allowing lists to dynamically expand and collapse.
 * Created by Stephen Morley - http://code.stephenmorley.org/ - and released under the terms of the CC0 1.0 Universal legal code:
 * http://creativecommons.org/publicdomain/zero/1.0/legalcode
 *
 * Modified to work with bootstrap/fontAwesome and incorporate cascading checkboxes.
 */
(function(root, factory) {
    // Assume a traditional browser.
    window.CollapsibleList = factory(root.$);
})(this, function($) {
    'use strict';

    /**
     * Declare CollapsibleList class.
     * @param {Node} element - Node to make collapsible.
     */
    var CollapsibleList = function(element) {
        this.container = $.get(element);
        this.init();
    };

    /**
     * Declare CollapseCollapsibleList class methods.
     */
    CollapsibleList.prototype = {
        /**
         * Make the specified list collapsible.
         */
        init: function() {
            var node = this.container;
            // loop over the list items within this node
            var lis = $.getAll('li', node);
            for (var i = 0; i < lis.length; i++) {
                // prevent text from being selected unintentionally
                $.on(lis[i], 'mousedown', function(e) { e.preventDefault(); }, false);
                // add the click listener
                $.on(lis[i], 'click', this.createClickListener(lis[i]), false);
                // close the unordered lists within this list item
                this.toggle(lis[i]);

                // check any parent items if this item is checked
                node = $.get('input[type="checkbox"]', lis[i]);
                if (node.checked) {
                    var p = node;
                    while (this.container !== (p = p.parentNode)) {
                        if (p.nodeName === 'LI') {
                            var parent = p.parentNode.parentNode;
                            if (parent.nodeName === 'LI') {
                                var pi = $.get('input[type="checkbox"]', parent);
                                if (pi && pi !== node) {
                                    pi.checked = true;
                                }
                                this.toggle(parent, true);
                            }
                        }
                    }
                }
            }
        },

        /**
         * Toggles the display status of any unordered list elements within the specified node.
         * @param {Node} node - Node containing the unordered list elements.
         * @returns {Function} Click handler function.
         */
        createClickListener: function(node) {
            var self = this;
            return function(e) {
                // ensure the event object is defined
                e = $.coalesce(e, window.event);

                // find the list item containing the target of the event
                var elem = $.coalesce(e.target, e.srcElement);

                // handle checking/unchecking buttons
                if (elem.nodeName === 'INPUT') {
                    var checked = elem.checked;

                    // first toggle the check for all of the children
                    var inputs = $.getAll('ul input[type="checkbox"]', elem.parentNode.parentNode);
                    i = inputs.length;
                    while (i--) {
                        inputs[i].checked = checked;
                    }

                    // then check all parents. if any of the children of a checkbox are checked, then it should be checked.
                    var checkedRelative = false;
                    var p = elem;
                    while (self.container !== (p = p.parentNode)) {
                        if (p.nodeName === 'LI') {
                            // see if anybody else on this node level is checked
                            var relatives = $.getAll('input[type="checkbox"]', p.parentNode);
                            var i = relatives.length;
                            checkedRelative = false;
                            while (i--) {
                                if (relatives[i].checked) {
                                    checkedRelative = true;
                                }
                            }

                            if (p.parentNode.parentNode !== self.container) {
                                var pi = $.get('input[type="checkbox"]', p.parentNode.parentNode);
                                if (pi && pi !== elem) {
                                    pi.checked = checkedRelative || checked;
                                }
                            }
                        }
                    }
                    return;
                }

                if ($.hasClass(elem, 'custom-checkbox') || (elem.parentNode && $.hasClass(elem.parentNode, 'custom-checkbox'))) {
                    // prevent toggling tree when checking/unchecking an element
                    return;
                }

                // now handle the tree itself
                while (elem.nodeName !== 'LI') {
                    elem = elem.parentNode;
                }

                // toggle the state of the node if it was the target of the event
                if (elem === node) {
                    self.toggle(node);
                }
            };
        },

        /**
         * Opens or closes the unordered list elements directly within the specified node.
         * @param {Node} node - Node containing the unordered list elements.
         * @param {bool} forceOpen - Set to true to force the node to be open regardless of current status.
         */
        toggle: function(node, forceOpen) {
            // determine whether to open or close the unordered lists
            var open = $.coalesce(forceOpen, $.hasClass(node, 'collapsible-list-closed'));

            // loop over the unordered list elements with the node
            var uls = $.getAll('ul', node);
            for (var i = 0; i < uls.length; i++) {
                // find the parent list item of this unordered list
                var li = uls[i];
                while (li.nodeName !== 'LI') {
                    li = li.parentNode;
                }

                // style the unordered list if it is directly within this node
                if (li === node) {
                    uls[i].style.display = open ? 'block' : 'none';
                }
            }

            // remove the current class from the node
            $.removeClass(node, 'collapsible-list-' + (open ? 'closed' : 'open'));

            // if the node contains unordered lists, set its class
            if (uls.length) {
                $.addClass(node, 'collapsible-list-' + (open ? 'open' : 'closed'));
            }
        }
    };

    return CollapsibleList;
});
