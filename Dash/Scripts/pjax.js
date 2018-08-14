/*!
 * PJAX library.
 */
(function(root, $, Alertify) {
    'use strict';

    var pjax = {
        firstRun: true,
        ignoreFileTypes: ['pdf', 'doc', 'docx', 'zip', 'rar', '7z', 'gif', 'jpeg', 'jpg', 'png'],
        excludeClass: 'pjax-no-follow',
        container: '#contentWrapper'
    };

    /**
     * Updates DOM with new content.
     * @param {string} html - DOM fragment of loaded container.
     * @param {Object} options - Configuration options.
     * return {Object} Updated options.
     */
    pjax.updateContent = function(html, options) {
        var newNode = $.createNode(html);
        var title = $.getAll('[data-pjax-title]', newNode, true);
        if (title.length) {
            options.title = title[0].getAttribute('data-pjax-title');
        }
        var url = $.getAll('[data-pjax-url]', newNode, true);
        if (url.length) {
            options.url = url[0].getAttribute('data-pjax-url');
        }
        var container = $.getAll('[data-pjax-container]', newNode, true);
        if (container.length) {
            options.container = container[0].getAttribute('data-pjax-container');
        }

        // Get container node
        var node = $.get(options.container || pjax.container);
        // Update the DOM with the new content
        node.innerHTML = html;

        return options;
    };

    /**
     * Fire load events after loading content.
     * @param {Object} options - Configuration options.
     */
    pjax.onLoad = function(options) {
        var node = $.get(options.container);
        $.content.load(node);
        pjax.checkEvents(node, 'data-load-event');

        if (!$.isNull(options.title)) {
            // Set page title
            document.title = options.title;
        }
        if (options.returnToTop) {
            // Scroll to top of page
            window.scrollTo(0, 0);
        }
    };

    /**
     * Fire unload events before unloading content.
     * @param {Object} options - Configuration options.
     */
    pjax.onUnload = function(options) {
        var node = $.get(options.container);
        pjax.checkEvents(node, 'data-unload-event');
        $.content.unload(node);
    };

    /**
     * Update browser history.
     * @param {Object} options - Configuration options.
     */
    pjax.updateHistory = function(options) {
        if (options.history) {
            // If this is the first time pjax has run, create a state object for the current page.
            if (pjax.firstRun) {
                window.history.replaceState({ url: document.location.href, container: '#' + options.container.id, title: document.title, method: options.method }, document.title);
                pjax.firstRun = false;
            }
            // Update browser history
            window.history.pushState({ url: options.url, container: '#' + options.container.id, title: options.title, method: options.method }, options.title, options.url);
        }
    };

    /**
     * Handle requests to load content.
     * @param {Object} options - Configuration options.
     */
    pjax.handle = function(options) {
        pjax.request(options, function(html) {
            if (html === false) {
                $.content.done();
                return;
            }

            pjax.onUnload(options);
            options = pjax.updateContent(html, options);
            pjax.updateHistory(options);
            pjax.onLoad(options);
        });
    };

    /**
     * Performs AJAX request to page and returns the result..
     * @param {Object} options - Configuration options.
     * @param {Function} callback - Method to call when a page is loaded.
     */
    pjax.request = function(options, callback) {
        $.content.loading();
        // Create xmlHttpRequest object.
        // @todo consider converting to fetch - see https://github.com/developit/unfetch polyfill
        var xhr = new XMLHttpRequest();
        // Add state listener.
        xhr.onreadystatechange = function() {
            if ((xhr.readyState === 4) && (xhr.status === 200)) {
                // Success, Return HTML
                callback(xhr.responseText);
                $.content.done();
            } else if ((xhr.readyState === 4) && (xhr.status === 404 || xhr.status === 500)) {
                // error (return false)
                callback(false);
                $.content.done();
            }
            // @todo possible bug here where readystate doesn't match either?
        };
        // Secret pjax ?get param so browser doesn't return pjax content from cache when we don't want it to
        // Switch between ? and & so as not to break any URL params (Based on change by zmasek https://github.com/zmasek/)
        xhr.open(options.method || 'GET', options.url + ((!/[?&]/.test(options.url)) ? '?_pjax' : '&_pjax'), true);
        // Add headers so things can tell the request is being performed via AJAX.
        xhr.setRequestHeader('X-PJAX', 'true'); // PJAX header
        xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');// Standard AJAX header.

        xhr.send(null);
    };

    /**
     * Performs AJAX form submission request to page and returns the result.
     * @param form. Form to submit.
     * @param callback. Method to call when a page is loaded.
     */
    pjax.submit = function(form, callback) {
        $.content.loading();
        // Create xmlHttpRequest object.
        var xhr = new XMLHttpRequest();
        // Add state listener.
        xhr.onreadystatechange = function() {
            if ((xhr.readyState === 4) && (xhr.status === 200)) {
                // Success, Return HTML
                callback(xhr.responseText);
                $.content.done();
            } else if ((xhr.readyState === 4) && (xhr.status === 404 || xhr.status === 500)) {
                // error (return false)
                callback(false);
                $.content.done();
            }
        };
        // Secret pjax ?get param so browser doesn't return pjax content from cache when we don't want it to
        // Switch between ? and & so as not to break any URL params (Based on change by zmasek https://github.com/zmasek/)
        xhr.open(form.hasAttribute('data-method') ? form.getAttribute('data-method') : 'POST', form.getAttribute('action'), true);
        // Add headers so things can tell the request is being performed via AJAX.
        xhr.setRequestHeader('X-PJAX', 'true'); // PJAX header
        xhr.setRequestHeader('X-Requested-With', 'XMLHttpRequest');// Standard AJAX header.
        xhr.send(new FormData(form));
    };

    /**
     * Validate and correct options object.
     * @param {Object} options - Configuration options.
     * @return {Object|bool} Valid options object or false.
     */
    pjax.parseOptions = function(options, target) {
        options = options || {};
        if ($.isNull(options.url)) {
            return false;
        }

        if (target) {
            // If data-method is specified, use as request method
            if (target.getAttribute('data-method')) {
                options.method = target.getAttribute('data-method');
            }

            // If data-pjax is specified, use as container
            if (target.getAttribute('data-pjax')) {
                options.container = target.getAttribute('data-pjax');
            }

            // If data-pjax-title is specified, use as title
            if (target.getAttribute('data-pjax-title')) {
                options.title = target.getAttribute('data-pjax-title');
            }
        }

        // Use default options if not defined
        options = $.extend(options, {
            method: 'GET',
            container: pjax.container,
            history: true,
            returnToTop: true
        });

        // Ensure history is a boolean.
        options.history = (options.history === false) ? false : true;

        // Return valid options
        return options;
    };

    /**
     * Search node for event attributes and dispatch them.
     * @param {Node} node - DOM node to search in.
     * @param {string} eventName - Name of event attribute to search for.
     */
    pjax.checkEvents = function(node, eventName) {
        $.getAll('[' + eventName + ']', node, node !== document).forEach(function(x) {
            var ev = x.getAttribute(eventName);
            if ($.events.hasOwnProperty(ev)) {
                $.dispatch(x, $.events[ev]);
            }
        });
    };

    /**
     * Configure pjax.
     * @param {Object} options - Configuration options.
     */
    pjax.init = function(options) {
        // Delete history and title if provided. These options should only be provided via invoke();
        delete options.title;
        delete options.history;

        pjax.options = options;
        var body = $.get('body');
        $.content.load(body);
        pjax.checkEvents(body, 'data-load-event');
    };

    /**
     * Directly invoke a pjax page load.
     * @param {Object} options - Configuration options.
     */
    pjax.invoke = function(options) {
        // Process options
        options = pjax.parseOptions(options);
        // If everything went okay, activate pjax.
        if (options !== false) {
            pjax.handle(options);
        }
    };

    /**
     * popstate listener
     * Listens for back/forward button events and updates page accordingly.
     */
    $.on(window, 'popstate', function(e) {
        if (e.state !== null) {
            var opt = $.coalesce({
                url: e.state.url,
                container: e.state.container,
                title: e.state.title,
                method: e.state.method,
                history: false
            }, pjax.options);

            // Convert state data to PJAX options
            var options = pjax.parseOptions(opt);
            // If something went wrong, return.
            if (options === false) return;
            // If there is a state object, handle it as a page load.
            pjax.handle(options);
        }
    });

    /**
     * Link click listener.
     */
    $.on(document, 'click', function(event) {
        var target = event.target || event.srcElement;
        if (target.nodeName !== 'A') {
            target = $.closest('a', target);
        }
        if (!(target && target.nodeName === 'A' && !(pjax.excludeClass && $.hasClass(target, pjax.excludeClass)))) {
            // Ignore clicks unless its a link that doesn't have the exclude class
            return;
        }
        if (target.protocol !== document.location.protocol || target.host !== document.location.host) {
            // Ignore external links
            return;
        }
        if (target.pathname === location.pathname && target.hash.length > 0) {
            // Ignore anchors on the same page
            return;
        }
        if (pjax.ignoreFileTypes.indexOf(target.pathname.split('.').pop().toLowerCase()) !== -1) {
            // Skip link if file type is within ignored types array
            return;
        }
        if (event.which > 1 || event.metaKey || event.ctrlKey) {
            // Allow middle click (pages in new windows)
            return;
        }

        // Don't fire normal event
        event.preventDefault();

        var options = { url: target.href };
        if (document.location.href === options.url && !target.hasAttribute('data-reload')) {
            // Take no action if we are already on said page and reload isn't allowed
            return;
        }

        // Check options are valid.
        options = pjax.parseOptions(options, target);
        if (options === false) {
            return;
        }

        // Check for confirmation or form changes before loading content.
        if (target.getAttribute('data-confirm')) {
            Alertify.dismissAll();
            Alertify.confirm(target.getAttribute('data-confirm'), pjax.handle.bind(null, options), function(e) { e.target.focus(); });
        } else {
            var form = $.get('form.has-changes');
            if (form) {
                Alertify.confirm(form.getAttribute('data-confirm'), pjax.handle.bind(null, options), function(e) { e.target.focus(); });
            } else {
                pjax.handle(options);
            }
        }
    });

    /**
     * Form change listener.
     */
    $.on(document, 'change', function(event) {
        var target = event.target || event.srcElement;
        if (['INPUT', 'SELECT', 'TEXTAREA'].indexOf(target.nodeName) === -1) {
            // Ignore change unless its form input
            return;
        }

        var form = $.closest('FORM', target);
        if (form) {
            $.addClass(form, 'has-changes');
        }
    });

    /**
     * Form submit listener.
     */
    $.on(document, 'submit', function(event) {
        var target = event.target || event.srcElement;
        if (target.nodeName !== 'FORM') {
            target = $.closest('FORM', target);
        }
        if (!(target && target.nodeName === 'FORM' && !(pjax.excludeClass && $.hasClass(target, pjax.excludeClass)))) {
            // Ignore submit unless its a form that doesn't have the exclude class
            return;
        }

        var actionNode = document.createElement('a');
        actionNode.href = target.action;
        if (actionNode.protocol !== document.location.protocol || actionNode.host !== document.location.host) {
            return;
        }

        // Add link HREF to object
        var options = { url: target.action };

        // Check options are valid.
        options = pjax.parseOptions(options, target);
        if (options === false) return;

        // Don't fire normal event
        event.preventDefault();

        // handle the submission
        pjax.submit(target, function(html) {
            if (html === false) {
                return;
            }

            pjax.onUnload(options);
            options = pjax.updateContent(html, options);
            pjax.updateHistory(options);
            pjax.onLoad(options);
        });
    });

    root.pjax = pjax;
})(this, this.$, this.Alertify);
