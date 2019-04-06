/*!
 * ==========================================================
 *  COLOR PICKER PLUGIN 1.4.1
 * ==========================================================
 * Author: Taufik Nurrohman <https://github.com/tovic>
 * License: MIT
 * ----------------------------------------------------------
 */

(function(root, factory) {
    root.CP = factory(root, root.$);
})(this, function(root, $) {
    'use strict';
    
    var isObject = function(x) {
        return typeof x === 'object';
    };

    var edge = function(a, b, c) {
        if (a < b)
            return b;
        if (a > c)
            return c;
        return a;
    };

    var toNum = function(i, j) {
        return parseInt(i, j || 10);
    };

    // [h, s, v] ... 0 <= h, s, v <= 1
    var hsv2rgb = function(a) {
        var h = +a[0],
            s = +a[1],
            v = +a[2],
            r, g, b, i, f, p, q, t;
        i = Math.floor(h * 6);
        f = h * 6 - i;
        p = v * (1 - s);
        q = v * (1 - f * s);
        t = v * (1 - (1 - f) * s);
        i = i || 0;
        q = q || 0;
        t = t || 0;
        switch (i % 6) {
            case 0:
                r = v, g = t, b = p;
                break;
            case 1:
                r = q, g = v, b = p;
                break;
            case 2:
                r = p, g = v, b = t;
                break;
            case 3:
                r = p, g = q, b = v;
                break;
            case 4:
                r = t, g = p, b = v;
                break;
            case 5:
                r = v, g = p, b = q;
                break;
        }
        return [Math.round(r * 255), Math.round(g * 255), Math.round(b * 255)];
    };

    var hsv2hex = function(a) {
        return '#' + rgb2hex(hsv2rgb(a));
    };

    // [r, g, b] ... 0 <= r, g, b <= 255
    var rgb2hsv = function(a) {
        var r = +a[0],
            g = +a[1],
            b = +a[2],
            max = Math.max(r, g, b),
            min = Math.min(r, g, b),
            d = max - min,
            h, s = (max === 0 ? 0 : d / max),
            v = max / 255;
        switch (max) {
            case min:
                h = 0;
                break;
            case r:
                h = (g - b) + d * (g < b ? 6 : 0);
                h /= 6 * d;
                break;
            case g:
                h = (b - r) + d * 2;
                h /= 6 * d;
                break;
            case b:
                h = (r - g) + d * 4;
                h /= 6 * d;
                break;
        }
        return [h, s, v];
    };

    var rgb2hex = function(a) {
        var s = +a[2] | (+a[1] << 8) | (+a[0] << 16);
        s = '000000' + s.toString(16);
        return s.slice(-6);
    };

    // rrggbb or rgb
    var hex2hsv = function(x) {
        x = x.indexOf('#') === 0 ? x.slice(1) : x;
        return rgb2hsv(hex2rgb(x));
    };

    var hex2rgb = function(s) {
        if (s.length === 3)
            s = s.replace(/./g, '$&$&');
        return [toNum(s[0] + s[1], 16), toNum(s[2] + s[3], 16), toNum(s[4] + s[5], 16)];
    };

    var parse = function(x) {
        if (isObject(x) || $.isArray(x))
            return x;
        x = x.indexOf('#') === 0 ? x.slice(1) : x;
        return hex2hsv(x);
    };

    var on = function(ev, el, fn) {
        ev = ev.split(/\s+/);
        for (var i = 0, ien = ev.length; i < ien; ++i)
            el.addEventListener(ev[i], fn, false);
    };

    var off = function(ev, el, fn) {
        ev = ev.split(/\s+/);
        for (var i = 0, ien = ev.length; i < ien; ++i)
            el.removeEventListener(ev[i], fn);
    };

    // get mouse/finger coordinate
    var point = function(el, e) {
        var x = e['touches'] ? e['touches'][0]['clientX'] : e['clientX'],
            y = e['touches'] ? e['touches'][0]['clientY'] : e['clientY'],
            o = offset(el);
        return { x: x - o.l, y: y - o.t };
    };

    // get position
    var offset = function(el, docElement) {
        var left, top;
        if (el === root) {
            left = root.pageXOffset || docElement.scrollLeft;
            top = root.pageYOffset || docElement.scrollTop;
        } else {
            var rect = el.getBoundingClientRect();
            left = rect.left;
            top = rect.top;
        }
        return { l: left, t: top };
    };

    // get closest parent
    var closest = function(a, b) {
        while ((a = a.parentElement) && a !== b);
        return a;
    };

    // prevent default
    var prevent = function(e) {
        if (e)
            e.preventDefault();
    };

    // get dimension
    var size = function(el, root) {
        return el === root ? { w: root.innerWidth, h: root.innerHeight } : { w: el.offsetWidth, h: el.offsetHeight };
    };
    
    var CP = function(node, onChange) {
        this.source = node;
        this.color = node.value ? hex2hsv(node.value) : [0, 1, 1];
        this.onChange = $.isFunction(onChange) ? onChange : function() { };
        this.docElement = root.document.documentElement;
        this.onDown = 'touchstart mousedown';
        this.onMove = 'touchmove mousemove';
        this.onUp = 'touchend mouseup';
        this.onResize = 'orientationchange resize';
        this.visible = false;
        this.startH = 0;
        this.startSV = 0;
        this.dragH = 0;
        this.dragSV = 0;

        this.create();
        this.setData(node.value);
    };

    CP.prototype.setData = function(val) {
        val = val || hsv2hex(this.color);
        var hex = val.charAt(0) === '#' ? val : ('#' + val);
        if (this.source.value !== hex) {
            this.source.value = hex;
            if ($.isFunction(this.onChange))
                this.onChange.call(this, hex);
        }
        this.color = parse(val);
        this.update();
    };

    CP.prototype.fit = function() {
        var w = size(root, root),
            screen_w = w.w - size(this.docElement, root).w, // vertical scroll bar
            screen_h = w.h - this.docElement.clientHeight, // horizontal scroll bar
            ww = offset(root, this.docElement),
            to = offset(this.source, this.docElement),
            left = to.l + ww.l,
            top = to.t + ww.t + size(this.source, root).h;

        var min_x = ww.l,
            min_y = ww.t,
            max_x = ww.l + w.w - size(this.pickerDiv, root).w - screen_w,
            max_y = ww.t + w.h - size(this.pickerDiv, root).h - screen_h;
        left = edge(left, min_x, max_x) >> 0;
        top = edge(top, min_y, max_y) >> 0;

        this.pickerDiv.style.left = left + 'px';
        this.pickerDiv.style.top = top + 'px';
    };

    CP.prototype.create = function() {
        // generate color picker pane ...
        this.pickerDiv = root.document.createElement('div');
        this.pickerDiv.className = 'color-picker';
        this.pickerDiv.innerHTML = '<div class="color-picker-container"><span class="color-picker-h"><i></i></span><span class="color-picker-sv"><i></i></span></div>';
        root.document.body.appendChild(this.pickerDiv);
        this.pickerDiv.style.left = this.pickerDiv.style.top = '-9999px';

        var self = this;
        var update = function() {
            self.setData(this.value);
            self.enter();
        };
        this.source.oncut = update;
        this.source.onpaste = update;
        this.source.onkeyup = update;
        this.source.oninput = update;

        var btn = $.get('button', this.source.parentNode);
        $.on(this.source, 'focus', function() { self.enter(); }, false);
        $.on(this.source, 'blur', function(e) {
            if (!btn || e.relatedTarget !== btn)
                self.exit();
        });
        if (btn) {
            $.on(btn, 'blur', function(e) {
                if (e.relatedTarget !== self.source)
                    self.exit();
            });
            $.on(btn, 'click', function() { self[self.visible ? 'exit' : 'enter'](); }, false);
        }

        var children = this.pickerDiv.firstChild.children;
        var h = children[0];
        var sv = children[1];
        var svSize = size(sv, root);
        var svPointSize = size(sv.firstChild, root);
        this.H_H = size(h, root).h;
        this.SV_W = svSize.w;
        this.SV_H = svSize.h;
        this.H_point_H = size(h.firstChild, root).h;
        this.SV_point_W = svPointSize.w;
        this.SV_point_H = svPointSize.h;

        this.fit();
        $.hide(this.pickerDiv);

        this.events = {
            downH: this.downH.bind(this),
            downSV: this.downSV.bind(this),
            move: this.move.bind(this),
            stop: this.stop.bind(this),
            fit: this.fit.bind(this),
        };
    };

    CP.prototype.enter = function() {
        this.visible = true;
        $.show(this.pickerDiv);
        this.fit();

        var children = this.pickerDiv.firstChild.children;
        on(this.onDown, children[0], this.events.downH);
        on(this.onDown, children[1], this.events.downSV);
        on(this.onMove, root.document, this.events.move);
        on(this.onUp, root.document, this.events.stop);
        on(this.onResize, root, this.events.fit);
    };

    CP.prototype.update = function() {
        this.updateColor();
        var children = this.pickerDiv.firstChild.children;
        children[0].firstChild.style.top = (this.H_H - (this.H_point_H / 2) - (this.H_H * +this.color[0])) + 'px';

        var svPoint = children[1].firstChild;
        svPoint.style.right = (this.SV_W - (this.SV_point_W / 2) - (this.SV_W * +this.color[1])) + 'px';
        svPoint.style.top = (this.SV_H - (this.SV_point_H / 2) - (this.SV_H * +this.color[2])) + 'px';
    };

    CP.prototype.destroy = function() {
        this.exit();
        if (this.pickerDiv.parentNode)
            this.pickerDiv.parentNode.removeChild(this.pickerDiv);
    };

    CP.prototype.exit = function() {
        this.visible = false;
        $.hide(this.pickerDiv);

        var children = this.pickerDiv.firstChild.children;
        off(this.onDown, children[0], this.events.downH);
        off(this.onDown, children[1], this.events.downSV);
        off(this.onMove, root.document, this.events.move);
        off(this.onUp, root.document, this.events.stop);
        off(this.onResize, root, this.events.fit);
    };

    CP.prototype.updateColor = function(e) {
        this.pickerDiv.firstChild.children[1].style.backgroundColor = 'rgb(' + hsv2rgb([this.color[0], 1, 1]).join(',') + ')';
        prevent(e);
    };

    CP.prototype.do_H = function(e) {
        var children = this.pickerDiv.firstChild.children;
        var y = edge(point(children[0], e).y, 0, this.H_H);
        this.color[0] = (this.H_H - y) / this.H_H;
        children[0].firstChild.style.top = (y - (this.H_point_H / 2)) + 'px';
        this.updateColor(e);
    };

    CP.prototype.do_SV = function(e) {
        var children = this.pickerDiv.firstChild.children;
        var o = point(children[1], e),
            x = edge(o.x, 0, this.SV_W),
            y = edge(o.y, 0, this.SV_H);
        this.color[1] = 1 - ((this.SV_W - x) / this.SV_W);
        this.color[2] = (this.SV_H - y) / this.SV_H;

        var svPoint = children[1].firstChild;
        svPoint.style.right = (this.SV_W - x - (this.SV_point_W / 2)) + 'px';
        svPoint.style.top = (y - (this.SV_point_H / 2)) + 'px';
        this.updateColor(e);
    };

    CP.prototype.move = function(e) {
        if (this.dragH) {
            this.do_H(e);
            if (!this.startH)
                this.setData();
        }
        if (this.dragSV) {
            this.do_SV(e);
            if (!this.startSV)
                this.setData();
        }
        this.startH = 0;
        this.startSV = 0;
    };

    CP.prototype.stop = function(e) {
        var target = e.target;
        if (target === this.pickerDiv || closest(target, this.pickerDiv) === this.pickerDiv)
            this.setData();
        this.dragH = 0;
        this.dragSV = 0;
    };

    CP.prototype.downH = function(e) {
        this.startH = 1;
        this.dragH = 1;
        this.move(e);
        prevent(e);
        this.setData();
    };

    CP.prototype.downSV = function(e) {
        this.startSV = 1;
        this.dragSV = 1;
        this.move(e);
        prevent(e);
        this.setData();
    };

    return CP;
});
