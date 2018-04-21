/*!
 * Validator that converts HTML5 form validation errors into bootstrap friendly errors.
 * Based on the library below, but ported from jQuery to native js.
 *
 * Validator v0.9.0 for Bootstrap 3, by @1000hz
 * Copyright 2015 Cina Saffary
 * Licensed under http://opensource.org/licenses/MIT
 * https://github.com/1000hz/bootstrap-validator
 *
 * Modified to use Dash's core library instead of jQuery, with unneeded functionality removed.
 */
(function(root, factory) {
    root.Validator = factory(root.$);
})(this, function($) {
    'use strict';

    /**
     * Declare validator class.
     * @param {Node} element - Form to validate.
     * @param {Object} errorMsgs - Error messages for custom errors.
     */
    var Validator = function(element, errorMsgs) {
        this.element = element;
        this.element.setAttribute('novalidate', true); // disable automatic native validation
        this.errorMsgs = $.extend({}, Validator._errors, errorMsgs || {});

        var inputs = $.getAll('[data-match]', this.element);
        for (var i = 0; i < inputs.length; i++) {
            // add the data match attribute to the target if it doesn't already exist
            var target = inputs[i].getAttribute('data-match');
            if (target) {
                var targetElement = $.get('#' + target, this.element);
                if (targetElement && !targetElement.hasAttribute('data-match')) {
                    targetElement.setAttribute('data-match', inputs[i].id);
                }
            }
        }

        // check for pre-existing error classes because razor is weird sometimes
        this.resetAll();

        $.on(this.element, 'input', this.validateInput.bind(this));
        $.on(this.element, 'change', this.validateInput.bind(this));
        $.on(this.element, 'focusout', this.validateInput.bind(this));
        $.on(this.element, 'submit', this.onSubmit.bind(this));
        $.on(this.element, 'formValidate', this.validate.bind(this));
    };

    /**
     * Default error messages for custom errors.
     * @type {Object}
     */
    Validator._errors = {
        match: '{0} does not match {1}.',
        minlength: 'This field is not long enough.'
    };

    /**
     * Functions to validate inputs against.
     * @type {Object}
     */
    Validator.VALIDATORS = {
        'native': function(el) {
            return el.checkValidity();
        },
        'match': function(el) {
            var target = $.get('#' + el.getAttribute('data-match'), this.element);
            var res = !target ? false : el.value === target.value;
            if (res) {
                el.setCustomValidity('');
            } else {
                el.setCustomValidity(this.errorMsgs.match.replace('{0}', el.name).replace('{1}', el.getAttribute('data-match')));
            }
            return res;
        },
        'minlength': function(el) {
            var minlength = el.getAttribute('data-minlength');
            var res = !el.value || el.value.length >= minlength;
            if (res) {
                el.setCustomValidity('');
            } else {
                el.setCustomValidity(this.errorMsgs.minlength);
            }
            return res;
        }
    };

    Validator.prototype = {
        /**
         * Validate a single input element.
         * @param {Node} e - Element to validate
         */
        validateInput: function(e) {
            var el;
            if (this.isValidatableInput(e)) {
                el = e;
            } else if (e.target && this.isValidatableInput(e.target)) {
                el = e.target;
            } else {
                return;
            }

            var self = this;
            if (el.type === 'radio') {
                el = $.getAll('input[name="' + el.getAttribute('name') + '"]', self.element);
            }

            if (e.defaultPrevented) {
                return;
            }

            var errors = this.runValidators(el);
            if (errors.length) {
                self.showErrors(el, errors);
            } else {
                self.clearErrors(el);
                if (el.getAttribute('data-match')) {
                    self.clearErrors($.get('#' + el.getAttribute('data-match'), self.element));
                }
            }
        },

        /**
         * Get all the validatable inputs in the form.
         * @returns {Node[]} Array of input elements.
         */
        inputSelector: function() {
            var inputs = $.getAll('input,select,textarea', this.element);
            var length = inputs.length, i = 0, results = [];
            for (; i < length; i++) {
                var input = inputs[i];
                if (input.type !== 'submit' && input.type !== 'button' && !input.getAttribute('disabled') && input.style.visibility !== 'hidden') {
                    results.push(input);
                } else {
                    // remove error class on disabled items
                    $.removeClass(input, 'form-control-error');
                }
            }
            return results;
        },

        /**
         * Get all the inputs in the form and remove error styling.
         */
        resetAll: function() {
            $.getAll('input,select,textarea', this.element).forEach(function(x) {
                $.removeClass(x, 'form-control-error');
            });
        },

        /**
         * Check if a node is an input that can be validated.
         * @param {Node} input - Node to check.
         * @returns {bool} True if input is an input that can be validated.
         */
        isValidatableInput: function(input) {
            return (input.tagName === 'INPUT' || input.tagName === 'SELECT' || input.tagName === 'TEXTAREA') && !input.getAttribute('disabled') && input.style.visibility !== 'hidden';
        },

        /**
         * Run all of the validator functions against an element.
         * @param {Node} el - Element to validate.
         * @returns {string[]} Array of error messages.
         */
        runValidators: function(el) {
            var errors = [];

            for (var key in Validator.VALIDATORS) {
                if (Validator.VALIDATORS.hasOwnProperty(key)) {
                    var validator = Validator.VALIDATORS[key];
                    var attr = el.getAttribute('data-' + key);
                    if ((attr || key === 'native') && !validator.call(this, el)) {
                        var error = el.getAttribute('data' + key + '-error') || el.getAttribute('data-error') || (key === 'native' ? el.validationMessage : this.errorMsgs[key]);
                        if (key === 'match') {
                            error = error.replace('{0}', el.name).replace('{1}', attr);
                        }
                        !~errors.indexOf(error) && errors.push(error);
                    }
                }
            }

            return errors;
        },

        validate: function() {
            var inputs = this.inputSelector();
            var length = inputs.length, i = 0;
            for (; i < length; i++) {
                this.validateInput(inputs[i]);
            }
        },

        /**
         * Display errors for an element.
         * @param {Node} el - Element to display errors for.
         * @param {string[]} errors - List of error messages.
         */
        showErrors: function(el, errors) {
            if (!errors.length) {
                return;
            }

            var group = $.closest('.form-group', el);
            var block = $.get('.help-block.with-errors', group);

            if (block) {
                var errorElement = document.createElement('ul');
                $.addClass(errorElement, 'list-unstyled');

                var errHtml = '', i = 0, len = errors.length;
                for (; i < len; i++) {
                    errHtml += '<li>' + errors[i] + '</li>';
                }
                errorElement.innerHTML = errHtml;

                block.innerHTML = '';
                block.appendChild(errorElement);
            }

            $.addClass(el, 'form-control-error');

            var tab = $.closest('.tab-pane', el);
            if (tab) {
                // add error class to tab
                var id = tab.getAttribute('aria-labelledby');
                if (id) {
                    $.addClass($.get('#' + id), 'tab-validation-error');
                }
            }
        },

        /**
         * Hide all error messages for an element.
         * @param {Node} el - Element to hide errors for.
         */
        clearErrors: function(el) {
            $.removeClass(el, 'form-control-error');

            var group = $.closest('.form-group', el);
            if (group) {
                var block = $.get('.help-block.with-errors', group);
                if (block) {
                    block.innerHTML = '';
                }
            }
            var tab = $.closest('.tab-pane', el);
            if (tab) {
                if ($.getAll('.form-control-error', tab).length == 0) {
                    var id = tab.getAttribute('aria-labelledby');
                    if (id) {
                        $.removeClass($.get('#' + id), 'tab-validation-error');
                    }
                }
            }
        },

        /**
         * Check if the validator found any errors.
         * @returns {bool} True if there are any errors.
         */
        hasErrors: function() {
            return $.getAll('.form-control-error', this.element).length;
        },

        /**
         * Validate the form and prevent submit if there are errors.
         * @param {Event} e - Form submit event.
         */
        onSubmit: function(e) {
            this.validate();
            if (this.hasErrors()) {
                e.preventDefault();
            }
        }
    };

    return Validator;
});
