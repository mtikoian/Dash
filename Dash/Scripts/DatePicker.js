/*!
 * DatePicker mithril component.
 * Modified from https://github.com/CreaturesInUnitards/mithril-datepicker
 */
(function(root, factory) {
    root.DatePicker = factory(root.m, root.$);
})(this, function(m, $) {
    'use strict';

    var _keys = {
        ENTER: 13,
        ESC: 27,
        SPACE: 32
    };

    var days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    var months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    var hours = [];
    var minutes = [];
    var i = 0;
    while (hours.push(i++) < 24) { /* nothing to do here */ }
    i = 0;
    while (minutes.push(i++) < 60) { /* nothing to do here */ }
    var prevNextTitles = ['1 Mo', '1 Yr', '10 Yr'];
    var weekStart = 0;
    var locale = 'en-us';
    var formatOptions = null;
    var defaultFormat = 'YYYY-MM-DD HH:mm';

    /**
     * Actions
     */
    function chooseDate(props, e) {
        var box = e.target;
        var selectedDate = parseInt(box.textContent);
        var dateObj = props.date;
        if ($.hasClass(box, 'other-scope')) {
            dateObj.setFullYear(dateObj.getFullYear(), dateObj.getMonth() + (selectedDate > 6 ? -1 : 1), selectedDate);
        } else {
            dateObj.setDate(selectedDate);
        }
    }

    function dismissAndCommit(props, e) {
        if (e) {
            if (e.target && $.hasClass('number', e.target)) {
                e.preventDefault();
                chooseDate(props, e);
                return false;
            }

            var parent = e.relatedTarget && $.closest('.mithril-date-picker-container', e.relatedTarget);
            if (!parent) {
                e.preventDefault();
                props.view = 0;
                props.active = false;
                return false;
            }
        }

        if (props.onchange) {
            props.onchange(props.date);
        }
    }

    function checkKey(props, e) {
        e.preventDefault();
        e.stopPropagation();
        if (e.keyCode === _keys.ESC) {
            dismissAndCommit(props, e);
        }
        return false;
    }

    function prevNext(props, delta) {
        var newDate = new Date(props.date);
        switch (props.view) {
            case 0:
                newDate.setMonth(newDate.getMonth() + delta);
                break;
            case 1:
                newDate.setFullYear(newDate.getFullYear() + delta);
                break;
            default:
                newDate.setFullYear(newDate.getFullYear() + (delta * 10));
        }
        props.date = pushToLastDay(props.date, newDate);
    }

    /**
     * Utility
     */
    function adjustedProps(date, delta) {
        var month = date.getMonth() + delta, year = date.getFullYear();
        var over = month > 11, under = month < 0;
        return {
            month: over ? 0 : under ? 11 : month,
            year: over ? year + 1 : under ? year - 1 : year
        };
    }

    function lastDateInMonth(date, delta) {
        var obj = adjustedProps(date, delta);
        if ([0, 2, 4, 6, 7, 9, 11].indexOf(obj.month) > -1) {
            return 31; // array of 31-day props.months
        }
        if (obj.month === 1) { // February
            if (!(obj.year % 400)) {
                return 29;
            }
            if (!(obj.year % 100)) {
                return 28;
            }
            return (obj.year % 4) ? 28 : 29;
        }
        return 30;
    }

    function pushToLastDay(oldDate, newDate) {
        if (oldDate.getDate() !== newDate.getDate()) {
            newDate.setMonth(newDate.getMonth() - 1, lastDateInMonth(newDate, -1));
        }
        return newDate;
    }

    function stringsForLocale(locale) {
        var date = new Date('jan 1 2017'), _months = [], _days = []; // 1/1/2017 was month:0 and weekday:0, so perfect
        while (_days.length < 7) {
            _days.push(date.toLocaleDateString(locale, { weekday: 'long' }));
            date.setDate(date.getDate() + 1);
        }
        while (_months.length < 12) {
            _months.push(date.toLocaleDateString(locale, { month: 'long' }));
            date.setMonth(date.getMonth() + 1);
        }
        return { days: _days, months: _months };
    }

    function wrapAround(idx, array) {
        var len = array.length;
        var n = idx >= len ? idx - len : idx;
        return array[n];
    }

    /**
     * Generators
     */
    function daysFromLastMonth(props) {
        var month = props.date.getMonth(), year = props.date.getFullYear();
        var firstDay = (new Date(year, month, 1)).getDay() - props.weekStart;
        if (firstDay < 0) firstDay += 7;
        var array = [];
        var lastDate = lastDateInMonth(props.date, -1);
        var offsetStart = lastDate - firstDay + 1;
        for (var i = offsetStart; i <= lastDate; i++) {
            array.push(i);
        }
        return array;
    }

    function daysFromThisMonth(props) {
        var max = lastDateInMonth(props.date, 0);
        var array = [];
        for (var i = 1; i <= max; i++) {
            array.push(i);
        }
        return array;
    }

    function daysFromNextMonth(prev, these) {
        var soFar = prev.concat(these);
        var mod = soFar.length % 7;
        var array = [];
        if (mod > 0) {
            var n = 7 - mod;
            for (var i = 1; i <= n; i++) {
                array.push(i);
            }
        }
        return array;
    }

    function defaultDate() {
        var now = new Date();
        now.setHours(0, 0, 0, 0);
        return now;
    }

    function yearsForDecade(date) {
        var year = date.getFullYear();
        var start = year - (year % 10);
        var array = [];
        for (var i = start; i < start + 10; i++) {
            array.push(i);
        }
        return array;
    }

    /**
     * View helpers
     */
    function classForBox(a, b) {
        return a === b ? 'chosen' : '';
    }

    /**
     * Components
     */
    var Header = {
        view: function(vnode) {
            var props = vnode.attrs.props;
            var date = props.date;
            var theseMonths = props.months || months;
            return m('.header',
                m('button.header-button.prev', { onclick: prevNext.bind(null, props, -1) }, [
                    m('i.dash.dash-to-start'),
                    prevNextTitles[props.view]
                ]),
                m('button.header-button.segment', { onclick: function() { props.view = 0; } }, date.getDate()),
                m('button.header-button.segment', { onclick: function() { props.view = 1; } }, theseMonths[date.getMonth()].substr(0, 3)),
                m('button.header-button.segment', { onclick: function() { props.view = 2; } }, date.getFullYear()),
                m('button.header-button.next', { onclick: prevNext.bind(null, props, 1) }, [
                    prevNextTitles[props.view],
                    m('i.dash.dash-to-end')
                ]),
                m('button.btn.btn-secondary.btn-sm', { onclick: dismissAndCommit.bind(null, props) }, m('i.dash.dash-cancel'))
            );
        }
    };

    var MonthView = {
        view: function(vnode) {
            var props = vnode.attrs.props;
            var prevDates = daysFromLastMonth(props);
            var theseDates = daysFromThisMonth(props);
            var nextDates = daysFromNextMonth(prevDates, theseDates);
            var theseWeekdays = props.days || days;
            return m('.calendar',
                m('.weekdays', theseWeekdays.map(function(_, idx) {
                    var day = wrapAround(idx + props.weekStart, theseWeekdays);
                    return m('.day.dummy', day.substring(0, 2));
                })),
                m('.weekdays', {
                    onclick: function(e) {
                        chooseDate(props, e);
                    }
                },
                    prevDates.map(function(date) {
                        return m('button.day.other-scope', date);
                    }),
                    theseDates.map(function(date) {
                        return m('button.day', { class: classForBox(props.date.getDate(), date) }, m('.number', date));
                    }),
                    nextDates.map(function(date) {
                        return m('button.day.other-scope', date);
                    })),
                m('.time',
                    m('select.form-control.custom-select', {
                        value: props.date.getHours(), onchange: function(e) {
                            props.date.setHours(e.target.value);
                            props.date.setHours(e.target.value);
                            if (props.onchange) {
                                props.onchange(props.date);
                            }
                        }
                    }, hours.map(function(x) {
                        return m('option', { value: x }, ('00' + x).slice(-2));
                    })),
                    m('select.form-control.custom-select', {
                        value: props.date.getMinutes(), onchange: function(e) {
                            props.date.setMinutes(e.target.value);
                            props.date.setMinutes(e.target.value);
                            if (props.onchange) {
                                props.onchange(props.date);
                            }
                        }
                    }, minutes.map(function(x) {
                        return m('option', { value: x }, ('00' + x).slice(-2));
                    }))
                )
            );
        }
    };

    var YearView = {
        view: function(vnode) {
            var props = vnode.attrs.props;
            var theseMonths = props.months || months;
            return m('.calendar',
                m('.months',
                    theseMonths.map(function(month, idx) {
                        return m('button.month', {
                            class: classForBox(props.date.getMonth(), idx), onclick: function() {
                                var newDate = new Date(props.date);
                                newDate.setMonth(idx);
                                props.date = pushToLastDay(props.date, newDate);
                                props.view = 0;
                            }
                        }, m('.number', month.substring(0, 3)));
                    })
                )
            );
        }
    };

    var DecadeView = {
        view: function(vnode) {
            var props = vnode.attrs.props;
            var decade = yearsForDecade(props.date);
            return m('.calendar',
                m('.years',
                    decade.map(function(year) {
                        return m('button.year', {
                            class: classForBox(props.date.getFullYear(), year), onclick: function() {
                                var newDate = new Date(props.date);
                                newDate.setFullYear(year);
                                props.date = pushToLastDay(props.date, newDate);
                                props.view = 1;
                            }
                        },
                            m('.number', year));
                    })
                )
            );
        }
    };

    var Editor = {
        oncreate: function(vnode) {
            requestAnimationFrame(function() {
                vnode.dom.classList.add('active');
            });
        },
        onbeforeremove: function(vnode) {
            vnode.dom.classList.remove('active');
            return new Promise(function(done) {
                setTimeout(done, 200);
            });
        },
        view: function(vnode) {
            var props = vnode.attrs.props;
            return m('.editor', { onkeydown: checkKey.bind(null, props) },
                m(Header, { props: props }),
                m('.sled', { class: 'p' + props.view },
                    m(MonthView, { props: props }),
                    m(YearView, { props: props }),
                    m(DecadeView, { props: props })
                )
            );
        }
    };

    var DatePicker = {
        localize: function(loc) {
            if (loc) {
                prevNextTitles = loc.prevNextTitles || prevNextTitles;
                locale = loc.locale || locale;
                formatOptions = loc.formatOptions || formatOptions;
                weekStart = $.isNumber(loc.weekStart) ? loc.weekStart : weekStart;
                var strings = stringsForLocale(locale);
                days = strings.days;
                months = strings.months;
            }
        },
        oninit: function(vnode) {
            var attrs = vnode.attrs;
            var date = new Date(attrs.date || defaultDate());
            if (!attrs.date) {
                attrs.onchange(date);
            }
            var props = {
                date: date,
                format: attrs.format || defaultFormat,
                name: attrs.name,
                active: false,
                view: 0,
                required: attrs.required,
                disabled: attrs.disabled,
                onchange: attrs.onchange,
                class: attrs.class
            };

            ['prevNextTitles', 'locale', 'formatOptions'].forEach(function(prop) {
                props[prop] = attrs[prop] || eval(prop);
            });
            props.weekStart = $.isNumber(attrs.weekStart) ? attrs.weekStart : weekStart;

            if (attrs.locale && attrs.locale !== locale) {
                var strings = stringsForLocale(props.locale);
                props.days = strings.days;
                props.months = strings.months;
            }

            vnode.state.props = props;
        },
        onbeforeupdate: function(vnode) {
            vnode.state.props.class = vnode.attrs.class;
        },
        onupdate: function(vnode) {
            var chosen = $.get('.chosen', vnode.dom);
            if (chosen) {
                chosen.focus();
            }
        },
        showEditor: function(props, e) {
            if (e && e instanceof KeyboardEvent) {
                if (e.keyCode !== _keys.SPACE && e.keyCode !== _keys.ENTER) {
                    return;
                }
            }

            if (props.disabled) {
                return;
            }
            if (props.active) {
                props.view = 0;
            }
            props.active = !props.active;
        },
        view: function(vnode) {
            var props = vnode.state.props;
            return m('.mithril-date-picker-container', { class: ((props.active ? 'active' : '') + ' ' + props.class).trim() },
                m('input.current-date.form-control', {
                    name: props.name,
                    class: props.required ? 'required' : null,
                    format: props.format,
                    readonly: true,
                    onclick: this.showEditor.bind(null, props),
                    onkeydown: this.showEditor.bind(null, props),
                    value: $.fecha.format(props.date, props.format || defaultFormat)
                }),
                m('i.dash.current-date-indicator', { class: props.active ? 'dash-sort-up' : 'dash-sort-down' }),
                props.active && m(Editor, { props: props })
            );
        }
    };

    return DatePicker;
});   
