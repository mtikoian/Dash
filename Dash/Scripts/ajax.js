/*!
 * Contains ajax wrapper and error logging code.
 */
(function(m, $, Alertify) {
    'use strict';

    /**
     * Wrap Mithril Ajax request with success/error handling.
     * @param {Object} options - Options to use for the ajax request.
     * @param {Function} onSuccess - Function to handle success result.
     * @param {Function} onError - Function to handle error result.
     */
    var _ajax = function(options, onSuccess, onError) {
        options.headers = {
            'Content-Type': 'application/jil; charset=utf-8',
            'Accept': 'application/jil',
            'X-Requested-With': 'XMLHttpRequest'
        };
        if (options.token) {
            options.headers['X-XSRF-Token'] = options.token;
            delete options.token;
        }
        options.config = function(xhr) {
            xhr.timeout = 60000;
        };
        options.extract = function(xhr) {
            return { status: xhr.status, data: _deserialize(xhr.responseText) };
        };

        var canBlock = $.coalesce(options.block, true);
        if (canBlock) {
            block();
        }

        // keep IE from caching requests by tacking milliseconds to end of url
        options.url += (options.url.indexOf('?') > -1 ? '&' : '?') + '_t=' + Date.now();

        m.request(options).then(function(response) {
            if (response.data.reload) {
                location.reload();
                return;
            }
            if (response.data.error) {
                if (canBlock) {
                    unblock();
                }
                if ($.isFunction(onError)) {
                    onError(response.data);
                }
                Alertify.error(response.data.error);
            } else {
                if (canBlock) {
                    unblock();
                }
                if ($.isFunction(onSuccess)) {
                    onSuccess(response.data);
                }
                if (response.data.message) {
                    Alertify.success(response.data.message);
                }
            }
        }).catch(function(response) {
            if (canBlock) {
                unblock();
            }
            if (options.url.indexOf('LogJavascriptError') > -1) {
                return;
            }
            logError(response.data);
            if ([400, 401, 402, 403].indexOf(response.status) > -1) {
                Alertify.error(($.resx && $.resx('errorAuthorization')) || 'You do not have permission to access the requested resource.');
            } else {
                Alertify.error(($.resx && $.resx('errorGeneric')) || 'An unhandled error occurred.');
            }
            if ($.isFunction(onError)) {
                onError(response.data);
            }
        });
    };

    /**
     * User request queue.
     */
    var _requestQueue = [];

    var Request = function(options, onSuccess, onError) {
        this.options = options;
        this.onSuccess = onSuccess;
        this.onError = onError;
        this.status = 0;
    };

    Request.prototype = {
        constructor: Request,
        key: function() {
            return this.options.key;
        },
        abort: function() {
            if (this.isInProcess()) {
                this.promise.reject();
            }
            this.dequeue();
        },
        execute: function() {
            _ajax(this.options, this.success.bind(this), this.error.bind(this));
            this.status = 1;
        },
        success: function(data) {
            this.dequeue();
            if (this.onSuccess) {
                this.onSuccess(data);
            }
        },
        error: function(data) {
            this.dequeue();
            if (this.onError) {
                this.onError(data);
            }
        },
        isInProcess: function() {
            return this.status === 1;
        },
        dequeue: function() {
            // remove this from the queue and start the next request
            var self = this;
            _requestQueue = _requestQueue.filter(function(x) { return x !== self; });
            if (_requestQueue.length) {
                _requestQueue[0].execute();
            }
        }
    };

    /**
     * Queue up an ajax request. Queue prevents one user from hammering the server.
     * @param {Object} options - Options to use for the ajax request.
     * @param {Function} onSuccess - Function to handle success result.
     * @param {Function} onError - Function to handle error result.
     */
    var ajax = function(options, onSuccess, onError) {
        options.key = options.key || options.url;
        var request = new Request(options, onSuccess, onError);

        // remove requests from queue that are for this key and aren't already in process
        _requestQueue = _requestQueue.filter(function(x) {
            return x.key() !== options.key || x.isInProcess();
        });
        _requestQueue.push(request);

        if (_requestQueue.length === 1) {
            // nothing else in the queue, so execute now
            request.execute();
        }
    };

    /**
     * Display the loading splash screen.
     */
    var block = function() {
        $.show(_loadingDiv);
    };

    /**
     * Log JS errors to elmah.
     * @param {string} msg - Error message.
     * @param {string} url - Error source URL.
     * @param {number} line - Line # error occurred on
     * @param {number} columnNo - Column # error occurred on
     * @param {string|string[]|null} stack - Stack trace.
     */
    var logError = function(msg, url, lineNo, columnNo, stack) {
        if ($.isNull(msg)) {
            return;
        }

        var detail = msg + ': at path \'' + (url || document.location) + '\'';
        if (!$.isNull(lineNo)) {
            detail += ' at ' + lineNo + ':' + columnNo;
        }
        if (!$.isNull(stack)) {
            detail += '\n    at ' + ($.isString(stack) ? stack : stack.join('\n    at '));
        }

        // save error message to server
        _ajax({ method: 'POST', url: '/Error/LogJavascriptError', data: { message: detail }, block: false }, null, null);
    };

    /**
     * Hide the loading splash screen.
     */
    var unblock = function() {
        $.hide(_loadingDiv);
    };

    /**
     * Deserialize response from ajax request.
     * @param {string} data - String of data to deserialize.
     * @returns {Object} Result object from JSON, or object with a single 'content' property if that fails.
     */
    var _deserialize = function(data) {
        if ($.isNull(data) || data.length === 0) {
            return null;
        }
        try {
            return JSON.parse(data);
        } catch (e) {
            return { content: data };
        }
    };

    /**
     * Closure to set up the loading splash screen and return the node for it.
     */
    var _loadingDiv = (function() {
        var div = $.get('#loader');
        $.on(div, 'keydown', function(e) {
            if ($.hasClass('#loader', 'hidden')) {
                return;
            }
            e.preventDefault();
            e.stopPropagation();
            return false;
        });
        return div;
    })();

    $.ajax = ajax;
    $.logError = logError;
}(this.m, this.$, this.Alertify));

/**
 * Add a application wide error handler to log errors.
 * Outside of strict mode to prevent errors.
 * @param {string} msg - Error message.
 * @param {string} url - Error source URL.
 * @param {number} line - Line # error occurred on
 * @param {number} columnNo - Column # error occurred on
 * @param {Error|null} error - Error object if browser supports it
 */
window.onerror = function(msg, url, lineNo, columnNo, error) {
    this.$.logError(msg, url, lineNo, columnNo, error && error.stack ? error.stack : null);
};
