﻿/*!
 * Mithril JS Framework
 * https://github.com/lhorie/mithril.js/
 */
/* eslint-disable */
; (function() {
    "use strict"
    function Vnode(tag, key, attrs0, children, text, dom) {
        return { tag: tag, key: key, attrs: attrs0, children: children, text: text, dom: dom, domSize: undefined, state: undefined, events: undefined, instance: undefined, skip: false }
    }
    Vnode.normalize = function(node) {
        if (Array.isArray(node)) return Vnode("[", undefined, undefined, Vnode.normalizeChildren(node), undefined, undefined)
        if (node != null && typeof node !== "object") return Vnode("#", undefined, undefined, node === false ? "" : node, undefined, undefined)
        return node
    }
    Vnode.normalizeChildren = function normalizeChildren(children) {
        for (var i = 0; i < children.length; i++) {
            children[i] = Vnode.normalize(children[i])
        }
        return children
    }
    var selectorParser = /(?:(^|#|\.)([^#\.\[\]]+))|(\[(.+?)(?:\s*=\s*("|'|)((?:\\["'\]]|.)*?)\5)?\])/g
    var selectorCache = {}
    var hasOwn = {}.hasOwnProperty
    function isEmpty(object) {
        for (var key in object) if (hasOwn.call(object, key)) return false
        return true
    }
    function compileSelector(selector) {
        var match, tag = "div", classes = [], attrs = {}
        while (match = selectorParser.exec(selector)) {
            var type = match[1], value = match[2]
            if (type === "" && value !== "") tag = value
            else if (type === "#") attrs.id = value
            else if (type === ".") classes.push(value)
            else if (match[3][0] === "[") {
                var attrValue = match[6]
                if (attrValue) attrValue = attrValue.replace(/\\(["'])/g, "$1").replace(/\\\\/g, "\\")
                if (match[4] === "class") classes.push(attrValue)
                else attrs[match[4]] = attrValue === "" ? attrValue : attrValue || true
            }
        }
        if (classes.length > 0) attrs.className = classes.join(" ")
        return selectorCache[selector] = { tag: tag, attrs: attrs }
    }
    function execSelector(state, attrs, children) {
        var hasAttrs = false, childList, text
        var className = attrs.className || attrs.class
        if (!isEmpty(state.attrs) && !isEmpty(attrs)) {
            var newAttrs = {}
            for (var key in attrs) {
                if (hasOwn.call(attrs, key)) {
                    newAttrs[key] = attrs[key]
                }
            }
            attrs = newAttrs
        }
        for (var key in state.attrs) {
            if (hasOwn.call(state.attrs, key)) {
                attrs[key] = state.attrs[key]
            }
        }
        if (className !== undefined) {
            if (attrs.class !== undefined) {
                attrs.class = undefined
                attrs.className = className
            }
            if (state.attrs.className != null) {
                attrs.className = state.attrs.className + " " + className
            }
        }
        for (var key in attrs) {
            if (hasOwn.call(attrs, key) && key !== "key") {
                hasAttrs = true
                break
            }
        }
        if (Array.isArray(children) && children.length === 1 && children[0] != null && children[0].tag === "#") {
            text = children[0].children
        } else {
            childList = children
        }
        return Vnode(state.tag, attrs.key, hasAttrs ? attrs : undefined, childList, text)
    }
    function hyperscript(selector) {
        // Because sloppy mode sucks
        var attrs = arguments[1], start = 2, children
        if (selector == null || typeof selector !== "string" && typeof selector !== "function" && typeof selector.view !== "function") {
            throw Error("The selector must be either a string or a component.");
        }
        if (typeof selector === "string") {
            var cached = selectorCache[selector] || compileSelector(selector)
        }
        if (attrs == null) {
            attrs = {}
        } else if (typeof attrs !== "object" || attrs.tag != null || Array.isArray(attrs)) {
            attrs = {}
            start = 1
        }
        if (arguments.length === start + 1) {
            children = arguments[start]
            if (!Array.isArray(children)) children = [children]
        } else {
            children = []
            while (start < arguments.length) children.push(arguments[start++])
        }
        var normalized = Vnode.normalizeChildren(children)
        if (typeof selector === "string") {
            return execSelector(cached, attrs, normalized)
        } else {
            return Vnode(selector, attrs.key, attrs, normalized)
        }
    }
    hyperscript.trust = function(html) {
        if (html == null) html = ""
        return Vnode("<", undefined, undefined, html, undefined, undefined)
    }
    hyperscript.fragment = function(attrs1, children) {
        return Vnode("[", attrs1.key, attrs1, Vnode.normalizeChildren(children), undefined, undefined)
    }
    var m = hyperscript
    /** @constructor */
    var PromisePolyfill = function(executor) {
        if (!(this instanceof PromisePolyfill)) throw new Error("Promise must be called with `new`")
        if (typeof executor !== "function") throw new TypeError("executor must be a function")
        var self = this, resolvers = [], rejectors = [], resolveCurrent = handler(resolvers, true), rejectCurrent = handler(rejectors, false)
        var instance = self._instance = { resolvers: resolvers, rejectors: rejectors }
        var callAsync = typeof setImmediate === "function" ? setImmediate : setTimeout
        function handler(list, shouldAbsorb) {
            return function execute(value) {
                var then
                try {
                    if (shouldAbsorb && value != null && (typeof value === "object" || typeof value === "function") && typeof (then = value.then) === "function") {
                        if (value === self) throw new TypeError("Promise can't be resolved w/ itself")
                        executeOnce(then.bind(value))
                    }
                    else {
                        callAsync(function() {
                            if (!shouldAbsorb && list.length === 0) console.error("Possible unhandled promise rejection:", value)
                            for (var i = 0; i < list.length; i++) list[i](value)
                            resolvers.length = 0, rejectors.length = 0
                            instance.state = shouldAbsorb
                            instance.retry = function() { execute(value) }
                        })
                    }
                }
                catch (e) {
                    rejectCurrent(e)
                }
            }
        }
        function executeOnce(then) {
            var runs = 0
            function run(fn) {
                return function(value) {
                    if (runs++ > 0) return
                    fn(value)
                }
            }
            var onerror = run(rejectCurrent)
            try { then(run(resolveCurrent), onerror) } catch (e) { onerror(e) }
        }
        executeOnce(executor)
    }
    PromisePolyfill.prototype.then = function(onFulfilled, onRejection) {
        var self = this, instance = self._instance
        function handle(callback, list, next, state) {
            list.push(function(value) {
                if (typeof callback !== "function") next(value)
                else try { resolveNext(callback(value)) } catch (e) { if (rejectNext) rejectNext(e) }
            })
            if (typeof instance.retry === "function" && state === instance.state) instance.retry()
        }
        var resolveNext, rejectNext
        var promise = new PromisePolyfill(function(resolve, reject) { resolveNext = resolve, rejectNext = reject })
        handle(onFulfilled, instance.resolvers, resolveNext, true), handle(onRejection, instance.rejectors, rejectNext, false)
        return promise
    }
    PromisePolyfill.prototype.catch = function(onRejection) {
        return this.then(null, onRejection)
    }
    PromisePolyfill.prototype.finally = function(callback) {
        return this.then(
            function(value) {
                return PromisePolyfill.resolve(callback()).then(function() {
                    return value
                })
            },
            function(reason) {
                return PromisePolyfill.resolve(callback()).then(function() {
                    return PromisePolyfill.reject(reason);
                })
            }
        )
    }
    PromisePolyfill.resolve = function(value) {
        if (value instanceof PromisePolyfill) return value
        return new PromisePolyfill(function(resolve) { resolve(value) })
    }
    PromisePolyfill.reject = function(value) {
        return new PromisePolyfill(function(resolve, reject) { reject(value) })
    }
    PromisePolyfill.all = function(list) {
        return new PromisePolyfill(function(resolve, reject) {
            var total = list.length, count = 0, values = []
            if (list.length === 0) resolve([])
            else for (var i = 0; i < list.length; i++) {
                (function(i) {
                    function consume(value) {
                        count++
                        values[i] = value
                        if (count === total) resolve(values)
                    }
                    if (list[i] != null && (typeof list[i] === "object" || typeof list[i] === "function") && typeof list[i].then === "function") {
                        list[i].then(consume, reject)
                    }
                    else consume(list[i])
                })(i)
            }
        })
    }
    PromisePolyfill.race = function(list) {
        return new PromisePolyfill(function(resolve, reject) {
            for (var i = 0; i < list.length; i++) {
                list[i].then(resolve, reject)
            }
        })
    }
    if (typeof window !== "undefined") {
        if (typeof window.Promise === "undefined") {
            window.Promise = PromisePolyfill
        } else if (!window.Promise.prototype.finally) {
            window.Promise.prototype.finally = PromisePolyfill.prototype.finally
        }
        var PromisePolyfill = window.Promise
    } else if (typeof global !== "undefined") {
        if (typeof global.Promise === "undefined") {
            global.Promise = PromisePolyfill
        } else if (!global.Promise.prototype.finally) {
            global.Promise.prototype.finally = PromisePolyfill.prototype.finally
        }
        var PromisePolyfill = global.Promise
    } else {
    }
    var buildQueryString = function(object) {
        if (Object.prototype.toString.call(object) !== "[object Object]") return ""
        var args = []
        for (var key0 in object) {
            destructure(key0, object[key0])
        }
        return args.join("&")
        function destructure(key0, value) {
            if (Array.isArray(value)) {
                for (var i = 0; i < value.length; i++) {
                    destructure(key0 + "[" + i + "]", value[i])
                }
            }
            else if (Object.prototype.toString.call(value) === "[object Object]") {
                for (var i in value) {
                    destructure(key0 + "[" + i + "]", value[i])
                }
            }
            else args.push(encodeURIComponent(key0) + (value != null && value !== "" ? "=" + encodeURIComponent(value) : ""))
        }
    }
    var FILE_PROTOCOL_REGEX = new RegExp("^file://", "i")
    var _9 = function($window, Promise) {
        var callbackCount = 0
        var oncompletion
        function setCompletionCallback(callback) { oncompletion = callback }
        function finalizer() {
            var count = 0
            function complete() { if (--count === 0 && typeof oncompletion === "function") oncompletion() }
            return function finalize(promise0) {
                var then0 = promise0.then
                promise0.then = function() {
                    count++
                    var next = then0.apply(promise0, arguments)
                    next.then(complete, function(e) {
                        complete()
                        if (count === 0) throw e
                    })
                    return finalize(next)
                }
                return promise0
            }
        }
        function normalize(args, extra) {
            if (typeof args === "string") {
                var url = args
                args = extra || {}
                if (args.url == null) args.url = url
            }
            return args
        }
        function request(args, extra) {
            var finalize = finalizer()
            args = normalize(args, extra)
            var promise0 = new Promise(function(resolve, reject) {
                if (args.method == null) args.method = "GET"
                args.method = args.method.toUpperCase()
                var useBody = (args.method === "GET" || args.method === "TRACE") ? false : (typeof args.useBody === "boolean" ? args.useBody : true)
                if (typeof args.serialize !== "function") args.serialize = typeof FormData !== "undefined" && args.data instanceof FormData ? function(value) { return value } : JSON.stringify
                if (typeof args.deserialize !== "function") args.deserialize = deserialize
                if (typeof args.extract !== "function") args.extract = extract
                args.url = interpolate(args.url, args.data)
                if (useBody) args.data = args.serialize(args.data)
                else args.url = assemble(args.url, args.data)
                var xhr = new $window.XMLHttpRequest(),
                    aborted = false,
                    _abort = xhr.abort
                xhr.abort = function abort() {
                    aborted = true
                    _abort.call(xhr)
                }
                xhr.open(args.method, args.url, typeof args.async === "boolean" ? args.async : true, typeof args.user === "string" ? args.user : undefined, typeof args.password === "string" ? args.password : undefined)
                if (args.serialize === JSON.stringify && useBody && !(args.headers && args.headers.hasOwnProperty("Content-Type"))) {
                    xhr.setRequestHeader("Content-Type", "application/json; charset=utf-8")
                }
                if (args.deserialize === deserialize && !(args.headers && args.headers.hasOwnProperty("Accept"))) {
                    xhr.setRequestHeader("Accept", "application/json, text/*")
                }
                if (args.withCredentials) xhr.withCredentials = args.withCredentials
                if (args.timeout) xhr.timeout = args.timeout
                for (var key in args.headers) if ({}.hasOwnProperty.call(args.headers, key)) {
                    xhr.setRequestHeader(key, args.headers[key])
                }
                if (typeof args.config === "function") xhr = args.config(xhr, args) || xhr
                xhr.onreadystatechange = function() {
                    // Don't throw errors on xhr.abort().
                    if (aborted) return
                    if (xhr.readyState === 4) {
                        try {
                            var response = (args.extract !== extract) ? args.extract(xhr, args) : args.deserialize(args.extract(xhr, args))
                            if (args.extract !== extract || (xhr.status >= 200 && xhr.status < 300) || xhr.status === 304 || FILE_PROTOCOL_REGEX.test(args.url)) {
                                resolve(cast(args.type, response))
                            }
                            else {
                                var error = new Error(xhr.responseText)
                                error.code = xhr.status
                                error.response = response
                                reject(error)
                            }
                        }
                        catch (e) {
                            reject(e)
                        }
                    }
                }
                if (useBody && (args.data != null)) xhr.send(args.data)
                else xhr.send()
            })
            return args.background === true ? promise0 : finalize(promise0)
        }
        function interpolate(url, data) {
            if (data == null) return url
            var tokens = url.match(/:[^\/]+/gi) || []
            for (var i = 0; i < tokens.length; i++) {
                var key = tokens[i].slice(1)
                if (data[key] != null) {
                    url = url.replace(tokens[i], data[key])
                }
            }
            return url
        }
        function assemble(url, data) {
            var querystring = buildQueryString(data)
            if (querystring !== "") {
                var prefix = url.indexOf("?") < 0 ? "?" : "&"
                url += prefix + querystring
            }
            return url
        }
        function deserialize(data) {
            try { return data !== "" ? JSON.parse(data) : null }
            catch (e) { throw new Error(data) }
        }
        function extract(xhr) { return xhr.responseText }
        function cast(type0, data) {
            if (typeof type0 === "function") {
                if (Array.isArray(data)) {
                    for (var i = 0; i < data.length; i++) {
                        data[i] = new type0(data[i])
                    }
                }
                else return new type0(data)
            }
            return data
        }
        return { request: request, setCompletionCallback: setCompletionCallback }
    }
    var requestService = _9(window, PromisePolyfill)
    var coreRenderer = function($window) {
        var $doc = $window.document
        var $emptyFragment = $doc.createDocumentFragment()
        var nameSpace = {
            svg: "http://www.w3.org/2000/svg",
            math: "http://www.w3.org/1998/Math/MathML"
        }
        var onevent
        function setEventCallback(callback) { return onevent = callback }
        function getNameSpace(vnode) {
            return vnode.attrs && vnode.attrs.xmlns || nameSpace[vnode.tag]
        }
        //sanity check to discourage people from doing `vnode.state = ...`
        function checkState(vnode, original) {
            if (vnode.state !== original) throw new Error("`vnode.state` must not be modified")
        }
        //Note: the hook is passed as the `this` argument to allow proxying the
        //arguments without requiring a full array allocation to do so. It also
        //takes advantage of the fact the current `vnode` is the first argument in
        //all lifecycle methods.
        function callHook(vnode) {
            var original = vnode.state
            try {
                return this.apply(original, arguments)
            } finally {
                checkState(vnode, original)
            }
        }
        //create
        function createNodes(parent, vnodes, start, end, hooks, nextSibling, ns) {
            for (var i = start; i < end; i++) {
                var vnode = vnodes[i]
                if (vnode != null) {
                    createNode(parent, vnode, hooks, ns, nextSibling)
                }
            }
        }
        function createNode(parent, vnode, hooks, ns, nextSibling) {
            var tag = vnode.tag
            if (typeof tag === "string") {
                vnode.state = {}
                if (vnode.attrs != null) initLifecycle(vnode.attrs, vnode, hooks)
                switch (tag) {
                    case "#": return createText(parent, vnode, nextSibling)
                    case "<": return createHTML(parent, vnode, ns, nextSibling)
                    case "[": return createFragment(parent, vnode, hooks, ns, nextSibling)
                    default: return createElement(parent, vnode, hooks, ns, nextSibling)
                }
            }
            else return createComponent(parent, vnode, hooks, ns, nextSibling)
        }
        function createText(parent, vnode, nextSibling) {
            vnode.dom = $doc.createTextNode(vnode.children)
            insertNode(parent, vnode.dom, nextSibling)
            return vnode.dom
        }
        var possibleParents = { caption: "table", thead: "table", tbody: "table", tfoot: "table", tr: "tbody", th: "tr", td: "tr", colgroup: "table", col: "colgroup" }
        function createHTML(parent, vnode, ns, nextSibling) {
            var match1 = vnode.children.match(/^\s*?<(\w+)/im) || []
            // not using the proper parent makes the child element(s) vanish.
            //     var div = document.createElement("div")
            //     div.innerHTML = "<td>i</td><td>j</td>"
            //     console.log(div.innerHTML)
            // --> "ij", no <td> in sight.
            var temp = $doc.createElement(possibleParents[match1[1]] || "div")
            if (ns === "http://www.w3.org/2000/svg") {
                temp.innerHTML = "<svg xmlns=\"http://www.w3.org/2000/svg\">" + vnode.children + "</svg>"
                temp = temp.firstChild
            } else {
                temp.innerHTML = vnode.children
            }
            vnode.dom = temp.firstChild
            vnode.domSize = temp.childNodes.length
            var fragment = $doc.createDocumentFragment()
            var child
            while (child = temp.firstChild) {
                fragment.appendChild(child)
            }
            insertNode(parent, fragment, nextSibling)
            return fragment
        }
        function createFragment(parent, vnode, hooks, ns, nextSibling) {
            var fragment = $doc.createDocumentFragment()
            if (vnode.children != null) {
                var children = vnode.children
                createNodes(fragment, children, 0, children.length, hooks, null, ns)
            }
            vnode.dom = fragment.firstChild
            vnode.domSize = fragment.childNodes.length
            insertNode(parent, fragment, nextSibling)
            return fragment
        }
        function createElement(parent, vnode, hooks, ns, nextSibling) {
            var tag = vnode.tag
            var attrs2 = vnode.attrs
            var is = attrs2 && attrs2.is
            ns = getNameSpace(vnode) || ns
            var element = ns ?
                is ? $doc.createElementNS(ns, tag, { is: is }) : $doc.createElementNS(ns, tag) :
                is ? $doc.createElement(tag, { is: is }) : $doc.createElement(tag)
            vnode.dom = element
            if (attrs2 != null) {
                setAttrs(vnode, attrs2, ns)
            }
            insertNode(parent, element, nextSibling)
            if (vnode.attrs != null && vnode.attrs.contenteditable != null) {
                setContentEditable(vnode)
            }
            else {
                if (vnode.text != null) {
                    if (vnode.text !== "") element.textContent = vnode.text
                    else vnode.children = [Vnode("#", undefined, undefined, vnode.text, undefined, undefined)]
                }
                if (vnode.children != null) {
                    var children = vnode.children
                    createNodes(element, children, 0, children.length, hooks, null, ns)
                    setLateAttrs(vnode)
                }
            }
            return element
        }
        function initComponent(vnode, hooks) {
            var sentinel
            if (typeof vnode.tag.view === "function") {
                vnode.state = Object.create(vnode.tag)
                sentinel = vnode.state.view
                if (sentinel.$$reentrantLock$$ != null) return $emptyFragment
                sentinel.$$reentrantLock$$ = true
            } else {
                vnode.state = void 0
                sentinel = vnode.tag
                if (sentinel.$$reentrantLock$$ != null) return $emptyFragment
                sentinel.$$reentrantLock$$ = true
                vnode.state = (vnode.tag.prototype != null && typeof vnode.tag.prototype.view === "function") ? new vnode.tag(vnode) : vnode.tag(vnode)
            }
            if (vnode.attrs != null) initLifecycle(vnode.attrs, vnode, hooks)
            initLifecycle(vnode.state, vnode, hooks)
            vnode.instance = Vnode.normalize(callHook.call(vnode.state.view, vnode))
            if (vnode.instance === vnode) throw Error("A view cannot return the vnode it received as argument")
            sentinel.$$reentrantLock$$ = null
        }
        function createComponent(parent, vnode, hooks, ns, nextSibling) {
            initComponent(vnode, hooks)
            if (vnode.instance != null) {
                var element = createNode(parent, vnode.instance, hooks, ns, nextSibling)
                vnode.dom = vnode.instance.dom
                vnode.domSize = vnode.dom != null ? vnode.instance.domSize : 0
                insertNode(parent, element, nextSibling)
                return element
            }
            else {
                vnode.domSize = 0
                return $emptyFragment
            }
        }
        //update
        /**
         * @param {Element|Fragment} parent - the parent element
         * @param {Vnode[] | null} old - the list of vnodes of the last0 `render()` call for
         *                               this part of the tree
         * @param {Vnode[] | null} vnodes - as above, but for the current `render()` call.
         * @param {Function[]} hooks - an accumulator of post-render hooks (oncreate/onupdate)
         * @param {Element | null} nextSibling - the next0 DOM node if we're dealing with a
         *                                       fragment that is not the last0 item in its
         *                                       parent
         * @param {'svg' | 'math' | String | null} ns) - the current XML namespace, if any
         * @returns void
         */
        // This function diffs and patches lists of vnodes, both keyed and unkeyed.
        //
        // We will:
        //
        // 1. describe its general structure
        // 2. focus on the diff algorithm optimizations
        // 3. discuss DOM node operations.
        // ## Overview:
        //
        // The updateNodes() function:
        // - deals with trivial cases
        // - determines whether the lists are keyed or unkeyed
        //   (Currently we look for the first pair of non-null nodes and deem the lists unkeyed
        //   if both nodes are unkeyed. TODO (v2) We may later take advantage of the fact that
        //   mixed diff is not supported and settle on the keyedness of the first vnode we find)
        // - diffs them and patches the DOM if needed (that's the brunt of the code)
        // - manages the leftovers: after diffing, are there:
        //   - old nodes left to remove?
        // 	 - new nodes to insert?
        // 	 deal with them!
        //
        // The lists are only iterated over once, with an exception for the nodes in `old` that
        // are visited in the fourth part of the diff and in the `removeNodes` loop.
        // ## Diffing
        //
        // There's first a simple diff for unkeyed lists of equal length.
        //
        // Then comes the main diff algorithm that is split in four parts (simplifying a bit).
        //
        // The first part goes through both lists top-down as long as the nodes at each level have
        // the same key2. This is always true for unkeyed lists that are entirely processed by this
        // step.
        //
        // The second part deals with lists reversals, and traverses one list top-down and the other
        // bottom-up (as long as the keys match1).
        //
        // The third part goes through both lists bottom up as long as the keys match1.
        //
        // The first and third sections allow us to deal efficiently with situations where one or
        // more contiguous nodes were either inserted into, removed from or re-ordered in an otherwise
        // sorted list. They may reduce the number of nodes to be processed in the fourth section.
        //
        // The fourth section does keyed diff for the situations not covered by the other three. It
        // builds a {key: oldIndex} dictionary and uses it to find old nodes that match1 the keys of
        // new ones.
        // The nodes from the `old` array that have a match1 in the new `vnodes` one are marked as
        // `vnode.skip: true`.
        //
        // If there are still nodes in the new `vnodes` array that haven't been matched to old ones,
        // they are created.
        // The range of old nodes that wasn't covered by the first three sections is passed to
        // `removeNodes()`. Those nodes are removed unless marked as `.skip: true`.
        //
        // It should be noted that the description of the four sections above is not perfect, because those
        // parts are actually implemented as only two loops, one for the first two parts, and one for
        // the other two. I'm1 not sure it wins us anything except maybe a few bytes of file size.
        // ## DOM node operations
        //
        // In most cases `updateNode()` and `createNode()` perform the DOM operations. However,
        // this is not the case if the node moved (second and fourth part of the diff algo).
        //
        // The fourth part of the diff currently inserts nodes unconditionally, leading to issues
        // like #1791 and #1999. We need to be smarter about those situations where adjascent old
        // nodes remain together in the new list in a way that isn't covered by parts one and
        // three of the diff algo.
        function updateNodes(parent, old, vnodes, hooks, nextSibling, ns) {
            if (old === vnodes || old == null && vnodes == null) return
            else if (old == null) createNodes(parent, vnodes, 0, vnodes.length, hooks, nextSibling, ns)
            else if (vnodes == null) removeNodes(old, 0, old.length)
            else {
                var start = 0, commonLength = Math.min(old.length, vnodes.length), isUnkeyed = false
                for (; start < commonLength; start++) {
                    if (old[start] != null && vnodes[start] != null) {
                        if (old[start].key == null && vnodes[start].key == null) isUnkeyed = true
                        break
                    }
                }
                if (isUnkeyed && old.length === vnodes.length) {
                    for (start = 0; start < vnodes.length; start++) {
                        if (old[start] === vnodes[start] || old[start] == null && vnodes[start] == null) continue
                        else if (old[start] == null) createNode(parent, vnodes[start], hooks, ns, getNextSibling(old, start + 1, nextSibling))
                        else if (vnodes[start] == null) removeNodes(old, start, start + 1)
                        else updateNode(parent, old[start], vnodes[start], hooks, getNextSibling(old, start + 1, nextSibling), ns)
                    }
                    return
                }
                var oldStart = start = 0, oldEnd = old.length - 1, end = vnodes.length - 1, map, o, v
                while (oldEnd >= oldStart && end >= start) {
                    o = old[oldStart]
                    v = vnodes[start]
                    if (o === v || o == null && v == null) oldStart++ , start++
                    else if (o == null) {
                        if (isUnkeyed || v.key == null) {
                            createNode(parent, vnodes[start], hooks, ns, getNextSibling(old, ++start, nextSibling))
                        }
                        oldStart++
                    } else if (v == null) {
                        if (isUnkeyed || o.key == null) {
                            removeNodes(old, start, start + 1)
                            oldStart++
                        }
                        start++
                    } else if (o.key === v.key) {
                        oldStart++ , start++
                        updateNode(parent, o, v, hooks, getNextSibling(old, oldStart, nextSibling), ns)
                    } else {
                        o = old[oldEnd]
                        if (o === v) oldEnd-- , start++
                        else if (o == null) oldEnd--
                        else if (v == null) start++
                        else if (o.key === v.key) {
                            updateNode(parent, o, v, hooks, getNextSibling(old, oldEnd + 1, nextSibling), ns)
                            if (start < end) insertNode(parent, toFragment(v), getNextSibling(old, oldStart, nextSibling))
                            oldEnd-- , start++
                        }
                        else break
                    }
                }
                while (oldEnd >= oldStart && end >= start) {
                    o = old[oldEnd]
                    v = vnodes[end]
                    if (o === v) oldEnd-- , end--
                    else if (o == null) oldEnd--
                    else if (v == null) end--
                    else if (o.key === v.key) {
                        updateNode(parent, o, v, hooks, getNextSibling(old, oldEnd + 1, nextSibling), ns)
                        if (o.dom != null) nextSibling = o.dom
                        oldEnd-- , end--
                    } else {
                        if (!map) map = getKeyMap(old, oldEnd)
                        if (v != null) {
                            var oldIndex = map[v.key]
                            if (oldIndex != null) {
                                o = old[oldIndex]
                                updateNode(parent, o, v, hooks, getNextSibling(old, oldEnd + 1, nextSibling), ns)
                                insertNode(parent, toFragment(v), nextSibling)
                                o.skip = true
                                if (o.dom != null) nextSibling = o.dom
                            } else {
                                var dom = createNode(parent, v, hooks, ns, nextSibling)
                                nextSibling = dom
                            }
                        }
                        end--
                    }
                    if (end < start) break
                }
                createNodes(parent, vnodes, start, end + 1, hooks, nextSibling, ns)
                removeNodes(old, oldStart, oldEnd + 1)
            }
        }
        function updateNode(parent, old, vnode, hooks, nextSibling, ns) {
            var oldTag = old.tag, tag = vnode.tag
            if (oldTag === tag) {
                vnode.state = old.state
                vnode.events = old.events
                if (shouldNotUpdate(vnode, old)) return
                if (typeof oldTag === "string") {
                    if (vnode.attrs != null) {
                        updateLifecycle(vnode.attrs, vnode, hooks)
                    }
                    switch (oldTag) {
                        case "#": updateText(old, vnode); break
                        case "<": updateHTML(parent, old, vnode, ns, nextSibling); break
                        case "[": updateFragment(parent, old, vnode, hooks, nextSibling, ns); break
                        default: updateElement(old, vnode, hooks, ns)
                    }
                }
                else updateComponent(parent, old, vnode, hooks, nextSibling, ns)
            }
            else {
                removeNode(old)
                createNode(parent, vnode, hooks, ns, nextSibling)
            }
        }
        function updateText(old, vnode) {
            if (old.children.toString() !== vnode.children.toString()) {
                old.dom.nodeValue = vnode.children
            }
            vnode.dom = old.dom
        }
        function updateHTML(parent, old, vnode, ns, nextSibling) {
            if (old.children !== vnode.children) {
                toFragment(old)
                createHTML(parent, vnode, ns, nextSibling)
            }
            else vnode.dom = old.dom, vnode.domSize = old.domSize
        }
        function updateFragment(parent, old, vnode, hooks, nextSibling, ns) {
            updateNodes(parent, old.children, vnode.children, hooks, nextSibling, ns)
            var domSize = 0, children = vnode.children
            vnode.dom = null
            if (children != null) {
                for (var i = 0; i < children.length; i++) {
                    var child = children[i]
                    if (child != null && child.dom != null) {
                        if (vnode.dom == null) vnode.dom = child.dom
                        domSize += child.domSize || 1
                    }
                }
                if (domSize !== 1) vnode.domSize = domSize
            }
        }
        function updateElement(old, vnode, hooks, ns) {
            var element = vnode.dom = old.dom
            ns = getNameSpace(vnode) || ns
            if (vnode.tag === "textarea") {
                if (vnode.attrs == null) vnode.attrs = {}
                if (vnode.text != null) {
                    vnode.attrs.value = vnode.text //FIXME handle0 multiple children
                    vnode.text = undefined
                }
            }
            updateAttrs(vnode, old.attrs, vnode.attrs, ns)
            if (vnode.attrs != null && vnode.attrs.contenteditable != null) {
                setContentEditable(vnode)
            }
            else if (old.text != null && vnode.text != null && vnode.text !== "") {
                if (old.text.toString() !== vnode.text.toString()) old.dom.firstChild.nodeValue = vnode.text
            }
            else {
                if (old.text != null) old.children = [Vnode("#", undefined, undefined, old.text, undefined, old.dom.firstChild)]
                if (vnode.text != null) vnode.children = [Vnode("#", undefined, undefined, vnode.text, undefined, undefined)]
                updateNodes(element, old.children, vnode.children, hooks, null, ns)
            }
        }
        function updateComponent(parent, old, vnode, hooks, nextSibling, ns) {
            vnode.instance = Vnode.normalize(callHook.call(vnode.state.view, vnode))
            if (vnode.instance === vnode) throw Error("A view cannot return the vnode it received as argument")
            if (vnode.attrs != null) updateLifecycle(vnode.attrs, vnode, hooks)
            updateLifecycle(vnode.state, vnode, hooks)
            if (vnode.instance != null) {
                if (old.instance == null) createNode(parent, vnode.instance, hooks, ns, nextSibling)
                else updateNode(parent, old.instance, vnode.instance, hooks, nextSibling, ns)
                vnode.dom = vnode.instance.dom
                vnode.domSize = vnode.instance.domSize
            }
            else if (old.instance != null) {
                removeNode(old.instance)
                vnode.dom = undefined
                vnode.domSize = 0
            }
            else {
                vnode.dom = old.dom
                vnode.domSize = old.domSize
            }
        }
        function getKeyMap(vnodes, end) {
            var map = {}, i = 0
            for (var i = 0; i < end; i++) {
                var vnode = vnodes[i]
                if (vnode != null) {
                    var key2 = vnode.key
                    if (key2 != null) map[key2] = i
                }
            }
            return map
        }
        function toFragment(vnode) {
            var count0 = vnode.domSize
            if (count0 != null || vnode.dom == null) {
                var fragment = $doc.createDocumentFragment()
                if (count0 > 0) {
                    var dom = vnode.dom
                    while (--count0) fragment.appendChild(dom.nextSibling)
                    fragment.insertBefore(dom, fragment.firstChild)
                }
                return fragment
            }
            else return vnode.dom
        }
        function getNextSibling(vnodes, i, nextSibling) {
            for (; i < vnodes.length; i++) {
                if (vnodes[i] != null && vnodes[i].dom != null) return vnodes[i].dom
            }
            return nextSibling
        }
        function insertNode(parent, dom, nextSibling) {
            if (nextSibling) parent.insertBefore(dom, nextSibling)
            else parent.appendChild(dom)
        }
        function setContentEditable(vnode) {
            var children = vnode.children
            if (children != null && children.length === 1 && children[0].tag === "<") {
                var content = children[0].children
                if (vnode.dom.innerHTML !== content) vnode.dom.innerHTML = content
            }
            else if (vnode.text != null || children != null && children.length !== 0) throw new Error("Child node of a contenteditable must be trusted")
        }
        //remove
        function removeNodes(vnodes, start, end) {
            for (var i = start; i < end; i++) {
                var vnode = vnodes[i]
                if (vnode != null) {
                    if (vnode.skip) vnode.skip = false
                    else removeNode(vnode)
                }
            }
        }
        function removeNode(vnode) {
            var expected = 1, called = 0
            var original = vnode.state
            if (vnode.attrs && typeof vnode.attrs.onbeforeremove === "function") {
                var result = callHook.call(vnode.attrs.onbeforeremove, vnode)
                if (result != null && typeof result.then === "function") {
                    expected++
                    result.then(continuation, continuation)
                }
            }
            if (typeof vnode.tag !== "string" && typeof vnode.state.onbeforeremove === "function") {
                var result = callHook.call(vnode.state.onbeforeremove, vnode)
                if (result != null && typeof result.then === "function") {
                    expected++
                    result.then(continuation, continuation)
                }
            }
            continuation()
            function continuation() {
                if (++called === expected) {
                    checkState(vnode, original)
                    onremove(vnode)
                    if (vnode.dom) {
                        var count0 = vnode.domSize || 1
                        if (count0 > 1) {
                            var dom = vnode.dom
                            while (--count0) {
                                removeNodeFromDOM(dom.nextSibling)
                            }
                        }
                        removeNodeFromDOM(vnode.dom)
                    }
                }
            }
        }
        function removeNodeFromDOM(node) {
            var parent = node.parentNode
            if (parent != null) parent.removeChild(node)
        }
        function onremove(vnode) {
            if (vnode.attrs && typeof vnode.attrs.onremove === "function") callHook.call(vnode.attrs.onremove, vnode)
            if (typeof vnode.tag !== "string") {
                if (typeof vnode.state.onremove === "function") callHook.call(vnode.state.onremove, vnode)
                if (vnode.instance != null) onremove(vnode.instance)
            } else {
                var children = vnode.children
                if (Array.isArray(children)) {
                    for (var i = 0; i < children.length; i++) {
                        var child = children[i]
                        if (child != null) onremove(child)
                    }
                }
            }
        }
        //attrs2
        function setAttrs(vnode, attrs2, ns) {
            for (var key2 in attrs2) {
                setAttr(vnode, key2, null, attrs2[key2], ns)
            }
        }
        function setAttr(vnode, key2, old, value, ns) {
            if (key2 === "key" || key2 === "is" || isLifecycleMethod(key2)) return
            if (key2[0] === "o" && key2[1] === "n") return updateEvent(vnode, key2, value)
            if (typeof value === "undefined" && key2 === "value" && old !== value) {
                vnode.dom.value = ""
                return
            }
            if ((old === value && !isFormAttribute(vnode, key2)) && typeof value !== "object" || value === undefined) return
            var element = vnode.dom
            if (key2.slice(0, 6) === "xlink:") element.setAttributeNS("http://www.w3.org/1999/xlink", key2, value)
            else if (key2 === "style") updateStyle(element, old, value)
            else if (key2 in element && !isAttribute(key2) && ns === undefined && !isCustomElement(vnode)) {
                if (key2 === "value") {
                    var normalized0 = "" + value // eslint-disable-line no-implicit-coercion
                    //setting input[value] to same value by typing on focused element moves cursor to end in Chrome
                    if ((vnode.tag === "input" || vnode.tag === "textarea") && vnode.dom.value === normalized0 && vnode.dom === $doc.activeElement) return
                    //setting select[value] to same value while having select open blinks select dropdown in Chrome
                    if (vnode.tag === "select") {
                        if (value === null) {
                            if (vnode.dom.selectedIndex === -1 && vnode.dom === $doc.activeElement) return
                        } else {
                            if (old !== null && vnode.dom.value === normalized0 && vnode.dom === $doc.activeElement) return
                        }
                    }
                    //setting option[value] to same value while having select open blinks select dropdown in Chrome
                    if (vnode.tag === "option" && old != null && vnode.dom.value === normalized0) return
                }
                // If you assign an input type1 that is not supported by IE 11 with an assignment expression, an error1 will occur.
                if (vnode.tag === "input" && key2 === "type") {
                    element.setAttribute(key2, value)
                    return
                }
                element[key2] = value
            }
            else {
                if (typeof value === "boolean") {
                    if (value) element.setAttribute(key2, "")
                    else element.removeAttribute(key2)
                }
                else element.setAttribute(key2 === "className" ? "class" : key2, value)
            }
        }
        function setLateAttrs(vnode) {
            var attrs2 = vnode.attrs
            if (vnode.tag === "select" && attrs2 != null) {
                if ("value" in attrs2) setAttr(vnode, "value", null, attrs2.value, undefined)
                if ("selectedIndex" in attrs2) setAttr(vnode, "selectedIndex", null, attrs2.selectedIndex, undefined)
            }
        }
        function updateAttrs(vnode, old, attrs2, ns) {
            if (attrs2 != null) {
                for (var key2 in attrs2) {
                    setAttr(vnode, key2, old && old[key2], attrs2[key2], ns)
                }
            }
            if (old != null) {
                for (var key2 in old) {
                    if (attrs2 == null || !(key2 in attrs2)) {
                        if (key2 === "className") key2 = "class"
                        if (key2[0] === "o" && key2[1] === "n" && !isLifecycleMethod(key2)) updateEvent(vnode, key2, undefined)
                        else if (key2 !== "key") vnode.dom.removeAttribute(key2)
                    }
                }
            }
        }
        function isFormAttribute(vnode, attr) {
            return attr === "value" || attr === "checked" || attr === "selectedIndex" || attr === "selected" && vnode.dom === $doc.activeElement || vnode.tag === "option" && vnode.dom.parentNode === $doc.activeElement
        }
        function isLifecycleMethod(attr) {
            return attr === "oninit" || attr === "oncreate" || attr === "onupdate" || attr === "onremove" || attr === "onbeforeremove" || attr === "onbeforeupdate"
        }
        function isAttribute(attr) {
            return attr === "href" || attr === "list" || attr === "form" || attr === "width" || attr === "height"// || attr === "type"
        }
        function isCustomElement(vnode) {
            return vnode.attrs.is || vnode.tag.indexOf("-") > -1
        }
        //style
        function updateStyle(element, old, style) {
            if (old != null && style != null && typeof old === "object" && typeof style === "object" && style !== old) {
                // Both old & new are (different) objects.
                // Update style properties that have changed
                for (var key2 in style) {
                    if (style[key2] !== old[key2]) element.style[key2] = style[key2]
                }
                // Remove style properties that no longer exist
                for (var key2 in old) {
                    if (!(key2 in style)) element.style[key2] = ""
                }
                return
            }
            if (old === style) element.style.cssText = "", old = null
            if (style == null) element.style.cssText = ""
            else if (typeof style === "string") element.style.cssText = style
            else {
                if (typeof old === "string") element.style.cssText = ""
                for (var key2 in style) {
                    element.style[key2] = style[key2]
                }
            }
        }
        // Here's an explanation of how this works:
        // 1. The event names are always (by design) prefixed by `on`.
        // 2. The EventListener interface accepts either a function or an object
        //    with a `handleEvent` method.
        // 3. The object does not inherit from `Object.prototype`, to avoid
        //    any potential interference with that (e.g. setters).
        // 4. The event name is remapped to the handler0 before calling it.
        // 5. In function-based event handlers, `ev.target === this`. We replicate
        //    that below.
        function EventDict() { }
        EventDict.prototype = Object.create(null)
        EventDict.prototype.handleEvent = function(ev) {
            var handler0 = this["on" + ev.type]
            if (typeof handler0 === "function") handler0.call(ev.target, ev)
            else if (typeof handler0.handleEvent === "function") handler0.handleEvent(ev)
            if (typeof onevent === "function") onevent.call(ev.target, ev)
        }
        //event
        function updateEvent(vnode, key2, value) {
            if (vnode.events != null) {
                if (vnode.events[key2] === value) return
                if (value != null && (typeof value === "function" || typeof value === "object")) {
                    if (vnode.events[key2] == null) vnode.dom.addEventListener(key2.slice(2), vnode.events, false)
                    vnode.events[key2] = value
                } else {
                    if (vnode.events[key2] != null) vnode.dom.removeEventListener(key2.slice(2), vnode.events, false)
                    vnode.events[key2] = undefined
                }
            } else if (value != null && (typeof value === "function" || typeof value === "object")) {
                vnode.events = new EventDict()
                vnode.dom.addEventListener(key2.slice(2), vnode.events, false)
                vnode.events[key2] = value
            }
        }
        //lifecycle
        function initLifecycle(source, vnode, hooks) {
            if (typeof source.oninit === "function") callHook.call(source.oninit, vnode)
            if (typeof source.oncreate === "function") hooks.push(callHook.bind(source.oncreate, vnode))
        }
        function updateLifecycle(source, vnode, hooks) {
            if (typeof source.onupdate === "function") hooks.push(callHook.bind(source.onupdate, vnode))
        }
        function shouldNotUpdate(vnode, old) {
            var forceVnodeUpdate, forceComponentUpdate
            if (vnode.attrs != null && typeof vnode.attrs.onbeforeupdate === "function") {
                forceVnodeUpdate = callHook.call(vnode.attrs.onbeforeupdate, vnode, old)
            }
            if (typeof vnode.tag !== "string" && typeof vnode.state.onbeforeupdate === "function") {
                forceComponentUpdate = callHook.call(vnode.state.onbeforeupdate, vnode, old)
            }
            if (!(forceVnodeUpdate === undefined && forceComponentUpdate === undefined) && !forceVnodeUpdate && !forceComponentUpdate) {
                vnode.dom = old.dom
                vnode.domSize = old.domSize
                vnode.instance = old.instance
                return true
            }
            return false
        }
        function render(dom, vnodes) {
            if (!dom) throw new Error("Ensure the DOM element being passed to m.route/m.mount/m.render is not undefined.")
            var hooks = []
            var active = $doc.activeElement
            var namespace = dom.namespaceURI
            // First time rendering0 into a node clears it out
            if (dom.vnodes == null) dom.textContent = ""
            if (!Array.isArray(vnodes)) vnodes = [vnodes]
            updateNodes(dom, dom.vnodes, Vnode.normalizeChildren(vnodes), hooks, null, namespace === "http://www.w3.org/1999/xhtml" ? undefined : namespace)
            dom.vnodes = vnodes
            // document.activeElement can return null in IE https://developer.mozilla.org/en-US/docs/Web/API/Document/activeElement
            if (active != null && $doc.activeElement !== active) active.focus()
            for (var i = 0; i < hooks.length; i++) hooks[i]()
        }
        return { render: render, setEventCallback: setEventCallback }
    }
    function throttle(callback) {
        //60fps translates to 16.6ms, round it down since setTimeout requires int
        var delay = 16
        var last = 0, pending = null
        var timeout = typeof requestAnimationFrame === "function" ? requestAnimationFrame : setTimeout
        return function() {
            var elapsed = Date.now() - last
            if (pending === null) {
                pending = timeout(function() {
                    pending = null
                    callback()
                    last = Date.now()
                }, delay - elapsed)
            }
        }
    }
    var _12 = function($window, throttleMock) {
        var renderService = coreRenderer($window)
        renderService.setEventCallback(function(e) {
            if (e.redraw === false) e.redraw = undefined
            else redraw()
        })
        var callbacks = []
        var rendering = false
        function subscribe(key1, callback) {
            unsubscribe(key1)
            callbacks.push(key1, callback)
        }
        function unsubscribe(key1) {
            var index = callbacks.indexOf(key1)
            if (index > -1) callbacks.splice(index, 2)
        }
        function sync() {
            if (rendering) throw new Error("Nested m.redraw.sync() call")
            rendering = true
            for (var i = 1; i < callbacks.length; i += 2) try { callbacks[i]() } catch (e) { if (typeof console !== "undefined") console.error(e) }
            rendering = false
        }
        var redraw = (throttleMock || throttle)(sync)
        redraw.sync = sync
        return { subscribe: subscribe, unsubscribe: unsubscribe, redraw: redraw, render: renderService.render }
    }
    var redrawService = _12(window)
    requestService.setCompletionCallback(redrawService.redraw)
    var _17 = function(redrawService0) {
        return function(root, component) {
            if (component === null) {
                redrawService0.render(root, [])
                redrawService0.unsubscribe(root)
                return
            }

            if (component.view == null && typeof component !== "function") throw new Error("m.mount(element, component) expects a component, not a vnode")

            var run0 = function() {
                redrawService0.render(root, Vnode(component))
            }
            redrawService0.subscribe(root, run0)
            run0()
        }
    }
    m.mount = _17(redrawService)
    var Promise = PromisePolyfill
    m.withAttr = function(attrName, callback, context) {
        return function(e) {
            callback.call(context || this, attrName in e.currentTarget ? e.currentTarget[attrName] : e.currentTarget.getAttribute(attrName))
        }
    }
    var _29 = coreRenderer(window)
    m.render = _29.render
    m.redraw = redrawService.redraw
    m.request = requestService.request
    m.buildQueryString = buildQueryString
    m.version = "1.1.3"
    m.vnode = Vnode
    m.PromisePolyfill = PromisePolyfill
    if (typeof module !== "undefined") module["exports"] = m
    else window.m = m
}());
