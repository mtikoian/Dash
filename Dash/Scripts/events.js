/*!
 * Custom event list.
 */
(function($) {
    'use strict';

    var events = {
        chartLoad: new CustomEvent('chartLoad'),
        chartShareLoad: new CustomEvent('chartShareLoad'),
        chartShareUnload: new CustomEvent('chartShareUnload'),
        chartUnload: new CustomEvent('chartUnload'),
        columnSelectorLoad: new CustomEvent('columnSelectorLoad'),
        dashboardLoad: new CustomEvent('dashboardLoad'),
        dashboardReload: new CustomEvent('dashboardReload'),
        datasetFormLoad: new CustomEvent('datasetFormLoad'),
        datasetFormUnload: new CustomEvent('datasetFormUnload'),
        formValidate: new CustomEvent('formValidate'),
        layoutUpdate: new CustomEvent('layoutUpdate'),
        reportLoad: new CustomEvent('reportLoad'),
        reportUnload: new CustomEvent('reportUnload'),
        reportShareLoad: new CustomEvent('reportShareLoad'),
        reportShareUnload: new CustomEvent('reportShareUnload'),
        resxLoaded: new CustomEvent('resxLoaded'),
        tableDestroy: new CustomEvent('tableDestroy'),
        tableRefresh: new CustomEvent('tableRefresh')
    };

    $.events = events;
}(this.$));
