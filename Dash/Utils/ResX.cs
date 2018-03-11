using Dash.I18n;
using System.Collections.Generic;
using System.Security.Claims;

namespace Dash
{
    /// <summary>
    /// Define common translation resources that are serizalized for frontend use.
    /// </summary>
    public static class ResX
    {
        public static Dictionary<string, string> Build(ClaimsPrincipal user)
        {
            var resources = new Dictionary<string, string> {
                { "firstPage", Core.FirstPage },
                { "previousPage", Core.PreviousPage },
                { "nextPage", Core.NextPage },
                { "lastPage", Core.LastPage },
                { "noData", Core.NoData },
                { "showing", Core.Showing },
                { "page", Core.Page },
                { "perPage", Core.PerPage },
                { "close", Core.Close },
                { "save", Core.Save },
                { "okay", Core.Okay },
                { "cancel", Core.Cancel },
                { "errorConnectingToDataSource", Core.ErrorConnectingToDataSource },
                { "fixIt", Core.ErrorFixIt },
                { "loadingError", Core.LoadingError },
                { "tryAgain", Core.TryAgain },
                { "errorMatch", Core.ErrorMatch },
                { "errorMinLength", Core.ErrorMinLength },
                { "delete", Core.Delete },
                { "confirmDelete", Core.ConfirmDelete },
                { "areYouSure", Core.AreYouSure },
                { "moveUp", Core.MoveUp },
                { "moveDown", Core.MoveDown },
                { "noMatchesFound", Core.NoMatchesFound },
                { "tooManyRecords", Reports.ErrorTooManyRecords },
                { "widgetReloaded", Widgets.WidgetReloaded },
                { "reportModified", Reports.ErrorReportModified },
                { "errorChartLoad", Core.ErrorChartLoad },
                { "editWidget", Widgets.EditWidget },
                { "deleteWidget", Widgets.DeleteWidget },
                { "viewReport", Widgets.ViewReport },
                { "viewChart", Widgets.ViewChart },
                { "refresh", Widgets.Refresh },
                { "toggleFullScreen", Widgets.ToggleFullScreen },
                { "chartModified", Charts.ErrorChartModified },
                { "viewSql", Core.ViewSql },
                { "select", Core.Select },
                { "selectUser", Core.SelectUser },
                { "selectRole", Core.SelectRole },
                { "add", Core.Add},
                { "deleteAll", Core.DeleteAll},
                { "role", Core.Role},
                { "user", Core.User},
                { "confirmDeleteAll", Core.ConfirmDeleteAll },
                { "export", Core.Export },
                { "help", Core.Help },
                { "confirm", Core.Confirm },
                { "discardChanges", Core.DiscardChanges },
                { "errorNameRequired", Core.ErrorNameRequired }
            };

            if (user.IsInRole("dashboard.index") || user.IsInRole("chart.index"))
            {
                resources.AddRange(new Dictionary<string, string> {
                    { "chart.report", Charts.Report },
                    {  "chart.rangeName", Charts.Ranges },
                    { "chart.xAxisColumn", Charts.XAxisColumn },
                    { "chart.yAxisColumn", Charts.YAxisColumn },
                    { "chart.dateInterval", Charts.DateInterval },
                    { "chart.aggregator", Charts.Aggregator },
                    { "chart.saveSuccessful", Charts.SuccessSavingChart },
                    { "chart.color", Charts.Color },
                    { "chart.selectColor", Charts.SelectColor },
                    { "chart.rangesRequired", Charts.ErrorNoRanges },
                    { "chart.rangeText", ContextHelp.Chart_Ranges },
                    { "chart.reportText", ContextHelp.Chart_RangeReport },
                    { "chart.xAxisColumnText", ContextHelp.Chart_RangeXAxis },
                    { "chart.yAxisColumnText", ContextHelp.Chart_RangeYAxis },
                    { "chart.aggregatorText", ContextHelp.Chart_RangeAggregator },
                    { "chart.dateIntervalText", ContextHelp.Chart_RangeInterval },
                    { "chart.colorText", ContextHelp.Chart_RangeColor },
                    { "chart.noData", Charts.ErrorNoData }
                });
            }

            if (user.IsInRole("dashboard.index") || user.IsInRole("report.index"))
            {
                resources.AddRange(new Dictionary<string, string> {
                    { "report.selectDate", Reports.SelectDate },
                    { "report.true", Reports.True },
                    { "report.false", Reports.False },
                    { "report.filterCriteria", Reports.FilterCriteria },
                    { "report.filterCriteria2", Reports.FilterCriteria2 },
                    { "report.and", Reports.And },
                    { "report.filterOperator", Reports.FilterOperator },
                    { "report.filterColumn", Reports.FilterColumn },
                    { "report.filterColumnText", ContextHelp.Report_FilterColumn },
                    { "report.filterOperatorText", ContextHelp.Report_FilterOperator },
                    { "report.filterCriteriaText", ContextHelp.Report_FilterCriteria },
                    { "report.filters", Reports.Filters },
                    { "report.filterText", ContextHelp.Report_Filters },
                    { "report.aggregator", Reports.Aggregator },
                    { "report.groupColumn", Reports.GroupColumn },
                    { "report.groups", Reports.Groups },
                    { "report.groupText", ContextHelp.Report_Groups },
                    { "report.errorProcNoGroups", Reports.ErrorProcNoGroups }
                });
            }

            if (user.IsInRole("dataset.index"))
            {
                resources.AddRange(new Dictionary<string, string> {
                    { "dataset.joinTableName", Datasets.JoinTableName },
                    { "dataset.joinTableText", ContextHelp.DatasetJoin_TableName },
                    { "dataset.joinType", Datasets.JoinType },
                    { "dataset.joinTypeText", ContextHelp.DatasetJoin_JoinType },
                    { "dataset.joinKeys", Datasets.JoinKeys },
                    { "dataset.joinKeysText", ContextHelp.DatasetJoin_JoinKeys },
                    { "dataset.confirmImport", Datasets.ConfirmImportColumns },
                    { "dataset.import", Datasets.Import },
                    { "dataset.columnTitle", Datasets.ColumnTitle },
                    { "dataset.columnTitleText", ContextHelp.DatasetColumn_Title },
                    { "dataset.columnName", Datasets.ColumnName },
                    { "dataset.columnNameText", ContextHelp.DatasetColumn_ColumnId },
                    { "dataset.dataType", Datasets.ColumnDataType },
                    { "dataset.dataTypeText", ContextHelp.DatasetColumn_DataTypeId },
                    { "dataset.derived", Datasets.ColumnTransform },
                    { "dataset.derivedText", ContextHelp.DatasetColumn_Derived },
                    { "dataset.filterType", Datasets.ColumnFilterType },
                    { "dataset.filterTypeText", ContextHelp.DatasetColumn_FilterTypeId },
                    { "dataset.query", Datasets.ColumnQuery },
                    { "dataset.queryText", ContextHelp.DatasetColumn_FilterQuery },
                    { "dataset.link", Datasets.ColumnLink },
                    { "dataset.linkText", ContextHelp.DatasetColumn_Link },
                    { "dataset.isParam", Datasets.ColumnIsParam },
                    { "dataset.isParamText", ContextHelp.DatasetColumn_IsParam },
                    { "dataset.importErrorDatabaseRequired", Datasets.ImportErrorDatabaseRequired },
                    { "dataset.importErrorPrimarySourceRequired ", Datasets.ImportErrorPrimarySourceRequired },
                    { "dataset.importSuccess", Datasets.SuccessReadingSchema },
                    { "dataset.importErrorNoColumns", Datasets.ImportErrorNoColumnsRead },
                    { "dataset.errorProcNoJoins", Datasets.ErrorProcNoJoins },
                    { "dataset.importErrorNoProcs", Datasets.ImportErrorNoProcs },
                    { "dataset.primarySourceHelp", ContextHelp.Dataset_PrimarySource },
                    { "dataset.joinsText", ContextHelp.DatasetJoin_Details },
                    { "dataset.columnsText", ContextHelp.DatasetColumn_Details }
                });
            }

            return resources;
        }
    }
}