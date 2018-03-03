/*!
 * Wraps dataset form functionality.
 */
(function ($, Dataset) {
    'use strict';

    /**
     * Store references to the dataset form mithril modules and value lists.
     * @type {Object}
     */
    var _datasets = {};
    
    /**
     * Initialize the dataset form when the datasetFormLoad event fires.
     */
    $.on(document, 'datasetFormLoad', function () {
        var form = $.dialogs.getActiveContent();
        if (!$.hasClass(form, 'dataset-form')) {
            return;
        }

        var dataset = $.get('.dataset-id', form);
        $.ajax({
            method: 'GET',
            url: form.getAttribute('data-url'),
            data: dataset ? { id: dataset.value } : null
        }, function(data) {
            var dlg = $.dialogs.getActiveDialog();
            data.content = dlg.getContent();
            _datasets[dlg.getId()] = new Dataset(data);
        });
    });

    /**
     * Destroy the form when the dialog closes.
     */
    $.on(document, 'datasetFormUnload', function () {
        if (!_datasets) {
            return;
        }

        var dlg = $.dialogs.getActiveDialog();
        var dataset = _datasets[dlg.getId()];
        if (dataset) {
            dataset.destroy();
        }
        delete _datasets[dlg.getId()];
    });
})(this.$, this.Dataset);