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
        'is_supported': window.history && window.history.pushState && window.history.replaceState && !navigator.userAgent.match(/((iPod|iPhone|iPad).+\bOS\s+[1-4]|WebApps\/.+CFNetwork)/),
        // Track which scripts have been included in to the page. (used if e)
        'loaded_scripts': []
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
     * AddEvent
     *
     * @scope private
     * @param obj Object to listen on
     * @param event Event to listen for.
     * @param callback Method to run when event is detected.
     */
    internal.addEvent = function(obj, event, callback) {
        obj.addEventListener(event, callback, false);
    };

    /**
     * Clone
     * Util method to create copies of the options object (so they do not share references)
     * This allows custom settings on different links.
     *
     * @scope private
     * @param obj
     * @return obj
     */
    internal.clone = function(obj) {
        var object = {};
        // For every option in object, create it in the duplicate.
        for (var i in obj) {
            object[i] = obj[i];
        }
        return object;
    };

    /**
     * triggerEvent
     * Fire an event on a given object (used for callbacks)
     *
     * @scope private
     * @param node. Objects to fire event on
     * @return event_name. type of event
     */
    internal.triggerEvent = function(node, event_name, data) {
        // Good browsers
        var evt = document.createEvent('HTMLEvents');
        evt.initEvent(event_name, true, true);
        // If additional data was provided, add it to event
        if (typeof data !== 'undefined') evt.data = data;
        node.dispatchEvent(evt);
    };

    /**
     * popstate listener
     * Listens for back/forward button events and updates page accordingly.
     */
    internal.addEvent(window, 'popstate', function(e) {
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

        // Check options are valid.
        options = internal.parseOptions(options);
        if (options === false) return;

        // Attach event.
        internal.addEvent(node, 'click', function(event) {
            // Allow middle click (pages in new windows)
            if (event.which > 1 || event.metaKey || event.ctrlKey) return;
            // Don't fire normal event
            if (event.preventDefault) { event.preventDefault(); } else { event.returnValue = false; }
            // Take no action if we are already on said page?
            if (document.location.href === options.url) return false;
            // handle the load
            if (options.method === 'DELETE') {
                Alertify.dismissAll();
                Alertify.confirm(this.getAttribute('data-message'), internal.handle.bind(null, options), event.target.focus);
            } else {
                internal.handle(options);
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

        // Attach event.
        internal.addEvent(node, 'submit', function(event) {
            // Allow middle click (pages in new windows)
            if (event.which > 1 || event.metaKey || event.ctrlKey) return;
            // Don't fire normal event
            if (event.preventDefault) { event.preventDefault(); } else { event.returnValue = false; }
            // handle the load.
            options.form = this;
            internal.handleForm(options);
        });
    };


    /**
     * parseLinks
     * Parse all links within a DOM node, using settings provided in options.
     * @scope private
     * @param dom_obj. Dom node to parse for links.
     * @param options. Valid Options object.
     */
    internal.parseLinks = function(dom_obj, options) {
        var nodes;

        if (typeof options.useClass !== 'undefined') {
            // Get all nodes with the provided class name.
            nodes = dom_obj.getElementsByClassName(options.useClass);
        } else {
            // If no class was provided, just get all the links
            nodes = dom_obj.getElementsByTagName('a');
        }

        var node;
        // For all returned nodes
        for (var i = 0, tmp_opt; i < nodes.length; i++) {
            node = nodes[i];
            if (typeof options.excludeClass !== 'undefined') {
                if (node.className.indexOf(options.excludeClass) !== -1) continue;
            }
            // Override options history to true, else link parsing could be triggered by back button (which runs in no-history mode)
            tmp_opt = internal.clone(options);
            tmp_opt.history = true;
            internal.attach(node, tmp_opt);
        }

        if (internal.firstrun) {
            // Store array or all currently included script src's to avoid PJAX accidentally reloading existing libraries
            var scripts = document.getElementsByTagName('script');
            for (var c = 0; c < scripts.length; c++) {
                if (scripts[c].src && internal.loaded_scripts.indexOf(scripts[c].src) === -1) {
                    internal.loaded_scripts.push(scripts[c].src);
                }
            }

            // Fire ready event once all links are connected
            node = internal.getContainerNode(options.container);
            if (node) {
                internal.triggerEvent(node, 'ready');
            }
        }
    };

    /**
     * parseForms
     * Parse all forms within a DOM node, using settings provided in options.
     * @scope private
     * @param dom_obj. Dom node to parse for forms.
     * @param options. Valid Options object.
     */
    internal.parseForms = function(dom_obj, options) {
        var nodes = dom_obj.getElementsByTagName('form');

        // For all returned nodes
        for (var i = 0, tmp_opt; i < nodes.length; i++) {
            var node = nodes[i];
            if (typeof options.excludeClass !== 'undefined') {
                if (node.className.indexOf(options.excludeClass) !== -1) continue;
            }
            // Override options history to true, else link parsing could be triggered by back button (which runs in no-history mode)
            tmp_opt = internal.clone(options);
            tmp_opt.history = true;
            internal.attachForm(node, tmp_opt);
        }

        /* going to want to fire ready event after processing both links and forms
        if (internal.firstrun) {
            // Store array or all currently included script src's to avoid PJAX accidentally reloading existing libraries
            var scripts = document.getElementsByTagName('script');
            for (var c = 0; c < scripts.length; c++) {
                if (scripts[c].src && internal.loaded_scripts.indexOf(scripts[c].src) === -1) {
                    internal.loaded_scripts.push(scripts[c].src);
                }
            }

            // Fire ready event once all links are connected
            var node = internal.getContainerNode(options.container);
            if (node) {
                internal.triggerEvent(node, 'ready');
            }
        }
        */
    };

    /**
     * SmartLoad
     * Smartload checks the returned HTML to ensure PJAX ready content has been provided rather than
     * a full HTML page. If a full HTML has been returned, it will attempt to scan the page and extract
     * the correct HTML to update our container with in order to ensure PJAX still functions as expected.
     *
     * @scope private
     * @param HTML (HTML returned from AJAX)
     * @param options (Options object used to request page)
     * @return HTML to append to our page.
     */
    internal.smartLoad = function(html, options) {
        // Grab the title if there is one
        var title = html.getElementsByTagName('title')[0];
        if (title) {
            document.title = title.innerHTML;
        }
        title = $.getAll('[data-title]', html, true);
        if (title.length) {
            document.title = title[0].getAttribute('data-title');
        }

        var container = html.querySelector('#' + options.container.id);
        if (container !== null) return container;

        // If our container was not found, HTML will be returned as is.
        return html;
    };

    /**
     * Update Content
     * Updates DOM with content loaded via PJAX
     *
     * @param html DOM fragment of loaded container
     * @param options PJAX configuration options
     * return options
     */
    internal.updateContent = function(html, options) {
        // Create in memory DOM node, to make parsing returned data easier
        var tmp = document.createElement('div');
        tmp.innerHTML = html;

        // Ensure we have the correct HTML to apply to our container.
        if (options.smartLoad) tmp = internal.smartLoad(tmp, options);

        // If no title was provided, extract it
        if (typeof options.title === 'undefined') {
            // Use current doc title (this will be updated via smart load if its enabled)
            options.title = document.title;

            // Attempt to grab title from non-smart loaded page contents
            if (!options.smartLoad) {
                var tmpTitle = tmp.getElementsByTagName('title');
                if (tmpTitle.length !== 0) {
                    options.title = tmpTitle[0].innerHTML;
                } else {
                    tmpTitle = $.getAll('[data-title]', tmp, true);
                    if (tmpTitle.length) {
                        document.title = tmpTitle[0].getAttribute('data-title');
                    }
                }
            }
        }

        // Update the DOM with the new content
        options.container.innerHTML = tmp.innerHTML;

        // Send data back to handle
        return options;
    };


    /**
     * handle
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
                return;
            }

            internal.checkEvents(options.container, 'data-unload-event');

            // Parse page & update DOM
            options = internal.updateContent(html, options);

            // Do we need to add this to the history?
            if (options.history) {
                // If this is the first time pjax has run, create a state object for the current page.
                if (internal.firstrun) {
                    window.history.replaceState({ url: document.location.href, container: options.container.id, title: document.title, method: options.method }, document.title);
                    internal.firstrun = false;
                }
                // Update browser history
                window.history.pushState({ url: options.url, container: options.container.id, title: options.title, method: options.method }, options.title, options.url);
            }

            // Initialize any new links found within document (if enabled).
            if (options.parseLinksOnload) {
                internal.parseLinks(options.container, options);
                internal.parseForms(options.container, options);
                $.dialogs.processContent(options.container);
            }

            // Fire Events
            internal.triggerEvent(options.container, 'complete', options);
            internal.triggerEvent(options.container, 'success', options);

            internal.checkEvents(options.container, 'data-load-event');

            // Set new title
            document.title = options.title;

            // Scroll page to top on new page load
            if (options.returnToTop) {
                window.scrollTo(0, 0);
            }
        });
    };

    /**
     * Request
     * Performs AJAX request to page and returns the result..
     *
     * @scope private
     * @param options Options
     * @param callback. Method to call when a page is loaded.
     */
    internal.request = function(options, callback) {
        // Create xmlHttpRequest object.
        var xhr = new XMLHttpRequest();
        // Add state listener.
        xhr.onreadystatechange = function() {
            if ((xhr.readyState === 4) && (xhr.status === 200)) {
                // Success, Return HTML
                callback(xhr.responseText);
            } else if ((xhr.readyState === 4) && (xhr.status === 404 || xhr.status === 500)) {
                // error (return false)
                callback(false);
            }
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

            internal.checkEvents(options.container, 'data-unload-event');

            // Parse page & update DOM
            options = internal.updateContent(html, options);

            // Do we need to add this to the history?
            /*
            if (options.history) {
                // If this is the first time pjax has run, create a state object for the current page.
                if (internal.firstrun) {
                    window.history.replaceState({ 'url': document.location.href, 'container': options.container.id, 'title': document.title }, document.title);
                    internal.firstrun = false;
                }
                // Update browser history
                window.history.pushState({ 'url': options.url, 'container': options.container.id, 'title': options.title }, options.title, options.url);
            }
            */

            // Initialize any new links found within document (if enabled).
            if (options.parseLinksOnload) {
                internal.parseLinks(options.container, options);
                internal.parseForms(options.container, options);
                $.dialogs.processContent(options.container);
            }

            // Fire Events
            internal.triggerEvent(options.container, 'complete', options);
            internal.triggerEvent(options.container, 'success', options);

            internal.checkEvents(options.container, 'data-load-event');

            // Set new title
            document.title = options.title;

            // Scroll page to top on new page load
            if (options.returnToTop) {
                window.scrollTo(0, 0);
            }
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
        // Create xmlHttpRequest object.
        var xhr = new XMLHttpRequest();
        // Add state listener.
        xhr.onreadystatechange = function() {
            if ((xhr.readyState === 4) && (xhr.status === 200)) {
                // Success, Return HTML
                callback(xhr.responseText);
            } else if ((xhr.readyState === 4) && (xhr.status === 404 || xhr.status === 500)) {
                // error (return false)
                callback(false);
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
         * - parseLinksOnload: Enabled by default. Process pages loaded via PJAX and setup PJAX on any links found.
         * - smartLoad: Tries to ensure the correct HTML is loaded. If you are certain your back end
         *        will only return PJAX ready content this can be disabled for a slight performance boost.
         * - returnToTop: Scroll user back to top of page, when new page is opened by PJAX
         */
        var defaults = {
            'history': true,
            'parseLinksOnload': true,
            'smartLoad': true,
            'returnToTop': true
        };

        // Ensure a URL and container have been provided.
        if (typeof options.url === 'undefined' || typeof options.container === 'undefined' || options.container === null) {
            return false;
        }

        // Check required options are defined, if not, use default
        for (var o in defaults) {
            if (typeof options[o] === 'undefined') options[o] = defaults[o];
        }

        // Ensure history setting is a boolean.
        options.history = (options.history === false) ? false : true;

        // Get container (if its an id, convert it to a DOM node.)
        options.container = internal.getContainerNode(options.container);

        // Events
        var events = ['ready', 'beforeSend', 'complete', 'error', 'success'];

        // If everything went okay thus far, connect up listeners
        for (var e in events) {
            var evt = events[e];
            if (typeof options[evt] === 'function') {
                internal.addEvent(options.container, evt, options[evt]);
            }
        }

        // Return valid options
        return options;
    };

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
     * connect
     * Attach links to PJAX handlers.
     * @scope public
     *
     * Can be called in 3 ways.
     * Calling as connect();
     *        Will look for links with the data-pjax attribute.
     *
     * Calling as connect(container_id)
     *        Will try to attach to all links, using the container_id as the target.
     *
     * Calling as connect(container_id, class_name)
     *        Will try to attach any links with the given class name, using container_id as the target.
     *
     * Calling as connect({
     *                        'url':'somepage.php',
     *                        'container':'somecontainer',
     *                        'beforeSend': function(){console.log("sending");}
     *                    })
     *        Will use the provided JSON to configure the script in full (including callbacks)
     */
    internal.connect = function(/* options */) {
        // connect();
        var options = {};
        // connect(container, class_to_apply_to)
        if (arguments.length === 2) {
            options.container = arguments[0];
            options.useClass = arguments[1];
        }
        // Either JSON or container id
        if (arguments.length === 1) {
            if (typeof arguments[0] === 'string') {
                //connect(container_id)
                options.container = arguments[0];
            } else {
                //Else connect({url:'', container: ''});
                options = arguments[0];
            }
        }
        // Delete history and title if provided. These options should only be provided via invoke();
        delete options.title;
        delete options.history;

        internal.options = options;
        if (document.readyState === 'complete') {
            internal.parseLinks(document, options);
            internal.parseForms(document, options);
            $.dialogs.processContent($.get('#' + options.container));
            internal.checkEvents(document);
        } else {
            //Don't run until the window is ready.
            internal.addEvent(window, 'load', function() {
                //Parse links using specified options
                internal.parseLinks(document, options);
                internal.parseForms(document, options);
                $.dialogs.processContent($.get('#' + options.container));
                internal.checkEvents(document);
            });
        }
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
