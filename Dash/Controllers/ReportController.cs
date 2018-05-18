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

        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyReport model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            return Success();
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return PartialView(new CreateReport(DbContext, User.UserId()));
        }

        [AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] CreateReport model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
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
            return Data(new { message = Reports.SuccessSavingReport, dialogUrl = Url.Action("SelectColumns", new { @id = newReport.Id, @closeParent = false }) });
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult Data([FromBody] ReportData model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Update();
            return Data(model.GetResult());
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return Error(Reports.ErrorOwnerOnly);
            }
            DbContext.Delete(report);
            return Success(Reports.SuccessDeletingReport);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Details(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                return Error(Reports.ErrorPermissionDenied);
            }

            if ((report.Dataset?.DatasetColumn?.Count ?? 0) == 0 || !user.CanAccessDataset(report.Dataset.Id))
            {
                return Error(Reports.ErrorGeneric);
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
                return Error(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                return Error(Reports.ErrorPermissionDenied);
            }

            return Data(new {
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
                return Error(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                return Error(Reports.ErrorPermissionDenied);
            }
            if (report.Dataset?.DatasetColumn.Any() != true)
            {
                return Error(Reports.ErrorNoColumnsSelected);
            }

            var export = new ExportData { Report = report, HttpContext = HttpContext, AppConfig = AppConfig };
            return File(export.Stream(), export.ContentType, export.FormattedFileName);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return Component(Dash.Component.Table, Reports.ViewAll, new Table("tableReports", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Reports.Name, Table.EditLink($"{Url.Action("Details")}/{{id}}", User.IsInRole("report.details"))),
                new TableColumn("datasetName", Reports.Dataset, Table.EditLink($"{Url.Action("Edit", "Dataset")}/{{datasetId}}", User.IsInRole("dataset.edit"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Details")}/{{id}}"), User.IsInRole("report.details"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Reports.ConfirmDelete), User.IsInRole("report.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Reports.NewName), User.IsInRole("report.copy"))
                )},
                new List<TableHeaderButton>().AddIf(Table.CreateButton(Url.Action("Create"), Reports.CreateReport), User.IsInRole("report.create")),
                GetList()
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(GetList());
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult Rename(int id, string prompt)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return Error(Reports.ErrorOwnerOnly);
            }
            if (prompt.IsEmpty())
            {
                return Error(Reports.ErrorNameRequired);
            }

            report.Name = prompt.Trim();
            DbContext.Save(report, false);
            return Data(new { message = Reports.NameSaved, content = prompt });
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult SaveFilters([FromBody] SaveFilter model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            return Data(new { filters = model.Update() });
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult SaveGroups([FromBody] SaveGroup model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            return Data(new { groups = model.Update() });
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult SelectColumns(int id, bool closeParent = true)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return Error(Reports.ErrorOwnerOnly);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanAccessDataset(report.DatasetId))
            {
                return Error(Reports.ErrorInvalidDatasetId);
            }
            report.AllowCloseParent = closeParent;
            return PartialView(report);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult SelectColumns([FromBody] SelectColumn model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Update();
            return Data(new {
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
                return Error(Core.ErrorInvalidId);
            }
            if (!report.IsOwner)
            {
                return Error(Reports.ErrorOwnerOnly);
            }

            return PartialView(report);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult Share([FromBody] SaveReportShare model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Update();
            return Success(Reports.SuccessSavingReport);
        }

        [HttpPut, AjaxRequestOnly]
        public IActionResult UpdateColumnWidths([FromBody] UpdateColumnWidth model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Update();
            return Success();
        }

        private IEnumerable<Report> GetList()
        {
            return DbContext.GetAll<Report>(new { UserId = User.UserId() });
        }
    }
}
