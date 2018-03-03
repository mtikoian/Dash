/*!
 * Contains custom events.
 */
(function ($) {
    'use strict';

    /**
    * Custom event list.
    */
    var events = {
        resxLoaded: new CustomEvent('resxLoaded'),
        formValidate: new CustomEvent('formValidate'),
        datasetFormLoad: new CustomEvent('datasetFormLoad'),
        datasetFormUnload: new CustomEvent('datasetFormUnload'),
        reportLoad: new CustomEvent('reportLoad'),
        reportUnload: new CustomEvent('reportUnload'),
        reportShareLoad: new CustomEvent('reportShareLoad'),
        reportShareUnload: new CustomEvent('reportShareUnload'),
        chartLoad: new CustomEvent('chartLoad'),
        chartUnload: new CustomEvent('chartUnload'),
        chartShareLoad: new CustomEvent('chartShareLoad'),
        chartShareUnload: new CustomEvent('chartShareUnload'),
        columnSelectorLoad: new CustomEvent('columnSelectorLoad'),
        dashboardLoad: new CustomEvent('dashboardLoad'),
        dashboardReload: new CustomEvent('dashboardReload')
    };

    $.events = events;
}(this.$));