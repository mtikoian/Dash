﻿/*!
 * JavaScript autoComplete v1.0.4
 * Copyright (c) 2014 Simon Steinberger / Pixabay
 * GitHub: https://github.com/Pixabay/JavaScript-autoComplete
 * License: http://www.opensource.org/licenses/mit-license.php
 */
(function(root, factory) {
    root.Autocomplete = factory(root.$);
})(this, function($) {
    'use strict';

    function Autocomplete(options) {
        function live(elClass, event, cb, context) {
            $.on(context || document, event, function(e) {
                var found, el = e.target || e.srcElement;
                while (el && !(found = $.hasClass(el, elClass)))
                    el = el.parentElement;
                if (found)
                    cb.call(el, e);
            });
        }

        var opts = $.extend({
            selector: 0,
            source: 0,
            minChars: 2,
            delay: 250,
            offsetLeft: 0,
            offsetTop: 1,
            cache: 1,
            sourceData: null,
            menuClass: '',
            renderItem: function(item, search) {
                // escape special characters
                search = search.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&');
                var re = new RegExp('(' + search.split(' ').join('|') + ')', 'gi');
                return '<div class="autocomplete-suggestion" data-val="' + item + '">' + item.replace(re, '<b>$1</b>') + '</div>';
            },
            onSelect: function() { }
        }, options);

        // init
        var elems = typeof opts.selector === 'object' ? [opts.selector] : document.querySelectorAll(opts.selector);
        for (var i = 0; i < elems.length; i++) {
            var that = elems[i];

            // create suggestions container "sc"
            that.sc = document.createElement('div');
            that.sc.className = 'autocomplete-suggestions ' + opts.menuClass;

            that.autocompleteAttr = that.getAttribute('autocomplete');
            that.setAttribute('autocomplete', 'off');
            that.cache = {};
            that.last_val = '';

            that.updateSC = function(resize, next) {
                var rect = that.getBoundingClientRect();
                that.sc.style.left = Math.round(rect.left + (window.pageXOffset || document.documentElement.scrollLeft) + opts.offsetLeft) + 'px';
                that.sc.style.top = Math.round(rect.bottom + (window.pageYOffset || document.documentElement.scrollTop) + opts.offsetTop) + 'px';
                that.sc.style.width = Math.round(rect.right - rect.left) + 'px'; // outerWidth
                if (!resize) {
                    that.sc.style.display = 'block';
                    if (!that.sc.maxHeight)
                        that.sc.maxHeight = parseInt((window.getComputedStyle ? getComputedStyle(that.sc, null) : that.sc.currentStyle).maxHeight);
                    if (!that.sc.suggestionHeight)
                        that.sc.suggestionHeight = that.sc.querySelector('.autocomplete-suggestion').offsetHeight;
                    if (that.sc.suggestionHeight)
                        if (!next) {
                            that.sc.scrollTop = 0;
                        } else {
                            var scrTop = that.sc.scrollTop, selTop = next.getBoundingClientRect().top - that.sc.getBoundingClientRect().top;
                            if (selTop + that.sc.suggestionHeight - that.sc.maxHeight > 0)
                                that.sc.scrollTop = selTop + that.sc.suggestionHeight + scrTop - that.sc.maxHeight;
                            else if (selTop < 0)
                                that.sc.scrollTop = selTop + scrTop;
                        }
                }
            };
            $.on(window, 'resize', that.updateSC);
            document.body.appendChild(that.sc);

            live('autocomplete-suggestion', 'mouseleave', function() {
                var sel = that.sc.querySelector('.autocomplete-suggestion.selected');
                if (sel)
                    setTimeout(function() { sel.className = sel.className.replace('selected', ''); }, 20);
            }, that.sc);

            live('autocomplete-suggestion', 'mouseover', function() {
                var sel = that.sc.querySelector('.autocomplete-suggestion.selected');
                if (sel)
                    sel.className = sel.className.replace('selected', '');
                this.className += ' selected';
            }, that.sc);

            live('autocomplete-suggestion', 'mousedown', function(e) {
                if ($.hasClass(this, 'autocomplete-suggestion')) { // else outside click
                    var v = this.getAttribute('data-val');
                    that.value = v;
                    opts.onSelect(e, v, this);
                    that.sc.style.display = 'none';
                }
            }, that.sc);

            that.blurHandler = function() {
                var over_sb;
                try {
                    over_sb = document.querySelector('.autocomplete-suggestions:hover');
                } catch (e) {
                    over_sb = 0;
                }
                if (!over_sb) {
                    if (opts.sourceData && opts.sourceData.indexOf(that.value) === -1)
                        that.value = '';
                    that.last_val = that.value;
                    that.sc.style.display = 'none';
                    setTimeout(function() { that.sc.style.display = 'none'; }, 350); // hide suggestions on fast input
                } else if (that !== document.activeElement) {
                    setTimeout(function() { that.focus(); }, 20);
                }
            };
            $.on(that, 'blur', that.blurHandler);

            var suggest = function(data) {
                var val = that.value;
                that.cache[val] = data;
                if (data.length && val.length >= opts.minChars) {
                    var s = '';
                    for (var i = 0; i < data.length; i++)
                        s += opts.renderItem(data[i], val);
                    that.sc.innerHTML = s;
                    that.updateSC(0);
                } else {
                    that.sc.style.display = 'none';
                }
            };

            var internalSource = function(sourceData, term, suggest) {
                term = term.toLowerCase();
                var matches = [];
                for (i = 0; i < sourceData.length; i++)
                    if (~sourceData[i].toLowerCase().indexOf(term)) matches.push(sourceData[i]);
                suggest(matches);
            };

            that.keydownHandler = function(e) {
                var sel = that.sc.querySelector('.autocomplete-suggestion.selected');
                var key = window.event ? e.keyCode : e.which;
                // down (40), up (38)
                if ((key === 40 || key === 38) && that.sc.innerHTML) {
                    var next;
                    if (!sel) {
                        next = key === 40 ? that.sc.querySelector('.autocomplete-suggestion') : that.sc.childNodes[that.sc.childNodes.length - 1]; // first : last
                        next.className += ' selected';
                        that.value = next.getAttribute('data-val');
                    } else {
                        next = key === 40 ? sel.nextSibling : sel.previousSibling;
                        if (next) {
                            sel.className = sel.className.replace('selected', '');
                            next.className += ' selected';
                            that.value = next.getAttribute('data-val');
                        } else {
                            sel.className = sel.className.replace('selected', '');
                            that.value = that.last_val;
                            next = 0;
                        }
                    }
                    that.updateSC(0, next);
                    return false;
                } else if (key === 27) {
                    // esc
                    that.value = '';
                    that.sc.style.display = 'none';
                } else if (key === 13 || key === 9) {
                    // enter or tab
                    if (sel && that.sc.style.display !== 'none') {
                        if (key === 13)
                            e.preventDefault();
                        that.value = sel.getAttribute('data-val');
                        opts.onSelect(e, sel.getAttribute('data-val'), sel);
                        setTimeout(function() {
                            that.sc.style.display = 'none';
                        }, 20);
                    } else {
                        if (opts.sourceData && opts.sourceData.indexOf(that.value) === -1)
                            that.value = '';
                    }
                }
            };
            $.on(that, 'keydown', that.keydownHandler);

            that.keyupHandler = function(e) {
                var key = window.event ? e.keyCode : e.which;
                if (!key || (key < 35 || key > 40) && key !== 13 && key !== 27) {
                    var val = that.value;
                    if (val.length >= opts.minChars) {
                        if (val !== that.last_val) {
                            that.last_val = val;
                            clearTimeout(that.timer);
                            if (opts.cache) {
                                if (val in that.cache) {
                                    suggest(that.cache[val]);
                                    return;
                                }
                                // no requests if previous suggestions were empty
                                for (var i = 1; i < val.length - opts.minChars; i++) {
                                    var part = val.slice(0, val.length - i);
                                    if (part in that.cache && !that.cache[part].length) {
                                        suggest([]);
                                        return;
                                    }
                                }
                            }
                            that.timer = setTimeout(opts.sourceData ? internalSource.bind(null, opts.sourceData, val, suggest) : opts.source.bind(this, val, suggest), opts.delay);
                        }
                    } else {
                        that.last_val = val;
                        that.sc.style.display = 'none';
                    }
                }
            };
            $.on(that, 'keyup', that.keyupHandler);

            that.focusHandler = function(e) {
                that.last_val = '\n';
                that.keyupHandler(e);
            };
            if (!opts.minChars)
                $.on(that, 'focus', that.focusHandler);
        }

        // public destroy method
        this.destroy = function() {
            for (var i = 0; i < elems.length; i++) {
                var that = elems[i];
                $.off(window, 'resize', that.updateSC);
                $.off(that, 'blur', that.blurHandler);
                $.off(that, 'focus', that.focusHandler);
                $.off(that, 'keydown', that.keydownHandler);
                $.off(that, 'keyup', that.keyupHandler);
                if (that.autocompleteAttr)
                    that.setAttribute('autocomplete', that.autocompleteAttr);
                else
                    that.removeAttribute('autocomplete');
                document.body.removeChild(that.sc);
                that = null;
            }
        };
    }

    return Autocomplete;
});
