/*!
 * Native Javascript for Bootstrap 3
 * by dnp_theme
 * https://github.com/thednp/bootstrap.native
 *
 * Modified to use Dash's core library, and remove unneeded functionality.
 */
(function(root, factory) {
    // Assume a traditional browser.
    root.Dropdown = factory(root.$);
})(this, function($) {
    var _keys = {
        DOWN: 40,
        ENTER: 13,
        ESC: 27,
        TAB: 9,
        UP: 38,
        SPACE: 32
    };

    /**
     * Declare Dropdown class.
     * @param {Node} element - Node to trigger the dropdown.
     */
    var Dropdown = function(element) {
        this.menu = $.get(element);
        this.target = $.get('.dropdown-menu', element.parentNode);
        this.container = $.closest('.dropdown', this.target);
        this.items = $.getAll('.dropdown-item', this.target);
        this.selectedIndex = -1;
        this.init();
    };

    /**
     * Declare Dropdown class methods.
     */
    Dropdown.prototype = {
        /**
         * Initialize the dropdown.
         */
        init: function() {
            this.actions();
            this.menu.setAttribute('tabindex', '0'); // Fix onblur on Chrome | Safari
            $.on(this.container, 'click', this.handle, false);
            $.on(this.container, 'keydown', this.key.bind(this), false);

            // need this combination of blur and mousedown to imitate a click on the link, and still allow blur to close the dropdown
            $.on(this.container, 'blur', this.close.bind(this), true);
            var self = this;
            this.items.forEach(function(x, index) {
                $.on(x, 'mousedown', function(e) {
                    x.click();
                    e.stopPropagation();
                });
                $.on(x, 'mouseover', function() {
                    self.selectIndex(index);
                });
            });
        },

        /**
         * Closure for class methods.
         */
        actions: function() {
            var self = this;

            /**
             * Trigger the dropdown event.
             * @param {Event} e - Event that requested the dropdown.
             */
            this.handle = function(e) {
                var target = e.target || e.currentTarget;
                if (target.nodeName === 'I' && (target.parentNode.nodeName === 'BUTTON' || target.parentNode.nodeName === 'A')) {
                    target = target.parentNode;
                }
                if (target === self.menu || target === $.get('span', self.menu)) {
                    self.toggle();
                } else {
                    self.close();
                }
                /#$/g.test(target.href) && e.preventDefault();
            };

            /**
             * Show/hide the dropdown content.
             */
            this.toggle = function() {
                if ($.hasClass(this.target, 'show')) {
                    this.close();
                } else {
                    $.addClass(this.target, 'show');
                    this.menu.setAttribute('aria-expanded', true);
                }
            };

            /**
             * Close dropdown on escape key.
             * @param {Event} e - Keydown event
             */
            this.key = function(e) {
                if ($.hasClass(this.target, 'show')) {
                    if (e.which === _keys.ESC) {
                        self.toggle();
                        e.preventDefault();
                        e.stopPropagation();
                    }
                    if (e.which === _keys.ENTER || e.which === _keys.SPACE) {
                        if (this.selectedIndex > -1) {
                            this.items[this.selectedIndex].click();
                        }
                        self.toggle();
                        e.preventDefault();
                        e.stopPropagation();
                    }
                    if (e.which === _keys.DOWN) {
                        this.selectIndex(Math.min(this.selectedIndex + 1, this.items.length - 1));
                        e.preventDefault();
                        e.stopPropagation();
                    }
                    if (e.which === _keys.UP) {
                        this.selectIndex(Math.max(this.selectedIndex - 1, 0));
                        e.preventDefault();
                        e.stopPropagation();
                    }
                } else if (e.which === _keys.ENTER || e.which === _keys.SPACE) {
                    self.toggle();
                    e.preventDefault();
                    e.stopPropagation();
                }
            };

            /**
             * Select an item from the list by index.
             * @param {number} index - Filtered list item index.
             */
            this.selectIndex = function(index) {
                this.selectedIndex = index;
                this.items.forEach(function(x) {
                    $.removeClass(x, 'active');
                });
                $.addClass(this.items[this.selectedIndex], 'active');
            };

            /**
             * Close the dropdown.
             */
            this.close = function() {
                this.selectedIndex = -1;
                this.items.forEach(function(x) {
                    $.removeClass(x, 'active');
                });
                setTimeout(function() {
                    $.removeClass(self.target, 'show');
                    self.menu.setAttribute('aria-expanded', false);
                }, 0);
            };
        }
    };

    return Dropdown;
});

(function(root, factory) {
    // Assume a traditional browser.
    root.Tab = factory(root.$);
})(this, function($) {
    /**
     * Declare Tab class.
     * @param {Node} element - Node to trigger the tab.
     */
    var Tab = function(element) {
        this.tab = $.get(element);
        this.tabs = this.tab.parentNode.parentNode;
        this.dropdown = $.get('.dropdown', this.tabs);
        if ($.hasClass(this.tabs, 'dropdown-menu')) {
            this.dropdown = this.tabs.parentNode;
            this.tabs = this.tabs.parentNode.parentNode;
        }
        this.duration = 100;
        this.init();
    };

    /**
     * Declare Tab class methods.
     */
    Tab.prototype = {
        init: function() {
            this.actions();
            $.on(this.tab, 'click', this.action, false);
        },

        /**
         * Closure for class methods.
         */
        actions: function() {
            var self = this;

            /**
             * Display content of a tab.
             * @param {Event} e - Event that triggered the tab change.
             */
            this.action = function(e) {
                e = e || window.e; e.preventDefault();
                var next = e.target; //the tab we clicked is now the next tab
                var nextContent = $.get(next.getAttribute('href')); //this is the actual object, the next tab content to activate

                // get current active tab and content
                var activeTab = self.getActiveTab();
                var activeContent = self.getActiveContent();

                if (!$.hasClass(next.parentNode, 'active')) {
                    // toggle "active" class name
                    $.removeClass($.getAll('a', activeTab)[0], 'active');
                    $.addClass(next, 'active');

                    // handle dropdown menu "active" class name
                    if (self.dropdown) {
                        if (!$.hasClass(self.tab.parentNode.parentNode, 'dropdown-menu')) {
                            $.removeClass(self.dropdown, 'active');
                        } else {
                            $.addClass(self.dropdown, 'active');
                        }
                    }

                    //1. hide current active content first
                    $.removeClass(activeContent, 'show');

                    setTimeout(function() {
                        //2. toggle current active content from view
                        $.removeClass(activeContent, 'active');
                        $.addClass(nextContent, 'active');
                    }, self.duration);
                    setTimeout(function() {
                        //3. show next active content
                        $.addClass(nextContent, 'show');
                    }, self.duration * 2);
                }
            },

                /**
                 * Gets the currently active tab.
                 * @returns {Node} Active tab element.
                 */
                this.getActiveTab = function() {
                    var activeTabs = $.getAll('.active', this.tabs);
                    if (activeTabs.length === 1 && !$.hasClass(activeTabs[0], 'dropdown')) {
                        return activeTabs[0].parentNode;
                    } else if (activeTabs.length > 1) {
                        return activeTabs[activeTabs.length - 1].parentNode;
                    }
                },

                /**
                 * Get the currently active tab content.
                 * @returns {Node} Active content element
                 */
                this.getActiveContent = function() {
                    var a = this.getActiveTab();
                    var b = a && $.getAll('a', a);
                    return b.length && $.get(b[0].getAttribute('href'));
                };
        }
    };

    return Tab;
});
