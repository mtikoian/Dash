/*!
 * Wraps functionality for displaying charts.
 */
(function($, Alertify, Chart, ChartDetails, ShareForm) {
    'use strict';

    // Change chart settings.
    Chart.defaults.global.maintainAspectRatio = false;
    Chart.defaults.global.title.display = false;
    Chart.defaults.global.legend.position = 'bottom';
    Chart.defaults.global.legend.labels.fontSize = 16;
    Chart.defaults.global.legend.labels.fontFamily = 'Calibri';
    Chart.defaults.global.layout = { padding: 10 };
    Chart.defaults.scale.ticks.fontFamily = 'Calibri';
    Chart.defaults.scale.ticks.fontSize = 12;

    var _charts = {};
    var _shares = {};

    /**
     * Request settings to display a chart and call the method to initialize it.
     */
    $.on(document, 'chartLoad', function() {
        var form = $.dialogs.getActiveContent();
        if (!$.hasClass(form, 'chart-form')) {
            return;
        }

        $.ajax({
            method: 'GET',
            url: form.getAttribute('data-url')
        }, function(data) {
            var dlg = $.dialogs.getActiveDialog();
            data.content = dlg.getContent();
            _charts[dlg.getId()] = new ChartDetails(data);
        });
    });

    /**
     * Clean up when closing the chart dialog.
     */
    $.on(document, 'chartUnload', function() {
        var dlg = $.dialogs.getActiveDialog();
        var chart = _charts[dlg.getId()];
        if (chart) {
            chart.destroy();
        }
        delete _charts[dlg.getId()];
        document.dispatchEvent($.events.dashboardReload);
    });
    
    /**
     * Load the settings to display the chart share form.
     */
    $.on(document, 'chartShareLoad', function() {
        var form = $.dialogs.getActiveContent();
        if (!$.hasClass(form, 'chart-share-form')) {
            return;
        }

        var dlg = $.dialogs.getActiveDialog();
        _shares[dlg.getId()] = new ShareForm({ content: dlg.getContent(), formName: 'ChartShare' });
        _shares[dlg.getId()].run();
    });

    /**
     * Clean up when the chart share dialog closes.
     */
    $.on(document, 'chartShareUnload', function() {
        var dlg = $.dialogs.getActiveDialog();
        var share = _shares[dlg.getId()];
        if (share) {
            share.destroy();
        }
        delete _shares[dlg.getId()];
    });
})(this.$, this.Alertify, this.Chart, this.ChartDetails, this.ShareForm);