﻿/*!
 * Fetch wrapper and error logging.
 */
(function($, Alertify) {
    'use strict';

    /**
     * Wrap fetch with success/error handling.
     * @param {Object} options - Options to use for the request.
     * @param {Function} onSuccess - Function to handle success result.
     * @param {Function} onError - Function to handle error result.
     */
    var _ajax = function(options, onSuccess, onError) {
        options = options || {};
        options.headers = $.extend({
            'X-Requested-With': 'XMLHttpRequest'
        }, options.headers);
        if (options.token) {
            options.headers['X-XSRF-TOKEN'] = options.token;
            delete options.token;
        }

        // keep browser from caching requests by tacking milliseconds to end of url
        var url = options.url + (options.url.indexOf('?') > -1 ? '&' : '?') + '_t=' + Date.now();
        delete options.url;

        if (options.data) {
            options.headers['Content-Type'] = 'application/json';
            if (options.method === 'GET') {
                url += '&' + Object.keys(options.data).map(function(x) {
                    return encodeURIComponent(x) + '=' + encodeURIComponent(options.data[x]);
                }).join('&');
            } else {
                options.body = JSON.stringify(options.data);
            }
            delete options.data;
        }
        options.credentials = 'same-origin';

        fetch(url, options)
            .then(_checkStatus)
            .then(_parse)
            .then(function(data) {
                if (data.error) {
                    if ($.isFunction(onError)) {
                        onError(data);
                    }
                    Alertify.error(data.error);
                } else {
                    if ($.isFunction(onSuccess)) {
                        onSuccess(data);
                    }
                    if (data.message) {
                        Alertify.success(data.message);
                    }
                }
            }).catch(function(data) {
                if (url.indexOf('LogJavascriptError') > -1) {
                    return;
                }

                if (data.response && data.response.status && [400, 401, 402, 403].indexOf(data.response.status) > -1) {
                    var locationHeader = data.response.headers && data.response.headers.get('location');
                    if (locationHeader) {
                        window.location.href = locationHeader;
                    } else {
                        window.location.reload(true);
                    }
                    return;
                }

                Alertify.error((data.response && data.response.error) || 'An unhandled error occurred.');
                if ($.isFunction(onError)) {
                    onError(data.response);
                }
            });
    };

    var _requestQueue = [];

    /**
     * Request constructor.
     * @param {Object} options - Request options including url/method/etc.
     * @param {Function} onSuccess - Function to run on success.
     * @param {Function} onError - Function to run on error.
     */
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
     * Check fetch response for error codes.
     * @param {Object} response - Fetch response.
     * @returns {Object} Returns fetch response.
     */
    var _checkStatus = function(response) {
        if (response.status >= 200 && response.status < 300) {
            return response;
        } else {
            var error = new Error(response.statusText);
            error.response = response;
            throw error;
        }
    };

    /**
     * Deserialize response from fetch.
     * @param {Object} response - Response object
     * @returns {Object} Result object from JSON, or object with a single 'content' property if that fails.
     */
    var _parse = function(response) {
        try {
            var contentType = response && response.headers.has('content-type') ? response.headers.get('content-type') : '';
            return contentType && contentType.indexOf('application/json') > -1 ? response.json() : response.text();
        } catch (ex) {
            return response;
        }
    };

    /**
     * Add a application wide error handler to log errors.
     * Outside of strict mode to prevent errors.
     * @param {string} msg - Error message.
     * @param {string} url - Error source URL.
     * @param {number} lineNo - Line # error occurred on
     * @param {number} columnNo - Column # error occurred on
     * @param {Error|null} error - Error object if browser supports it
     */
    window.onerror = function(msg, url, lineNo, columnNo, error) {
        if ($.isNull(msg)) {
            return;
        }

        var detail = msg + ': at path \'' + (url || document.location) + '\'';
        if (!$.isNull(lineNo)) {
            detail += ' at ' + lineNo + ':' + columnNo;
        }
        var stack = error && error.stack ? error.stack : null;
        if (!$.isNull(stack)) {
            detail += '\n    at ' + ($.isString(stack) ? stack : stack.join('\n    at '));
        }

        // save error message to server
        _ajax({ method: 'POST', url: '/Error/LogJavascriptError', data: { message: detail }, block: false }, null, null);
    };

    $.ajax = ajax;
}(this.$, this.Alertify));
