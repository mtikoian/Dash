/*!
 * Native Javascript for Bootstrap 3
 * by dnp_theme
 * https://github.com/thednp/bootstrap.native
 *
 * Modified to use Dash's core library, and remove unneeded functionality.
 */
(function(root, factory) {
    root.Tab = factory(root.$);
})(this, function($) {
    /**
     * Declare Tab class.
     * @param {Node} element - Node to trigger the tab.
     */
    var Tab = function(element) {
        this.tab = $.get(element);
        this.tabs = this.tab.parentNode.parentNode;
        this.duration = 100;
        this.init();
    };

    Tab.prototype = {
        init: function() {
            this.actions();
            $.on(this.tab, 'click', this.action, false);
        },

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
            };

            /**
            * Gets the currently active tab.
            * @returns {Node} Active tab element.
            */
            this.getActiveTab = function() {
                var activeTabs = $.getAll('.active', this.tabs);
                if (activeTabs.length === 1) {
                    return activeTabs[0].parentNode;
                } else if (activeTabs.length > 1) {
                    return activeTabs[activeTabs.length - 1].parentNode;
                }
            };

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
