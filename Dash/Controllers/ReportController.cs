using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Dash.Controllers
{
    /// <summary>
    /// Handles CRUD for reports.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class ReportController : BaseController
    {
        public ReportController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Make a copy of a report.
        /// </summary>
        /// <param name="model">CopyReport object</param>
        /// <returns>Redirects to new report.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyReport model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return Details(model.Id);
        }

        /// <summary>
        /// Show datasets so user can select one to create a report from.
        /// </summary>
        /// <returns>Form to select dataset and enter name.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return PartialView(new CreateReport(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt()));
        }

        /// <summary>
        /// Create a new report.
        /// </summary>
        /// <param name="model">CreateReport object</param>
        /// <returns>Redirects to index.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Create(CreateReport model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }

            var newReport = new Report(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt()) { DatasetId = model.DatasetId, Name = model.Name, Width = 0 };
            DbContext.Save(newReport);
            return Json(new { message = Reports.SuccessSavingReport, dialogUrl = Url.Action("SelectColumns", new { @id = newReport.Id, @closeParent = false }) });
        }

        /// <summary>
        /// Gets the data for a report.
        /// </summary>
        /// <param name="id">Report Id</param>
        /// <param name="startItem">Record number to start with.</param>
        /// <param name="items">Maximum number of rows to return.</param>
        /// <param name="sorting">JSON object of sorting settings.</param>
        /// <param name="save">Save the changes if value=1.</param>
        /// <returns>Object with the queries run, retrieved data, and any errors.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Data(int id, int? startItem, int? items, IEnumerable<TableSorting> sort = null, bool? save = false)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewReport(model))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }
            if ((model.Dataset?.DatasetColumn?.Count ?? 0) == 0)
            {
                return JsonError(Reports.ErrorNoColumnsSelected);
            }

            var totalItems = items ?? model.RowLimit;
            if (save == true)
            {
                model.DataUpdate(totalItems, sort);
            }

            // return our results as json
            return Json(model.GetData(AppConfig, startItem ?? 0, totalItems, User.IsInRole("dataset.create")));
        }

        /// <summary>
        /// Delete a report.
        /// </summary>
        /// <param name="id">ID of report to delete</param>
        /// <returns>Success or error message.</returns>
        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            DbContext.Delete(model);
            return JsonSuccess(Reports.SuccessDeletingReport);
        }

        /// <summary>
        /// Display a report to view data and edit the report.
        /// </summary>
        /// <param name="id">ID of report to display.</param>
        /// <returns>Details view, or an error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Details(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewReport(model))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }

            if ((model.Dataset?.DatasetColumn?.Count ?? 0) == 0 || !user.CanAccessDataset(model.Dataset.Id))
            {
                return JsonError(Reports.ErrorGeneric);
            }

            if (model.ReportColumn.Count > 0 && !model.ReportColumn.Any(x => x.SortDirection != null))
            {
                model.ReportColumn[0].SortDirection = "asc";
                model.ReportColumn[0].SortOrder = 1;
            }

            return PartialView("Details", model);
        }

        /// <summary>
        /// Return an object with translations and other data needed to view a report.
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>Options object for viewing the report, or an error message.</returns>
        [HttpGet, ParentAction("Details"), AjaxRequestOnly]
        public IActionResult DetailsOptions(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewReport(model))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }

            return Json(new {
                reportId = model.Id,
                allowEdit = model.IsOwner,
                loadAllData = model.Dataset.IsProc,
                wantsHelp = HttpContextAccessor.HttpContext.Session.GetString("ContextHelp").ToBool(),
                columns = model.Dataset.DatasetColumn.Select(x => new { x.Id, x.Title, x.FilterTypeId, x.IsParam })
                    .Prepend(new { Id = 0, Title = Reports.FilterColumn, FilterTypeId = 0, IsParam = true }),
                filterOperators = FilterType.FilterOperators,
                dateOperators = FilterType.DateOperators,
                filters = model.ReportFilter,
                lookups = model.Lookups(),
                saveFiltersUrl = Url.Action("SaveFilters", "Report", new { model.Id }),
                saveGroupsUrl = Url.Action("SaveGroups", "Report", new { model.Id }),
                saveColumnsUrl = Url.Action("UpdateColumnWidths", "Report", new { model.Id }),
                dataUrl = Url.Action("Data", "Report", new { model.Id }),
                exportUrl = Url.Action("Export", "Report", new { model.Id }),
                aggregators = model.AggregatorList.Prepend(new { Id = 0, Name = Reports.Aggregator }),
                groups = model.ReportGroup,
                aggregatorId = model.AggregatorId,
                dateFormat = model.Dataset.DateFormat,
                currencyFormat = model.Dataset.CurrencyFormat,
                filterTypes = new {
                    boolean = (int)FilterTypes.Boolean,
                    date = (int)FilterTypes.Date,
                    select = (int)FilterTypes.Select,
                    numeric = (int)FilterTypes.Numeric
                },
                filterOperatorIds = new {
                    dateInterval = (int)FilterOperatorsAbstract.DateInterval,
                    equal = (int)FilterOperatorsAbstract.Equal,
                    range = (int)FilterOperatorsAbstract.Range,
                },
                rowLimit = model.RowLimit,
                sortColumns = model.SortColumns(),
                width = model.Width,
                reportColumns = model.ReportColumns(),
                countAggregatorId = (int)Aggregators.Count
            });
        }

        /// <summary>
        /// Gets the data for a report and return an Excel spreadsheet.
        /// </summary>
        /// <param name="id">Report Id</param>
        /// <returns>Streams an Excel spreadsheet.</returns>
        [HttpGet]
        public IActionResult Export(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanViewReport(model))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }
            if (model.Dataset?.DatasetColumn.Any() != true)
            {
                return JsonError(Reports.ErrorNoColumnsSelected);
            }

            new ExportData { Report = model }.Stream();
            return null;
        }

        /// <summary>
        /// List all reports.
        /// </summary>
        /// <returns>Index view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableReports", Url.Action("List"), new List<TableColumn>() {
                new TableColumn("name", Reports.Name, Table.EditLink($"{Url.Action("Details")}/{{id}}", "Report", "Details", User.IsInRole("report.details"))),
                new TableColumn("datasetName", Reports.Dataset, Table.EditLink($"{Url.Action("Edit", "Dataset")}/{{datasetId}}", "Dataset", hasAccess: User.IsInRole("dataset.edit"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Details")}/{{id}}", "Report", "Details", User.IsInRole("report.details")),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Report", Reports.ConfirmDelete, User.IsInRole("report.delete")),
                    Table.CopyButton($"{Url.Action("Copy")}/{{id}}", "Report", Reports.NewName, User.IsInRole("report.copy"))
                })
            }));
        }

        /// <summary>
        /// Return the report list for table to display.
        /// </summary>
        /// <returns>Array of report objects.</returns>
        [HttpGet, ParentAction("Index"), AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Report>(new { UserId = User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt() }));
        }

        /// <summary>
        /// Rename a report.
        /// </summary>
        /// <param name="id">ID of report to display.</param>
        /// <param name="prompt">New report name.</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult Rename(int id, string prompt)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            if (prompt.IsEmpty())
            {
                return JsonError(Reports.ErrorNameRequired);
            }

            model.Name = prompt.Trim();
            DbContext.Save(model, false);
            return Json(new { message = Reports.NameSaved, content = prompt });
        }

        /// <summary>
        /// Save the filters for a report.
        /// </summary>
        /// <param name="id">Report Id</param>
        /// <param name="filters">New filter objects to save</param>
        /// <returns>Error message, or list of updated filter objects.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult SaveFilters(int id, List<ReportFilter> filters = null)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }

            var newFilters = model.UpdateFilters(filters);
            return Json(new { filters = newFilters });
        }

        /// <summary>
        /// Save the groups for a report.
        /// </summary>
        /// <param name="id">Report Id</param>
        /// <param name="groups">New group objects to save</param>
        /// <returns>Error message, or list of updated group objects.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult SaveGroups(int id, int groupAggregator, List<ReportGroup> groups = null)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }

            var newGroups = model.UpdateGroups(groupAggregator, groups);
            return Json(new { groups = newGroups });
        }

        /// <summary>
        /// Show columns so user can select one ones to use in a report.
        /// </summary>
        /// <param name="id">Report ID</param>
        /// <returns>Form to select and order columns.</returns>
        [HttpGet, ParentAction("Create"), AjaxRequestOnly]
        public IActionResult SelectColumns(int id, bool closeParent = true)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanAccessDataset(model.DatasetId))
            {
                return JsonError(Reports.ErrorInvalidDatasetId);
            }
            model.CloseParent = closeParent;
            return PartialView(model);
        }

        /// <summary>
        /// Update the column list for an existing report.
        /// </summary>
        /// <param name="id">Report object</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, ParentAction("Create"), AjaxRequestOnly]
        public IActionResult SelectColumns(int id, List<ReportColumn> columns, bool closeParent = true)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            var user = DbContext.Get<User>(User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt());
            if (!user.CanAccessDataset(model.DatasetId))
            {
                return JsonError(Reports.ErrorInvalidDatasetId);
            }
            if (columns?.Any(x => x.DisplayOrder > 0) != true)
            {
                return JsonError(Reports.ErrorSelectColumn);
            }

            var myReport = DbContext.Get<Report>(model.Id);
            myReport.UpdateColumns(columns.Where(x => x.DisplayOrder > 0).ToList());
            return Json(new {
                message = Reports.SuccessSavingReport,
                closeParent = closeParent,
                parentTarget = true,
                dialogUrl = Url.Action("Details", new { @id = myReport.Id })
            });
        }

        /// <summary>
        /// Display a report to share with users/roles.
        /// </summary>
        /// <param name="id">ID of report to display.</param>
        /// <returns>Share view, or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Share(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }

            return PartialView(model);
        }

        /// <summary>
        /// Save the report shares.
        /// </summary>
        /// <param name="id">ID of report to update.</param>
        /// <param name="reportShare">List of shares.</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, ActionName("Share"), AjaxRequestOnly]
        public IActionResult ShareSave(int id, List<ReportShare> reportShare)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }

            reportShare?.ForEach(x => { x.ReportId = model.Id; DbContext.Save(x); });
            model.ReportShare.Where(x => reportShare == null || !reportShare.Any(s => s.Id == x.Id))
                .ToList().ForEach(x => DbContext.Delete(x));
            return JsonSuccess(Reports.SuccessSavingReport);
        }

        /// <summary>
        /// Update the list of selected columns for this report and the order of those columns.
        /// </summary>
        /// <param name="id">Report Id</param>
        /// <param name="columnWidths">Array of column widths</param>
        /// <param name="reportWidth">Total report width</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult UpdateColumnWidths(int id, List<TableColumnWidth> columnWidths, decimal reportWidth)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            if (columnWidths == null || model.ReportColumn == null || !model.ReportColumn.Any())
            {
                return JsonError(Reports.ErrorNoColumnsSelected);
            }

            model.UpdateColumnWidths(reportWidth, columnWidths);
            return JsonSuccess();
        }
    }
}