/**!
 * PJAX- Standalone
 *
 * A standalone implementation of Pushstate AJAX, for non-jQuery web pages.
 * jQuery are recommended to use the original implementation at: http://github.com/defunkt/jquery-pjax
 *
 * @version 0.6.1
 * @author Carl
 * @source https://github.com/thybag/PJAX-Standalone
 * @license MIT
 */
(function(root, $, Alertify) {
    'use strict';

    // Object to store private values/methods.
    var internal = {
        // Is this the first usage of PJAX? (Ensure history entry has required values if so.)
        'firstrun': true,
        // Borrowed wholesale from https://github.com/defunkt/jquery-pjax
        // Attempt to check that a device supports pushstate before attempting to use it.
        'is_supported': window.history && window.history.pushState && window.history.replaceState && !navigator.userAgent.match(/((iPod|iPhone|iPad).+\bOS\s+[1-4]|WebApps\/.+CFNetwork)/)
    };

    // If PJAX isn't supported we can skip setting up the library all together
    // So as not to break any code expecting PJAX to be there, return a shell object containing
    // IE7 + compatible versions of connect (which needs to do nothing) and invoke ( which just changes the page)
    if (!internal.is_supported) {
        // PJAX shell, so any code expecting PJAX will work
        window.pjax = {
            'connect': function() { return; },
            'invoke': function() {
                var url = (arguments.length === 2) ? arguments[0] : arguments.url;
                document.location = url;
                return;
            }
        };
        return;
    }

    /**
     * triggerEvent
     * Fire an event on a given object (used for callbacks)
     *
     * @scope private
     * @param node. Objects to fire event on
     * @return event_name. type of event
     */
    internal.triggerEvent = function(node, eventName, data) {
        // Good browsers
        var evt = document.createEvent('HTMLEvents');
        evt.initEvent(eventName, true, true);
        // If additional data was provided, add it to event
        if (typeof data !== 'undefined') evt.data = data;
        node.dispatchEvent(evt);
    };

    /**
     * popstate listener
     * Listens for back/forward button events and updates page accordingly.
     */
    $.on(window, 'popstate', function(e) {
        if (e.state !== null) {
            var opt = {
                url: e.state.url,
                container: e.state.container,
                title: e.state.title,
                method: e.state.method,
                history: false
            };

            // Merge original in original connect options
            if (typeof internal.options !== 'undefined') {
                for (var a in internal.options) {
                    if (typeof opt[a] === 'undefined') opt[a] = internal.options[a];
                }
            }

            // Convert state data to PJAX options
            var options = internal.parseOptions(opt);
            // If something went wrong, return.
            if (options === false) return;
            // If there is a state object, handle it as a page load.
            internal.handle(options);
        }
    });

    /**
     * attach
     * Attach PJAX listeners to a link.
     * @scope private
     * @param link_node. link that will be clicked.
     * @param content_node.
     */
    internal.attach = function(node, options) {
        // Ignore external links.
        if (node.protocol !== document.location.protocol ||
            node.host !== document.location.host) {
            return;
        }

        // Ignore anchors on the same page
        if (node.pathname === location.pathname && node.hash.length > 0) {
            return;
        }

        // Ignore common non-PJAX loadable media types (pdf/doc/zips & images) unless user provides alternate array
        var ignoreFileTypes = ['pdf', 'doc', 'docx', 'zip', 'rar', '7z', 'gif', 'jpeg', 'jpg', 'png'];
        if (typeof options.ignoreFileTypes === 'undefined') options.ignoreFileTypes = ignoreFileTypes;
        // Skip link if file type is within ignored types array
        if (options.ignoreFileTypes.indexOf(node.pathname.split('.').pop().toLowerCase()) !== -1) {
            return;
        }

        // Add link HREF to object
        options.url = node.href;
        options.method = node.getAttribute('data-method') || 'GET';

        // If PJAX data is specified, use as container
        if (node.getAttribute('data-pjax')) {
            options.container = node.getAttribute('data-pjax');
        }

        // If data-title is specified, use as title.
        if (node.getAttribute('data-title')) {
            options.title = node.getAttribute('data-title');
        }

        // If data-reload is specified, allow repeat requests to the same url.
        if (node.getAttribute('data-reload')) {
            options.reload = true;
        }

        // Check options are valid.
        options = internal.parseOptions(options);
        if (options === false) return;

        // Attach event.
        $.on(node, 'click', function(event) {
            // Allow middle click (pages in new windows)
            if (event.which > 1 || event.metaKey || event.ctrlKey) return;
            // Don't fire normal event
            if (event.preventDefault) { event.preventDefault(); } else { event.returnValue = false; }
            // Take no action if we are already on said page and reload isn't allowed
            if (document.location.href === options.url && !options.reload) return false;
            // handle the load
            if (this.getAttribute('data-confirm')) {
                Alertify.dismissAll();
                Alertify.confirm(this.getAttribute('data-confirm'), internal.handle.bind(null, options), function(e) { e.target.focus(); });
            } else {
                var form = $.get('form.has-changes');
                if (form) {
                    Alertify.confirm(form.getAttribute('data-confirm'), internal.handle.bind(null, options), function(e) { e.target.focus(); });
                } else {
                    internal.handle(options);
                }
            }
        });
    };


    /**
     * attachForm
     * Attach PJAX listeners to a form.
     * @scope private
     * @param link_node. form that will be submitted.
     * @param content_node.
     */
    internal.attachForm = function(node, options) {
        // Ignore external links.
        /* check protocol for form action
        if (form.getAttribute('action') node.protocol !== document.location.protocol ||
            node.host !== document.location.host) {
            return;
        }
        */

        // Add link HREF to object
        options.url = node.action;

        // If PJAX data is specified, use as container
        if (node.getAttribute('data-pjax')) {
            options.container = node.getAttribute('data-pjax');
        }

        // If data-title is specified, use as title.
        if (node.getAttribute('data-title')) {
            options.title = node.getAttribute('data-title');
        }

        // Check options are valid.
        options = internal.parseOptions(options);
        if (options === false) return;

        // Attach event for detecting changes
        $.on(node, 'change', function(e) {
            var form = e.currentTarget || e.target;
            if (form.nodeName === 'FORM') {
                $.addClass(form, 'has-changes');
            }
        }, true);

        // Attach event for handling form submission
        $.on(node, 'submit', function(e) {
            // Allow middle click (pages in new windows)
            if (e.which > 1 || e.metaKey || e.ctrlKey) {
                return;
            }
            // Don't fire normal event
            if (e.preventDefault) {
                e.preventDefault();
            } else {
                e.returnValue = false;
            }
            // handle the submission
            options.form = this;
            internal.handleForm(options);
        });
    };


    /**
     * Parse all links within a DOM node, using settings provided in options.
     * @scope private
     * @param {Node} node - DOM node to parse for links
     * @param {Object} options - Valid Options object
     */
    internal.parseLinks = function(node, options) {
        $.getAll('a', node).filter(function(x) {
            return $.isNull(options.excludeClass) || x.className.indexOf(options.excludeClass) === -1;
        }).forEach(function(x) {
            // Override options history to true, else link parsing could be triggered by back button (which runs in no-history mode)
            var opt = $.clone(options);
            opt.history = true;
            internal.attach(x, opt);
        });

        if (internal.firstrun) {
            // Fire ready event once all links are connected
            var container = internal.getContainerNode(options.container);
            if (container) {
                internal.triggerEvent(container, 'ready');
            }
        }
    };

    /**
     * Parse all forms within a DOM node, using settings provided in options.
     * @scope private
     * @param {Node} node - Dom node to parse for forms
     * @param {Object} options - Valid Options object
     */
    internal.parseForms = function(node, options) {
        $.getAll('form', node).filter(function(x) {
            return $.isNull(options.excludeClass) || x.className.indexOf(options.excludeClass) === -1;
        }).forEach(function(node) {
            // Override options history to true, else link parsing could be triggered by back button (which runs in no-history mode)
            var opt = $.clone(options);
            opt.history = true;
            internal.attachForm(node, opt);
        });
    };

    /**
     * Updates DOM with content loaded via PJAX
     * @param html DOM fragment of loaded container
     * @param options PJAX configuration options
     * return options
     */
    internal.updateContent = function(html, options) {
        var newNode = $.createNode(html);
        if ($.isNull(options.title)) {
            var title = $.getAll('[data-title]', newNode, true);
            if (title.length) {
                options.title = title[0].getAttribute('data-title');
            }
        }

        // Update the DOM with the new content
        options.container.innerHTML = html;

        // Send data back to handle
        return options;
    };

    /**
     * Attach listeners to content after loading it.
     * @param {Object} options - Request options.
     */
    internal.onLoad = function(options) {
        internal.parseLinks(options.container, options);
        internal.parseForms(options.container, options);
        $.content.load(options.container);
        internal.checkEvents(options.container, 'data-load-event');

        // Fire Events
        internal.triggerEvent(options.container, 'complete', options);
        internal.triggerEvent(options.container, 'success', options);

        // Set new title
        if (!$.isNull(options.title)) {
            document.title = options.title;
        }

        // Scroll page to top on new page load
        if (options.returnToTop) {
            window.scrollTo(0, 0);
        }
    };

    /**
     * Remove listeners from content after unloading it.
     * @param options
     */
    internal.onUnload = function(options) {
        internal.checkEvents(options.container, 'data-unload-event');
        $.content.unload(options.container);
    };

    /**
     * Handle requests to load content via PJAX.
     * @scope private
     * @param url. Page to load.
     * @param node. Dom node to add returned content in to.
     * @param addtohistory. Does this load require a history event.
     */
    internal.handle = function(options) {
        // Fire beforeSend Event.
        internal.triggerEvent(options.container, 'beforeSend', options);

        // Do the request
        internal.request(options, function(html) {
            // Fail if unable to load HTML via AJAX
            if (html === false) {
                internal.triggerEvent(options.container, 'complete', options);
                internal.triggerEvent(options.container, 'error', options);
                $.content.done();
                return;
            }

            internal.onUnload(options);
            options = internal.updateContent(html, options);
            if (options.history) {
                // If this is the first time pjax has run, create a state object for the current page.
                if (internal.firstrun) {
                    window.history.replaceState({ url: document.location.href, container: options.container.id, title: document.title, method: options.method }, document.title);
                    internal.firstrun = false;
                }
                // Update browser history
                window.history.pushState({ url: options.url, container: options.container.id, title: options.title, method: options.method }, options.title, options.url);
            }
            internal.onLoad(options);
        });
    };

    /**
     * Performs AJAX request to page and returns the result..
     * @scope private
     * @param options Options
     * @param callback. Method to call when a page is loaded.
     */
    internal.request = function(options, callback) {
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
     * handleForm
     * Handle form requests to load content via PJAX.
     * @scope private
     * @param url. Page to load.
     * @param node. Dom node to add returned content in to.
     * @param addtohistory. Does this load require a history event.
     */
    internal.handleForm = function(options) {
        // Fire beforeSend Event.
        internal.triggerEvent(options.container, 'beforeSend', options);

        // Do the request
        internal.submit(options.form, function(html) {
            // Fail if unable to load HTML via AJAX
            if (html === false) {
                internal.triggerEvent(options.container, 'complete', options);
                internal.triggerEvent(options.container, 'error', options);
                return;
            }

            internal.onUnload(options);
            options = internal.updateContent(html, options);
            internal.onLoad(options);
        });
    };

    /**
     * submit
     * Performs AJAX form submission request to page and returns the result.
     *
     * @scope private
     * @param form. Form to submit.
     * @param callback. Method to call when a page is loaded.
     */
    internal.submit = function(form, callback) {
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
     * parseOptions
     * Validate and correct options object while connecting up any listeners.
     *
     * @scope private
     * @param options
     * @return false | valid options object
     */
    internal.parseOptions = function(options) {
        /**  Defaults parse options. (if something isn't provided)
         *
         * - history: track event to history (on by default, set to off when performing back operation)
         * - returnToTop: Scroll user back to top of page, when new page is opened by PJAX
         */
        var defaults = {
            'history': true,
            'returnToTop': true
        };

        // Ensure a URL and container have been provided.
        if (typeof options.url === 'undefined' || typeof options.container === 'undefined' || options.container === null) {
            return false;
        }

        // Check required options are defined, if not, use default
        options = $.extend(options, defaults);

        // Ensure history setting is a boolean.
        options.history = (options.history === false) ? false : true;

        // Get container (if its an id, convert it to a DOM node.)
        options.container = internal.getContainerNode(options.container);

        // Events
        ['ready', 'beforeSend', 'complete', 'error', 'success'].forEach(function(x) {
            if (typeof options[x] === 'function') {
                $.on(options.container, x, options[x]);
            }
        });

        // Return valid options
        return options;
    };

    /**
     * Search node for event attributes and dispatch them.
     * @param {Node} node - DOM node to search in.
     * @param {string} eventName - Name of event attribute to search for.
     */
    internal.checkEvents = function(node, eventName) {
        $.getAll('[' + eventName + ']', node, node !== document).forEach(function(x) {
            var ev = x.getAttribute(eventName);
            if ($.events.hasOwnProperty(ev)) {
                $.dispatch(x, $.events[ev]);
            }
        });
    };

    /**
     * getContainerNode
     * Returns container node
     *
     * @param container - (string) container ID | container DOM node.
     * @return container DOM node | false
     */
    internal.getContainerNode = function(container) {
        if (typeof container === 'string') {
            container = document.getElementById(container);
            if (container === null) {
                return false;
            }
        }
        return container;
    };

    /**
     * Attach links to PJAX handlers.
     * @scope public
     *
     * Calling as connect();
     *        Will look for links with the data-pjax attribute.
     * Calling as connect({
     *                        'url':'somepage.php',
     *                        'container':'somecontainer',
     *                        'beforeSend': function(){console.log("sending");}
     *                    })
     *        Will use the provided JSON to configure the script in full (including callbacks)
     */
    internal.connect = function(options) {
        // Delete history and title if provided. These options should only be provided via invoke();
        delete options.title;
        delete options.history;

        internal.options = options;
        var body = $.get('body');
        internal.parseLinks(body, options);
        internal.parseForms(body, options);
        $.content.load(body);
        internal.checkEvents(body, 'data-load-event');
    };

    /**
     * invoke
     * Directly invoke a pjax page load.
     * invoke({url: 'file.php', 'container':'content'});
     *
     * @scope public
     * @param options
     */
    internal.invoke = function(options) {
        // Process options
        options = internal.parseOptions(options);
        // If everything went okay, activate pjax.
        if (options !== false) {
            internal.handle(options);
        }
    };

    // Make PJAX object accessible in global name space
    root.pjax = internal;
})(this, this.$, this.Alertify);
