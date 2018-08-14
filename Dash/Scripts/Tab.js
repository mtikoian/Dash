/*!
 * Spectre tab component.
 */
(function(root, factory) {
    root.Tab = factory(root.$);
})(this, function($) {
    var Tab = function(element) {
        var tab = $.get(element);
        if (tab) {
            $.on($.get(element), 'click', this.action.bind(this), false);
            this.container = tab.parentNode.parentNode;
        }
    };

    Tab.prototype = {
        action: function(e) {
            if (!e) {
                return;
            }

            e.preventDefault();
            var target = e.target;
            if (!$.hasClass(target.parentNode, 'active')) {
                var content = this.getContent();
                $.removeClass($.getAll('a', this.getTab())[0], 'active');
                $.addClass(target, 'active');
                setTimeout(function() {
                    $.removeClass(content, 'active');
                    $.addClass($.get(target.getAttribute('href')), 'active');
                }, 100);
            }
        },

        getTab: function() {
            var activeTabs = $.getAll('.active', this.container);
            return activeTabs[activeTabs.length - 1].parentNode;
        },

        getContent: function() {
            var tab = this.getTab();
            var links = tab && $.getAll('a', tab);
            return links.length && $.get(links[0].getAttribute('href'));
        }
    };

    return Tab;
});
