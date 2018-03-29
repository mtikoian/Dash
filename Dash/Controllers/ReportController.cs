using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission")]
    public class ReportController : BaseController
    {
        public ReportController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        // @todo test and see which, if any, methods i could antiforgery to
        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyReport model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess();
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return PartialView(new CreateReport(DbContext, User.UserId()));
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] CreateReport model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }

            var userId = User.UserId();
            var newReport = new Report {
                DatasetId = model.DatasetId,
                Name = model.Name,
                Width = 0,
                OwnerId = userId,
                RequestUserId = userId
            };
            DbContext.Save(newReport, false);
            return JsonData(new { message = Reports.SuccessSavingReport, dialogUrl = Url.Action("SelectColumns", new { @id = newReport.Id, @closeParent = false }) });
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult Data([FromBody] ReportData model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonData(model.GetResult());
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            DbContext.Delete(report);
            return JsonSuccess(Reports.SuccessDeletingReport);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Details(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }

            if ((report.Dataset?.DatasetColumn?.Count ?? 0) == 0 || !user.CanAccessDataset(report.Dataset.Id))
            {
                return JsonError(Reports.ErrorGeneric);
            }

            if (report.ReportColumn.Count > 0 && !report.ReportColumn.Any(x => x.SortDirection != null))
            {
                report.ReportColumn[0].SortDirection = "asc";
                report.ReportColumn[0].SortOrder = 1;
            }

            return PartialView("Details", report);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult DetailsOptions(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }

            return JsonData(new {
                reportId = report.Id,
                allowEdit = report.IsOwner,
                loadAllData = report.Dataset.IsProc,
                wantsHelp = HttpContext.Session.GetString("ContextHelp").ToBool(),
                columns = report.Dataset.DatasetColumn.Select(x => new { x.Id, x.Title, x.FilterTypeId, x.IsParam })
                    .Prepend(new { Id = 0, Title = Reports.FilterColumn, FilterTypeId = 0, IsParam = true }),
                filterOperators = FilterType.FilterOperators,
                dateOperators = FilterType.DateOperators,
                filters = report.ReportFilter,
                lookups = report.Lookups(),
                saveFiltersUrl = Url.Action("SaveFilters", "Report", new { report.Id }),
                saveGroupsUrl = Url.Action("SaveGroups", "Report", new { report.Id }),
                saveColumnsUrl = Url.Action("UpdateColumnWidths", "Report", new { report.Id }),
                dataUrl = Url.Action("Data", "Report", new { report.Id }),
                exportUrl = Url.Action("Export", "Report", new { report.Id }),
                aggregators = report.AggregatorList.Prepend(new { Id = 0, Name = Reports.Aggregator }),
                groups = report.ReportGroup,
                aggregatorId = report.AggregatorId,
                dateFormat = report.Dataset.DateFormat,
                currencyFormat = report.Dataset.CurrencyFormat,
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
                rowLimit = report.RowLimit,
                sortColumns = report.SortColumns(),
                width = report.Width,
                reportColumns = report.ReportColumns(),
                countAggregatorId = (int)Aggregators.Count
            });
        }

        [HttpGet]
        public IActionResult Export(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                return JsonError(Reports.ErrorPermissionDenied);
            }
            if (report.Dataset?.DatasetColumn.Any() != true)
            {
                return JsonError(Reports.ErrorNoColumnsSelected);
            }

            var export = new ExportData { Report = report, HttpContext = HttpContext, AppConfig = AppConfig };
            return File(export.Stream(), export.ContentType, export.FormattedFileName);
        }

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

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Report>(new { UserId = User.UserId() }));
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult Rename(int id, string prompt)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            if (prompt.IsEmpty())
            {
                return JsonError(Reports.ErrorNameRequired);
            }

            report.Name = prompt.Trim();
            DbContext.Save(report, false);
            return JsonData(new { message = Reports.NameSaved, content = prompt });
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult SaveFilters([FromBody] SaveFilter model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            return JsonData(new { filters = model.Update() });
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult SaveGroups([FromBody] SaveGroup model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            return JsonData(new { groups = model.Update() });
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult SelectColumns(int id, bool closeParent = true)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanAccessDataset(report.DatasetId))
            {
                return JsonError(Reports.ErrorInvalidDatasetId);
            }
            report.AllowCloseParent = closeParent;
            return PartialView(report);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult SelectColumns([FromBody] SelectColumn model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonData(new {
                message = Reports.SuccessSavingReport,
                closeParent = model.AllowCloseParent,
                parentTarget = true,
                dialogUrl = Url.Action("Details", new { @id = model.Id })
            });
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Share(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return JsonError(Reports.ErrorOwnerOnly);
            }

            return PartialView(report);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult Share([FromBody] SaveReportShare model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonSuccess(Reports.SuccessSavingReport);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult UpdateColumnWidths([FromBody] UpdateColumnWidth model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonSuccess();
        }
    }
}
