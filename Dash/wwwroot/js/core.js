!function(e){if("function"==typeof e.CustomEvent)return!1;function t(e,t){t=t||{bubbles:!1,cancelable:!1,detail:void 0};var n=document.createEvent("CustomEvent");return n.initCustomEvent(e,t.bubbles,t.cancelable,t.detail),n}t.prototype=e.Event.prototype,e.CustomEvent=t}(this),new function(){function e(e,t,n,r,o,i){return{tag:e,key:t,attrs:n,children:r,text:o,dom:i,domSize:void 0,state:void 0,_state:void 0,events:void 0,instance:void 0,skip:!1}}e.normalize=function(t){return Array.isArray(t)?e("[",void 0,void 0,e.normalizeChildren(t),void 0,void 0):null!=t&&"object"!=typeof t?e("#",void 0,void 0,!1===t?"":t,void 0,void 0):t},e.normalizeChildren=function(t){for(var n=0;n<t.length;n++)t[n]=e.normalize(t[n]);return t};var t=/(?:(^|#|\.)([^#\.\[\]]+))|(\[(.+?)(?:\s*=\s*("|'|)((?:\\["'\]]|.)*?)\5)?\])/g,n={},r={}.hasOwnProperty;function o(e){for(var t in e)if(r.call(e,t))return!1;return!0}function i(i){var a,l=arguments[1],s=2;if(null==i||"string"!=typeof i&&"function"!=typeof i&&"function"!=typeof i.view)throw Error("The selector must be either a string or a component.");if("string"==typeof i)var u=n[i]||function(e){for(var r,o="div",i=[],a={};r=t.exec(e);){var l=r[1],s=r[2];if(""===l&&""!==s)o=s;else if("#"===l)a.id=s;else if("."===l)i.push(s);else if("["===r[3][0]){var u=r[6];u&&(u=u.replace(/\\(["'])/g,"$1").replace(/\\\\/g,"\\")),"class"===r[4]?i.push(u):a[r[4]]=""===u?u:u||!0}}return i.length>0&&(a.className=i.join(" ")),n[e]={tag:o,attrs:a}}(i);if(null==l?l={}:("object"!=typeof l||null!=l.tag||Array.isArray(l))&&(l={},s=1),arguments.length===s+1)a=arguments[s],Array.isArray(a)||(a=[a]);else for(a=[];s<arguments.length;)a.push(arguments[s++]);var c=e.normalizeChildren(a);return"string"==typeof i?function(t,n,i){var a,l,s=!1,u=n.className||n.class;if(!o(t.attrs)&&!o(n)){var c={};for(var f in n)r.call(n,f)&&(c[f]=n[f]);n=c}for(var f in t.attrs)r.call(t.attrs,f)&&(n[f]=t.attrs[f]);for(var f in void 0!==u&&(void 0!==n.class&&(n.class=void 0,n.className=u),null!=t.attrs.className&&(n.className=t.attrs.className+" "+u)),n)if(r.call(n,f)&&"key"!==f){s=!0;break}return Array.isArray(i)&&1===i.length&&null!=i[0]&&"#"===i[0].tag?l=i[0].children:a=i,e(t.tag,n.key,s?n:void 0,a,l)}(u,l,c):e(i,l.key,l,c)}i.trust=function(t){return null==t&&(t=""),e("<",void 0,void 0,t,void 0,void 0)},i.fragment=function(t,n){return e("[",t.key,t,e.normalizeChildren(n),void 0,void 0)};var a=i;if((l=function(e){if(!(this instanceof l))throw new Error("Promise must be called with `new`");if("function"!=typeof e)throw new TypeError("executor must be a function");var t=this,n=[],r=[],o=u(n,!0),i=u(r,!1),a=t._instance={resolvers:n,rejectors:r},s="function"==typeof setImmediate?setImmediate:setTimeout;function u(e,o){return function l(u){var f;try{if(!o||null==u||"object"!=typeof u&&"function"!=typeof u||"function"!=typeof(f=u.then))s(function(){o||0!==e.length||console.error("Possible unhandled promise rejection:",u);for(var t=0;t<e.length;t++)e[t](u);n.length=0,r.length=0,a.state=o,a.retry=function(){l(u)}});else{if(u===t)throw new TypeError("Promise can't be resolved w/ itself");c(f.bind(u))}}catch(e){i(e)}}}function c(e){var t=0;function n(e){return function(n){t++>0||e(n)}}var r=n(i);try{e(n(o),r)}catch(e){r(e)}}c(e)}).prototype.then=function(e,t){var n,r,o=this._instance;function i(e,t,i,a){t.push(function(t){if("function"!=typeof e)i(t);else try{n(e(t))}catch(e){r&&r(e)}}),"function"==typeof o.retry&&a===o.state&&o.retry()}var a=new l(function(e,t){n=e,r=t});return i(e,o.resolvers,n,!0),i(t,o.rejectors,r,!1),a},l.prototype.catch=function(e){return this.then(null,e)},l.resolve=function(e){return e instanceof l?e:new l(function(t){t(e)})},l.reject=function(e){return new l(function(t,n){n(e)})},l.all=function(e){return new l(function(t,n){var r=e.length,o=0,i=[];if(0===e.length)t([]);else for(var a=0;a<e.length;a++)!function(a){function l(e){o++,i[a]=e,o===r&&t(i)}null==e[a]||"object"!=typeof e[a]&&"function"!=typeof e[a]||"function"!=typeof e[a].then?l(e[a]):e[a].then(l,n)}(a)})},l.race=function(e){return new l(function(t,n){for(var r=0;r<e.length;r++)e[r].then(t,n)})},"undefined"!=typeof window){void 0===window.Promise&&(window.Promise=l);var l=window.Promise}else if("undefined"!=typeof global){void 0===global.Promise&&(global.Promise=l);l=global.Promise}var s=function(e){if("[object Object]"!==Object.prototype.toString.call(e))return"";var t=[];for(var n in e)r(n,e[n]);return t.join("&");function r(e,n){if(Array.isArray(n))for(var o=0;o<n.length;o++)r(e+"["+o+"]",n[o]);else if("[object Object]"===Object.prototype.toString.call(n))for(var o in n)r(e+"["+o+"]",n[o]);else t.push(encodeURIComponent(e)+(null!=n&&""!==n?"="+encodeURIComponent(n):""))}},u=new RegExp("^file://","i"),c=function(e,t){var n,r=0;function o(){var e=0;function t(){0==--e&&"function"==typeof n&&n()}return function n(r){var o=r.then;return r.then=function(){e++;var i=o.apply(r,arguments);return i.then(t,function(n){if(t(),0===e)throw n}),n(i)},r}}function i(e,t){if("string"==typeof e){var n=e;null==(e=t||{}).url&&(e.url=n)}return e}function a(e,t){if(null==t)return e;for(var n=e.match(/:[^\/]+/gi)||[],r=0;r<n.length;r++){var o=n[r].slice(1);null!=t[o]&&(e=e.replace(n[r],t[o]))}return e}function l(e,t){var n=s(t);return""!==n&&(e+=(e.indexOf("?")<0?"?":"&")+n),e}function c(e){try{return""!==e?JSON.parse(e):null}catch(t){throw new Error(e)}}function f(e){return e.responseText}function d(e,t){if("function"==typeof e){if(!Array.isArray(t))return new e(t);for(var n=0;n<t.length;n++)t[n]=new e(t[n])}return t}return{request:function(n,r){var s=o();n=i(n,r);var h=new t(function(t,r){null==n.method&&(n.method="GET"),n.method=n.method.toUpperCase();var o="GET"!==n.method&&"TRACE"!==n.method&&("boolean"!=typeof n.useBody||n.useBody);"function"!=typeof n.serialize&&(n.serialize="undefined"!=typeof FormData&&n.data instanceof FormData?function(e){return e}:JSON.stringify),"function"!=typeof n.deserialize&&(n.deserialize=c),"function"!=typeof n.extract&&(n.extract=f),n.url=a(n.url,n.data),o?n.data=n.serialize(n.data):n.url=l(n.url,n.data);var i=new e.XMLHttpRequest,s=!1,h=i.abort;for(var v in i.abort=function(){s=!0,h.call(i)},i.open(n.method,n.url,"boolean"!=typeof n.async||n.async,"string"==typeof n.user?n.user:void 0,"string"==typeof n.password?n.password:void 0),n.serialize!==JSON.stringify||!o||n.headers&&n.headers.hasOwnProperty("Content-Type")||i.setRequestHeader("Content-Type","application/json; charset=utf-8"),n.deserialize!==c||n.headers&&n.headers.hasOwnProperty("Accept")||i.setRequestHeader("Accept","application/json, text/*"),n.withCredentials&&(i.withCredentials=n.withCredentials),n.headers)({}).hasOwnProperty.call(n.headers,v)&&i.setRequestHeader(v,n.headers[v]);"function"==typeof n.config&&(i=n.config(i,n)||i),i.onreadystatechange=function(){if(!s&&4===i.readyState)try{var e=n.extract!==f?n.extract(i,n):n.deserialize(n.extract(i,n));if(i.status>=200&&i.status<300||304===i.status||u.test(n.url))t(d(n.type,e));else{var o=new Error(i.responseText);for(var a in e)o[a]=e[a];r(o)}}catch(e){r(e)}},o&&null!=n.data?i.send(n.data):i.send()});return!0===n.background?h:s(h)},jsonp:function(n,s){var u=o();n=i(n,s);var c=new t(function(t,o){var i=n.callbackName||"_mithril_"+Math.round(1e16*Math.random())+"_"+r++,s=e.document.createElement("script");e[i]=function(r){s.parentNode.removeChild(s),t(d(n.type,r)),delete e[i]},s.onerror=function(){s.parentNode.removeChild(s),o(new Error("JSONP request failed")),delete e[i]},null==n.data&&(n.data={}),n.url=a(n.url,n.data),n.data[n.callbackKey||"callback"]=i,s.src=l(n.url,n.data),e.document.documentElement.appendChild(s)});return!0===n.background?c:u(c)},setCompletionCallback:function(e){n=e}}}(window,l),f=function(t){var n,r=t.document,o=r.createDocumentFragment(),i={svg:"http://www.w3.org/2000/svg",math:"http://www.w3.org/1998/Math/MathML"};function a(e){return e.attrs&&e.attrs.xmlns||i[e.tag]}function l(e,t,n,r,o,i,a){for(var l=n;l<r;l++){var u=t[l];null!=u&&s(e,u,o,a,i)}}function s(t,n,i,f,d){var h,v,p,y=n.tag;if("string"!=typeof y)return function(e,t,n,r,i){{if(c(t,n),null!=t.instance){var a=s(e,t.instance,n,r,i);return t.dom=t.instance.dom,t.domSize=null!=t.dom?t.instance.domSize:0,m(e,a,i),a}return t.domSize=0,o}}(t,n,i,f,d);switch(n.state={},null!=n.attrs&&E(n.attrs,n,i),y){case"#":return h=t,p=d,(v=n).dom=r.createTextNode(v.children),m(h,v.dom,p),v.dom;case"<":return u(t,n,d);case"[":return function(e,t,n,o,i){var a=r.createDocumentFragment();if(null!=t.children){var s=t.children;l(a,s,0,s.length,n,null,o)}return t.dom=a.firstChild,t.domSize=a.childNodes.length,m(e,a,i),a}(t,n,i,f,d);default:return function(t,n,o,i,s){var u=n.tag,c=n.attrs,f=c&&c.is,d=(i=a(n)||i)?f?r.createElementNS(i,u,{is:f}):r.createElementNS(i,u):f?r.createElement(u,{is:f}):r.createElement(u);n.dom=d,null!=c&&function(e,t,n){for(var r in t)x(e,r,null,t[r],n)}(n,c,i);if(m(t,d,s),null!=n.attrs&&null!=n.attrs.contenteditable)g(n);else if(null!=n.text&&(""!==n.text?d.textContent=n.text:n.children=[e("#",void 0,void 0,n.text,void 0,void 0)]),null!=n.children){var h=n.children;l(d,h,0,h.length,o,null,i),p=(v=n).attrs,"select"===v.tag&&null!=p&&("value"in p&&x(v,"value",null,p.value,void 0),"selectedIndex"in p&&x(v,"selectedIndex",null,p.selectedIndex,void 0))}var v,p;return d}(t,n,i,f,d)}}function u(e,t,n){var o={caption:"table",thead:"table",tbody:"table",tfoot:"table",tr:"tbody",th:"tr",td:"tr",colgroup:"table",col:"colgroup"}[(t.children.match(/^\s*?<(\w+)/im)||[])[1]]||"div",i=r.createElement(o);i.innerHTML=t.children,t.dom=i.firstChild,t.domSize=i.childNodes.length;for(var a,l=r.createDocumentFragment();a=i.firstChild;)l.appendChild(a);return m(e,l,n),l}function c(t,n){var r;if("function"==typeof t.tag.view){if(t.state=Object.create(t.tag),null!=(r=t.state.view).$$reentrantLock$$)return o;r.$$reentrantLock$$=!0}else{if(t.state=void 0,null!=(r=t.tag).$$reentrantLock$$)return o;r.$$reentrantLock$$=!0,t.state=null!=t.tag.prototype&&"function"==typeof t.tag.prototype.view?new t.tag(t):t.tag(t)}if(t._state=t.state,null!=t.attrs&&E(t.attrs,t,n),E(t._state,t,n),t.instance=e.normalize(t._state.view.call(t.state,t)),t.instance===t)throw Error("A view cannot return the vnode it received as argument");r.$$reentrantLock$$=null}function f(e,t,n,r,o,i,a){if(t!==n&&(null!=t||null!=n))if(null==t)l(e,n,0,n.length,o,i,a);else if(null==n)y(t,0,t.length,n);else{if(t.length===n.length){for(var u=!1,c=0;c<n.length;c++)if(null!=n[c]&&null!=t[c]){u=null==n[c].key&&null==t[c].key;break}if(u){for(c=0;c<t.length;c++)t[c]!==n[c]&&(null==t[c]&&null!=n[c]?s(e,n[c],o,a,p(t,c+1,i)):null==n[c]?y(t,c,c+1,n):d(e,t[c],n[c],o,p(t,c+1,i),r,a));return}}if(r=r||function(e,t){if(null!=e.pool&&Math.abs(e.pool.length-t.length)<=Math.abs(e.length-t.length)){var n=e[0]&&e[0].children&&e[0].children.length||0,r=e.pool[0]&&e.pool[0].children&&e.pool[0].children.length||0,o=t[0]&&t[0].children&&t[0].children.length||0;if(Math.abs(r-o)<=Math.abs(n-o))return!0}return!1}(t,n)){var f=t.pool;t=t.concat(t.pool)}for(var g,b=0,w=0,x=t.length-1,k=n.length-1;x>=b&&k>=w;){if((E=t[b])!==(L=n[w])||r)if(null==E)b++;else if(null==L)w++;else if(E.key===L.key){var C=null!=f&&b>=t.length-f.length||null==f&&r;w++,d(e,E,L,o,p(t,++b,i),C,a),r&&E.tag===L.tag&&m(e,v(E),i)}else{if((E=t[x])!==L||r)if(null==E)x--;else if(null==L)w++;else{if(E.key!==L.key)break;C=null!=f&&x>=t.length-f.length||null==f&&r;d(e,E,L,o,p(t,x+1,i),C,a),(r||w<k)&&m(e,v(E),p(t,b,i)),x--,w++}else x--,w++}else b++,w++}for(;x>=b&&k>=w;){var E,L;if((E=t[x])!==(L=n[k])||r)if(null==E)x--;else if(null==L)k--;else if(E.key===L.key){C=null!=f&&x>=t.length-f.length||null==f&&r;d(e,E,L,o,p(t,x+1,i),C,a),r&&E.tag===L.tag&&m(e,v(E),i),null!=E.dom&&(i=E.dom),x--,k--}else{if(g||(g=h(t,x)),null!=L){var S=g[L.key];if(null!=S){var A=t[S];C=null!=f&&S>=t.length-f.length||null==f&&r;d(e,A,L,o,p(t,x+1,i),r,a),m(e,v(A),i),t[S].skip=!0,null!=A.dom&&(i=A.dom)}else{i=s(e,L,o,a,i)}}k--}else x--,k--;if(k<w)break}l(e,n,w,k+1,o,i,a),y(t,b,x+1,n)}}function d(t,n,r,o,i,l,h){var p,m,y,w,S=n.tag;if(S===r.tag){if(r.state=n.state,r._state=n._state,r.events=n.events,!l&&function(e,t){var n,r;null!=e.attrs&&"function"==typeof e.attrs.onbeforeupdate&&(n=e.attrs.onbeforeupdate.call(e.state,e,t));"string"!=typeof e.tag&&"function"==typeof e._state.onbeforeupdate&&(r=e._state.onbeforeupdate.call(e.state,e,t));if(!(void 0===n&&void 0===r||n||r))return e.dom=t.dom,e.domSize=t.domSize,e.instance=t.instance,!0;return!1}(r,n))return;if("string"==typeof S)switch(null!=r.attrs&&(l?(r.state={},E(r.attrs,r,o)):L(r.attrs,r,o)),S){case"#":!function(e,t){e.children.toString()!==t.children.toString()&&(e.dom.nodeValue=t.children);t.dom=e.dom}(n,r);break;case"<":p=t,y=r,w=i,(m=n).children!==y.children?(v(m),u(p,y,w)):(y.dom=m.dom,y.domSize=m.domSize);break;case"[":!function(e,t,n,r,o,i,a){f(e,t.children,n.children,r,o,i,a);var l=0,s=n.children;if(n.dom=null,null!=s){for(var u=0;u<s.length;u++){var c=s[u];null!=c&&null!=c.dom&&(null==n.dom&&(n.dom=c.dom),l+=c.domSize||1)}1!==l&&(n.domSize=l)}}(t,n,r,l,o,i,h);break;default:!function(t,n,r,o,i){var l=n.dom=t.dom;i=a(n)||i,"textarea"===n.tag&&(null==n.attrs&&(n.attrs={}),null!=n.text&&(n.attrs.value=n.text,n.text=void 0));(function(e,t,n,r){if(null!=n)for(var o in n)x(e,o,t&&t[o],n[o],r);if(null!=t)for(var o in t)null!=n&&o in n||("className"===o&&(o="class"),"o"!==o[0]||"n"!==o[1]||k(o)?"key"!==o&&e.dom.removeAttribute(o):C(e,o,void 0))})(n,t.attrs,n.attrs,i),null!=n.attrs&&null!=n.attrs.contenteditable?g(n):null!=t.text&&null!=n.text&&""!==n.text?t.text.toString()!==n.text.toString()&&(t.dom.firstChild.nodeValue=n.text):(null!=t.text&&(t.children=[e("#",void 0,void 0,t.text,void 0,t.dom.firstChild)]),null!=n.text&&(n.children=[e("#",void 0,void 0,n.text,void 0,void 0)]),f(l,t.children,n.children,r,o,null,i))}(n,r,l,o,h)}else!function(t,n,r,o,i,a,l){if(a)c(r,o);else{if(r.instance=e.normalize(r._state.view.call(r.state,r)),r.instance===r)throw Error("A view cannot return the vnode it received as argument");null!=r.attrs&&L(r.attrs,r,o),L(r._state,r,o)}null!=r.instance?(null==n.instance?s(t,r.instance,o,l,i):d(t,n.instance,r.instance,o,i,a,l),r.dom=r.instance.dom,r.domSize=r.instance.domSize):null!=n.instance?(b(n.instance,null),r.dom=void 0,r.domSize=0):(r.dom=n.dom,r.domSize=n.domSize)}(t,n,r,o,i,l,h)}else b(n,null),s(t,r,o,h,i)}function h(e,t){var n={},r=0;for(r=0;r<t;r++){var o=e[r];if(null!=o){var i=o.key;null!=i&&(n[i]=r)}}return n}function v(e){var t=e.domSize;if(null!=t||null==e.dom){var n=r.createDocumentFragment();if(t>0){for(var o=e.dom;--t;)n.appendChild(o.nextSibling);n.insertBefore(o,n.firstChild)}return n}return e.dom}function p(e,t,n){for(;t<e.length;t++)if(null!=e[t]&&null!=e[t].dom)return e[t].dom;return n}function m(e,t,n){n&&n.parentNode?e.insertBefore(t,n):e.appendChild(t)}function g(e){var t=e.children;if(null!=t&&1===t.length&&"<"===t[0].tag){var n=t[0].children;e.dom.innerHTML!==n&&(e.dom.innerHTML=n)}else if(null!=e.text||null!=t&&0!==t.length)throw new Error("Child node of a contenteditable must be trusted")}function y(e,t,n,r){for(var o=t;o<n;o++){var i=e[o];null!=i&&(i.skip?i.skip=!1:b(i,r))}}function b(e,t){var n,r=1,o=0;e.attrs&&"function"==typeof e.attrs.onbeforeremove&&(null!=(n=e.attrs.onbeforeremove.call(e.state,e))&&"function"==typeof n.then&&(r++,n.then(i,i)));"string"!=typeof e.tag&&"function"==typeof e._state.onbeforeremove&&(null!=(n=e._state.onbeforeremove.call(e.state,e))&&"function"==typeof n.then&&(r++,n.then(i,i)));function i(){if(++o===r&&(function e(t){t.attrs&&"function"==typeof t.attrs.onremove&&t.attrs.onremove.call(t.state,t);if("string"!=typeof t.tag)"function"==typeof t._state.onremove&&t._state.onremove.call(t.state,t),null!=t.instance&&e(t.instance);else{var n=t.children;if(Array.isArray(n))for(var r=0;r<n.length;r++){var o=n[r];null!=o&&e(o)}}}(e),e.dom)){var n=e.domSize||1;if(n>1)for(var i=e.dom;--n;)w(i.nextSibling);w(e.dom),null==t||null!=e.domSize||null!=(a=e.attrs)&&(a.oncreate||a.onupdate||a.onbeforeremove||a.onremove)||"string"!=typeof e.tag||(t.pool?t.pool.push(e):t.pool=[e])}var a}i()}function w(e){var t=e.parentNode;null!=t&&t.removeChild(e)}function x(e,t,n,o,i){var a=e.dom;if("key"!==t&&"is"!==t&&(n!==o||(l=e,"value"===(s=t)||"checked"===s||"selectedIndex"===s||"selected"===s&&l.dom===r.activeElement)||"object"==typeof o)&&void 0!==o&&!k(t)){var l,s,u,c,f=t.indexOf(":");if(f>-1&&"xlink"===t.substr(0,f))a.setAttributeNS("http://www.w3.org/1999/xlink",t.slice(f+1),o);else if("o"===t[0]&&"n"===t[1]&&"function"==typeof o)C(e,t,o);else if("style"===t)!function(e,t,n){t===n&&(e.style.cssText="",t=null);if(null==n)e.style.cssText="";else if("string"==typeof n)e.style.cssText=n;else{for(var r in"string"==typeof t&&(e.style.cssText=""),n)e.style[r]=n[r];if(null!=t&&"string"!=typeof t)for(var r in t)r in n||(e.style[r]="")}}(a,n,o);else if(t in a&&("href"!==(c=t)&&"list"!==c&&"form"!==c&&"width"!==c&&"height"!==c)&&void 0===i&&!((u=e).attrs.is||u.tag.indexOf("-")>-1)){if("value"===t){var d=""+o;if(("input"===e.tag||"textarea"===e.tag)&&e.dom.value===d&&e.dom===r.activeElement)return;if("select"===e.tag)if(null===o){if(-1===e.dom.selectedIndex&&e.dom===r.activeElement)return}else if(null!==n&&e.dom.value===d&&e.dom===r.activeElement)return;if("option"===e.tag&&null!=n&&e.dom.value===d)return}if("input"===e.tag&&"type"===t)return void a.setAttribute(t,o);a[t]=o}else"boolean"==typeof o?o?a.setAttribute(t,""):a.removeAttribute(t):a.setAttribute("className"===t?"class":t,o)}}function k(e){return"oninit"===e||"oncreate"===e||"onupdate"===e||"onremove"===e||"onbeforeremove"===e||"onbeforeupdate"===e}function C(e,t,r){var o=e.dom,i="function"!=typeof n?r:function(e){var t=r.call(o,e);return n.call(o,e),t};if(t in o)o[t]="function"==typeof r?i:null;else{var a=t.slice(2);if(void 0===e.events&&(e.events={}),e.events[t]===i)return;null!=e.events[t]&&o.removeEventListener(a,e.events[t],!1),"function"==typeof r&&(e.events[t]=i,o.addEventListener(a,e.events[t],!1))}}function E(e,t,n){"function"==typeof e.oninit&&e.oninit.call(t.state,t),"function"==typeof e.oncreate&&n.push(e.oncreate.bind(t.state,t))}function L(e,t,n){"function"==typeof e.onupdate&&n.push(e.onupdate.bind(t.state,t))}return{render:function(t,n){if(!t)throw new Error("Ensure the DOM element being passed to m.route/m.mount/m.render is not undefined.");var o=[],i=r.activeElement,a=t.namespaceURI;null==t.vnodes&&(t.textContent=""),Array.isArray(n)||(n=[n]),f(t,t.vnodes,e.normalizeChildren(n),!1,o,null,"http://www.w3.org/1999/xhtml"===a?void 0:a),t.vnodes=n,null!=i&&r.activeElement!==i&&i.focus();for(var l=0;l<o.length;l++)o[l]()},setEventCallback:function(e){return n=e}}};var d=function(e){var t=f(e);t.setEventCallback(function(e){!1===e.redraw?e.redraw=void 0:o()});var n=[];function r(e){var t=n.indexOf(e);t>-1&&n.splice(t,2)}function o(){for(var e=1;e<n.length;e+=2)n[e]()}return{subscribe:function(e,t){var o,i,a,l;r(e),n.push(e,(o=t,i=0,a=null,l="function"==typeof requestAnimationFrame?requestAnimationFrame:setTimeout,function(){var e=Date.now();0===i||e-i>=16?(i=e,o()):null===a&&(a=l(function(){a=null,o(),i=Date.now()},16-(e-i)))}))},unsubscribe:r,redraw:o,render:t.render}}(window);c.setCompletionCallback(d.redraw);var h;a.mount=(h=d,function(t,n){if(null===n)return h.render(t,[]),void h.unsubscribe(t);if(null==n.view&&"function"!=typeof n)throw new Error("m.mount(element, component) expects a component, not a vnode");h.subscribe(t,function(){h.render(t,e(n))}),h.redraw()});var v,p,m,g,y,b,w,x,k,C=l,E=function(e){if(""===e||null==e)return{};"?"===e.charAt(0)&&(e=e.slice(1));for(var t=e.split("&"),n={},r={},o=0;o<t.length;o++){var i=t[o].split("="),a=decodeURIComponent(i[0]),l=2===i.length?decodeURIComponent(i[1]):"";"true"===l?l=!0:"false"===l&&(l=!1);var s=a.split(/\]\[?|\[/),u=n;a.indexOf("[")>-1&&s.pop();for(var c=0;c<s.length;c++){var f=s[c],d=s[c+1],h=""==d||!isNaN(parseInt(d,10)),v=c===s.length-1;if(""===f)null==r[a=s.slice(0,c).join()]&&(r[a]=0),f=r[a]++;null==u[f]&&(u[f]=v?l:h?[]:{}),u=u[f]}}return n},L=function(e){var t,n="function"==typeof e.history.pushState,r="function"==typeof setImmediate?setImmediate:setTimeout;function o(t){var n=e.location[t].replace(/(?:%[a-f89][a-f0-9])+/gim,decodeURIComponent);return"pathname"===t&&"/"!==n[0]&&(n="/"+n),n}function i(e,t,n){var r=e.indexOf("?"),o=e.indexOf("#"),i=r>-1?r:o>-1?o:e.length;if(r>-1){var a=o>-1?o:e.length,l=E(e.slice(r+1,a));for(var s in l)t[s]=l[s]}if(o>-1){var u=E(e.slice(o+1));for(var s in u)n[s]=u[s]}return e.slice(0,i)}var a={prefix:"#!",getPath:function(){switch(a.prefix.charAt(0)){case"#":return o("hash").slice(a.prefix.length);case"?":return o("search").slice(a.prefix.length)+o("hash");default:return o("pathname").slice(a.prefix.length)+o("search")+o("hash")}},setPath:function(t,r,o){var l={},u={};if(t=i(t,l,u),null!=r){for(var c in r)l[c]=r[c];t=t.replace(/:([^\/]+)/g,function(e,t){return delete l[t],r[t]})}var f=s(l);f&&(t+="?"+f);var d=s(u);if(d&&(t+="#"+d),n){var h=o?o.state:null,v=o?o.title:null;e.onpopstate(),o&&o.replace?e.history.replaceState(h,v,a.prefix+t):e.history.pushState(h,v,a.prefix+t)}else e.location.href=a.prefix+t}};return a.defineRoutes=function(o,l,s){function u(){var t=a.getPath(),n={},r=i(t,n,n),u=e.history.state;if(null!=u)for(var c in u)n[c]=u[c];for(var f in o){var d=new RegExp("^"+f.replace(/:[^\/]+?\.{3}/g,"(.*?)").replace(/:[^\/]+/g,"([^\\/]+)")+"/?$");if(d.test(r))return void r.replace(d,function(){for(var e=f.match(/:[^\/]+/g)||[],r=[].slice.call(arguments,1,-2),i=0;i<e.length;i++)n[e[i].replace(/:|\./g,"")]=decodeURIComponent(r[i]);l(o[f],n,t,f)})}s(t,n)}var c;n?e.onpopstate=(c=u,function(){null==t&&(t=r(function(){t=null,c()}))}):"#"===a.prefix.charAt(0)&&(e.onhashchange=u),u()},a};a.route=(v=window,p=d,x=L(v),(k=function(t,n,r){if(null==t)throw new Error("Ensure the DOM element that was passed to `m.route` is not undefined");var o=function(){null!=m&&p.render(t,m(e(g,y.key,y)))},i=function(e){if(e===n)throw new Error("Could not resolve default route "+n);x.setPath(n,null,{replace:!0})};x.defineRoutes(r,function(e,t,n){var r=w=function(e,i){r===w&&(g=null==i||"function"!=typeof i.view&&"function"!=typeof i?"div":i,y=t,b=n,w=null,m=(e.render||function(e){return e}).bind(e),o())};e.view||"function"==typeof e?r({},e):e.onmatch?C.resolve(e.onmatch(t,n)).then(function(t){r(e,t)},i):r(e,"div")},i),p.subscribe(t,o)}).set=function(e,t,n){null!=w&&((n=n||{}).replace=!0),w=null,x.setPath(e,t,n)},k.get=function(){return b},k.prefix=function(e){x.prefix=e},k.link=function(e){e.dom.setAttribute("href",x.prefix+e.attrs.href),e.dom.onclick=function(e){if(!(e.ctrlKey||e.metaKey||e.shiftKey||2===e.which)){e.preventDefault(),e.redraw=!1;var t=this.getAttribute("href");0===t.indexOf(x.prefix)&&(t=t.slice(x.prefix.length)),k.set(t,void 0,void 0)}}},k.param=function(e){return void 0!==y&&void 0!==e?y[e]:y},k),a.withAttr=function(e,t,n){return function(r){t.call(n||this,e in r.currentTarget?r.currentTarget[e]:r.currentTarget.getAttribute(e))}};var S=f(window);a.render=S.render,a.redraw=d.redraw,a.request=c.request,a.jsonp=c.jsonp,a.parseQueryString=E,a.buildQueryString=s,a.version="1.1.6",a.vnode=e,"undefined"!=typeof module?module.exports=a:window.m=a}(function(e){"use strict";var t=function(e,t){var n=a(e);n&&n.classList.add(t)},n=function(e){if(d(e))return e;var t;if(u(e))return e.map(function(e){return n(e)});if(c(e))return new Date(e.getTime());if(e instanceof RegExp)return(t=new RegExp(e.source)).global=e.global,t.ignoreCase=e.ignoreCase,t.multiline=e.multiline,t.lastIndex=e.lastIndex,t;if(h(e)){for(var r in t={},e)e.hasOwnProperty(r)&&(t[r]=n(e[r]));return t}return e},r=function(e,t){return d(e)?t:e},o=function(e,t){var n;for(n in t)if(v(e[n]))return!1;for(n in t)if(t[n])switch(typeof t[n]){case"object":if(!o(t[n],e[n]))return!1;break;case"function":if(v(e[n])||"equals"!==n&&t[n].toString()!==e[n].toString())return!1;break;default:if(t[n]!==e[n])return!1}else if(e[n])return!1;for(n in e)if(v(t[n]))return!1;return!0},i=function(){var e,t,n=arguments.length,r=n>0?arguments[0]:{};for(d(r)&&(r={}),t=1;t<n;t++)for(e in arguments[t])arguments[t].hasOwnProperty(e)&&(r[e]=arguments[t][e]);return r},a=function(e,t){if("string"!=typeof e)return e;if(t)return t.querySelector(e);var n=e.charAt(0),r=-1===e.indexOf(" ",1)&&-1===e.indexOf(".",1);if("#"===n&&r)return document.getElementById(e.substr(1));if("."===n&&r){var o=document.getElementsByClassName(e.substr(1));return o.length?o[0]:null}return document.querySelector(e)},l=function(e,t){var n;return n="."===e.charAt(0)&&-1===e.indexOf(",")&&-1===e.indexOf(">")?(t||document).getElementsByClassName(e.substr(1)):(t||document).querySelectorAll(e),Array.prototype.slice.call(n)},s=function(e){return!(d(e)||0===e.length)},u=function(e){return!d(e)&&e.constructor===Array},c=function(e){return!d(e)&&e.getMonth&&!isNaN(e.getTime())},f=function(e){return"function"==typeof e},d=function(e){return v(e)||null===e},h=function(e){return"object"==typeof e},v=function(e){return void 0===e},p=function(e,t,n,r){var o=a(e);o&&o.addEventListener(t,n,r)},m=function(e,t){var n=a(e);n&&n.classList.remove(t)},g=function(e){if(d(e))return{};for(var t,n=Object.keys(e),r=n.length,o={};r--;)o[(t=n[r]).toLowerCase()]=e[t];return o},y=function(e){for(var t,n=/([\w-]*)\s*:\s*([^;]*)/g,r={};t=n.exec(e);)r[t[1].toLowerCase()]=t[2];return r},b=function(e){if(d(e))return"";for(var t,n=Object.keys(e),r=n.length,o="";r--;)t=n[r],d(e[t])||(o+=t+": "+e[t]+"; ");return o};e.$={addClass:t,clone:n,closest:function(e,t){for(var n=e.charAt(0),r=e.substr(1),o=e.toLowerCase();t!==document;){if(!(t=t.parentNode))return null;if("."===n&&t.classList&&t.classList.contains(r))return t;if("#"===n&&t.id===r)return t;if("["===n&&t.hasAttribute(e.substr(1,e.length-2)))return t;if(t.tagName&&t.tagName.toLowerCase()===o)return t}return null},coalesce:r,createNode:function(e){var t=document.createElement("div");return t.innerHTML=e,e&&e.length?t.children[0]:t},debounce:function(e,t){var n,r,o,i;return function(){o=this,r=[].slice.call(arguments,0),i=new Date;var a=function(){var l=new Date-i;l<t?n=setTimeout(a,t-l):(n=null,e.apply(o,r))};n||(n=setTimeout(a,t))}},destroy:function(e){d(e)||(e.destroy&&e.destroy(),e=null)},equals:o,extend:i,get:a,getAll:l,findByKey:function(e,t,n){if(e&&!d(t)){for(var r=e.length-1;r>-1;){if(e[r][t]===n)return e[r]._i=r,e[r];r--}return null}},hasClass:function(e,t){var n=a(e);return n&&n.classList&&n.classList.contains(t)},hasPositiveValue:function(e){return s(e)&&e>0},hasValue:s,hide:function(e,n){var o=a(e);o&&(r(n,!1)?t(o,"invisible"):t(o,"hidden"))},isArray:u,isDate:c,isFunction:f,isNode:function(e){return!d(e)&&1===e.nodeType&&e.nodeName},isNull:d,isNumber:function(e){return"number"==typeof e&&!isNaN(e)},isObject:h,isString:function(e){return"string"==typeof e},isUndefined:v,isVisible:function(e){return null!==e.offsetParent},matches:function(e,t){var n=Element.prototype;return(n.matches||n.webkitMatchesSelector||n.mozMatchesSelector||n.msMatchesSelector||function(e){return-1!==[].indexOf.call(l(e),this)}).call(e,t)},noop:function(){},off:function(e,t,n,r){var o=a(e);o&&o.removeEventListener(t,n,r)},on:p,onChange:function(e,t,n){var o=a(e);o&&(p(o,"change",t),r(n,!0)&&t.call(o))},ready:function(e){f(e)&&("complete"===document.readyState&&e(),document.addEventListener("DOMContentLoaded",e,!1))},removeClass:m,setText:function(e,t){var n=a(e);n&&(n.textContent=t)},show:function(e){var t=a(e);t&&(m(t,"invisible"),m(t,"hidden"))},style:function(e,t,n){var o=a(e);if(o){if(d(t))return o.style.cssText;o.style.cssText=b(r(n,!1)?i(y(o.style.cssText),g(t)):t)}}}}(this)),function(e,t){"use strict";var n=function(e){if(e){var n=function(){e&&e.parentNode&&e.parentNode.removeChild(e)};t.removeClass(e,"show"),t.hide(e),t.on(e,"transitionend",n),setTimeout(n,500)}},r={parent:document.body,version:"1.0.11",defaultOkLabel:"Okay",okLabel:"Okay",defaultCancelLabel:"Cancel",cancelLabel:"Cancel",maxLogItems:4,promptValue:"",promptPlaceholder:"",closeLogOnClick:!0,delay:5e3,logContainerClass:"alertify-logs bottom left",dialogs:{buttons:{holder:"<nav>{{buttons}}</nav>",ok:'<button class="ok btn btn-warning" tabindex="1">{{ok}}</button>',cancel:'<button class="cancel btn btn-primary" tabindex="2">{{cancel}}</button>'},input:'<div class="ml-10 mr-10"><input type="text" class="form-control"></div>',message:'<p class="msg">{{message}}</p>',log:'<div class="{{class}}">{{message}}</div>'},build:function(e){var t=this.dialogs.buttons.ok,n='<div class="dialog"><div class="dialog-content">'+this.dialogs.message.replace("{{message}}",e.message);return"confirm"!==e.type&&"prompt"!==e.type||(t=this.dialogs.buttons.ok+this.dialogs.buttons.cancel),"prompt"===e.type&&(n+=this.dialogs.input),n=(n+this.dialogs.buttons.holder+"</div></div>").replace("{{buttons}}",t).replace("{{ok}}",this.okLabel).replace("{{cancel}}",this.cancelLabel)},close:function(e,r){this.closeLogOnClick&&t.on(e,"click",function(){n(e)}),(r=r&&!isNaN(+r)?+r:this.delay)<0?n(e):r>0&&setTimeout(function(){n(e)},r)},dialog:function(e,t,n,r){return this.setup({type:t,message:e,onOkay:n,onCancel:r})},log:function(e,n,r){var o=t.getAll(".alertify-logs > div");if(o){var i=o.length-this.maxLogItems;if(i>=0)for(var a=0,l=i+1;a<l;a++)this.close(o[a],-1)}this.notify(e,n,r)},setupLogContainer:function(){var e=t.get(".alertify-logs"),n=this.logContainerClass;return e||((e=document.createElement("div")).className=n,this.parent.appendChild(e)),e.className!==n&&(e.className=n),e},notify:function(e,n,r){var o=this.setupLogContainer(),i=document.createElement("div");i.className=n||"default",i.innerHTML=e,t.isFunction(r)&&i.addEventListener("click",r),o.appendChild(i),setTimeout(function(){t.addClass(i,"show")},10),this.close(i,this.delay)},setup:function(e){var r=document.createElement("div");r.className="alertify hidden",r.innerHTML=this.build(e);var o=t.get(".ok",r),i=t.get(".cancel",r),a=t.get("input",r),l=t.get("label",r),s=this;a&&(t.isString(this.promptPlaceholder)&&(l?l.textContent=this.promptPlaceholder:a.placeholder=this.promptPlaceholder),t.isString(this.promptValue)&&(a.value=this.promptValue));var u=new Promise(function(l){t.isFunction(l)||(l=function(){}),o&&t.on(o,"click",function(o){t.isFunction(e.onOkay)&&(a?e.onOkay(a.value,o):e.onOkay(o)),l(a?{buttonClicked:"ok",inputValue:a.value,event:o}:{buttonClicked:"ok",event:o}),n(r),s.reset()}),i&&t.on(i,"click",function(o){t.isFunction(e.onCancel)&&e.onCancel(o),l({buttonClicked:"cancel",event:o}),n(r),s.reset()}),a&&t.on(a,"keydown",function(e){o&&13===e.which&&o.click()}),t.on(r,"keydown",function(e){27===e.which&&(i?i.click():o&&o.click())})});return this.parent.appendChild(r),setTimeout(function(){t.show(r),a&&e.type&&"prompt"===e.type?(a.select(),a.focus()):o&&o.focus()},100),u},okBtn:function(e){return this.okLabel=e,this},cancelBtn:function(e){return this.cancelLabel=e,this},reset:function(){this.parent=document.body,this.okBtn(this.defaultOkLabel),this.cancelBtn(this.defaultCancelLabel),this.promptValue="",this.promptPlaceholder="",this.logTemplateMethod=null}},o={_$$alertify:r,parent:function(e){r.parent=e},reset:function(){return r.reset(),this},alert:function(e,t,n){return r.dialog(e,"alert",t,n)||this},confirm:function(e,t,n){return r.dialog(e,"confirm",t,n)||this},prompt:function(e,t,n){return r.dialog(e,"prompt",t,n)||this},log:function(e,t){return r.log(e,"default",t),this},success:function(e,t){return r.log(e,"success",t),this},error:function(e,t){return r.log(e,"error",t),this},cancelBtn:function(e){return r.cancelBtn(e),this},okBtn:function(e){return r.okBtn(e),this},placeholder:function(e){return r.promptPlaceholder=e,this},defaultValue:function(e){return r.promptValue=e,this},dismissAll:function(){return r.setupLogContainer().innerHTML="",this},updateResources:function(e,t){r.defaultOkLabel=e,r.okLabel=e,r.defaultCancelLabel=t,r.cancelLabel=t}};e.Alertify=o}(this,this.$),function(e,t,n){"use strict";var r=function(r,o,i){r.deserialize=c,r.config=function(e,t){e.timeout=6e4,e.setRequestHeader("X-Requested-With","XMLHttpRequest"),t&&t.token&&e.setRequestHeader("X-XSRF-Token",t.token)};var a=t.coalesce(r.block,!0);a&&l(),r.url+=(r.url.indexOf("?")>-1?"&":"?")+"_t="+Date.now(),e.request(r).then(function(e){e.reload?location.reload():e.error?(a&&u(),t.isFunction(i)&&i(e),n.error(e.error)):(a&&u(),t.isFunction(o)&&o(e),e.message&&n.success(e.message))}).catch(function(e){a&&u(),r.url.indexOf("LogJavascriptError")>-1||(s(e),n.error("An unhandled error occurred."),t.isFunction(i)&&i(e))})},o=[],i=function(e,t,n){this.options=e,this.onSuccess=t,this.onError=n,this.status=0};i.prototype={constructor:i,key:function(){return this.options.key},abort:function(){this.isInProcess()&&this.promise.reject(),this.dequeue()},execute:function(){r(this.options,this.success.bind(this),this.error.bind(this)),this.status=1},success:function(e){this.dequeue(),this.onSuccess&&this.onSuccess(e)},error:function(e){this.dequeue(),this.onError&&this.onError(e)},isInProcess:function(){return 1===this.status},dequeue:function(){var e=this;(o=o.filter(function(t){return t!==e})).length&&o[0].execute()}};var a,l=function(){t.show(f)},s=function(e,n,o,i,a){if(!t.isNull(e)){var l=e+": at path '"+(n||document.location)+"'";t.isNull(o)||(l+=" at "+o+":"+i),t.isNull(a)||(l+="\n    at "+(t.isString(a)?a:a.join("\n    at "))),r({method:"POST",url:"/Error/LogJavascriptError",data:{message:l},block:!1},null,null)}},u=function(){t.hide(f)},c=function(e){if(t.isNull(e)||0===e.length)return null;try{return JSON.parse(e)}catch(t){return{content:e}}},f=(a=t.get("#loader"),t.on(a,"keydown",function(e){if(!t.hasClass("#loader","hidden"))return e.preventDefault(),e.stopPropagation(),!1}),a);t.ajax=function(e,t,n){e.key=e.key||e.url;var r=new i(e,t,n);(o=o.filter(function(t){return t.key()!==e.key||t.isInProcess()})).push(r),1===o.length&&r.execute()},t.logError=s}(this.m,this.$,this.Alertify),window.onerror=function(e,t,n,r,o){this.$.logError(e,t,n,r,o&&o.stack?o.stack:null)},function(e){"use strict";var t={resxLoaded:new CustomEvent("resxLoaded"),formValidate:new CustomEvent("formValidate"),datasetFormLoad:new CustomEvent("datasetFormLoad"),datasetFormUnload:new CustomEvent("datasetFormUnload"),reportLoad:new CustomEvent("reportLoad"),reportUnload:new CustomEvent("reportUnload"),reportShareLoad:new CustomEvent("reportShareLoad"),reportShareUnload:new CustomEvent("reportShareUnload"),chartLoad:new CustomEvent("chartLoad"),chartUnload:new CustomEvent("chartUnload"),chartShareLoad:new CustomEvent("chartShareLoad"),chartShareUnload:new CustomEvent("chartShareUnload"),columnSelectorLoad:new CustomEvent("columnSelectorLoad"),dashboardLoad:new CustomEvent("dashboardLoad"),dashboardReload:new CustomEvent("dashboardReload")};e.events=t}(this.$),function(e){"use strict";var t={},n=function(e){return e.charAt(0).toUpperCase()+e.slice(1)},r=function(e,t){return t=t||{},e.split(t.split||/(?=[A-Z])/).join(t.separator||" ")},o=e.get("body");o&&o.hasAttribute("data-resx")&&e.ajax({method:"GET",url:o.getAttribute("data-resx")},function(n){n&&(t=n),document.dispatchEvent(e.events.resxLoaded),e.resxLoaded=!0},function(){document.dispatchEvent(e.events.resxLoaded),e.resxLoaded=!0}),e.resx=function(o,i){if(e.isString(o)){if(e.isNull(i)){if(t.hasOwnProperty(o))return t[o];console.log("Couldn't find translation for key `"+o+"`."),console.trace();var a=o.split(".");return r(n(a[a.length-1])).trim()}t[o]=i}else e.extend(t,o)}}(this.$);
//# sourceMappingURL=core.js.map
