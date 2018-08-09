﻿using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
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
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return Index();
            }
            model.Save();
            ViewBag.Message = Reports.SuccessCopyingReport;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateReport(DbContext, User.UserId()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CreateReport model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return View("Create", model);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Create", model);
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
            ViewBag.Message = Reports.SuccessSavingReport;
            return Index();
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
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!report.IsOwner)
            {
                ViewBag.Error = Reports.ErrorOwnerOnly;
                return Index();
            }
            DbContext.Delete(report);
            ViewBag.Message = Reports.SuccessDeletingReport;
            return Index();
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewReport(report))
            {
                ViewBag.Error = Reports.ErrorPermissionDenied;
                return Index();
            }

            if ((report.Dataset?.DatasetColumn?.Count ?? 0) == 0 || !user.CanAccessDataset(report.Dataset.Id))
            {
                ViewBag.Error = Reports.ErrorGeneric;
                return Index();
            }

            if (report.ReportColumn.Count > 0 && !report.ReportColumn.Any(x => x.SortDirection != null))
            {
                report.ReportColumn[0].SortDirection = "asc";
                report.ReportColumn[0].SortOrder = 1;
            }
            return View("Details", report);
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

            var export = new ExportData { Report = report, AppConfig = AppConfig };
            return File(export.Stream(), export.ContentType, export.FormattedFileName);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index", new Table("tableReports", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Reports.Name, Table.EditLink($"{Url.Action("Details")}/{{id}}", User.IsInRole("report.details"))),
                new TableColumn("datasetName", Reports.Dataset, Table.EditLink($"{Url.Action("Edit", "Dataset")}/{{datasetId}}", User.IsInRole("dataset.edit"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Details")}/{{id}}"), User.IsInRole("report.details"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Reports.ConfirmDelete), User.IsInRole("report.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Reports.NewName), User.IsInRole("report.copy"))
                )}
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Report>(new { UserId = User.UserId() }));
        }

        [HttpPut]
        public IActionResult Rename(int id, string prompt)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!report.IsOwner)
            {
                ViewBag.Error = Reports.ErrorOwnerOnly;
                return Details(id);
            }
            if (prompt.IsEmpty())
            {
                ViewBag.Error = Reports.ErrorNameRequired;
                return Details(id);
            }

            report.Name = prompt.Trim();
            DbContext.Save(report, false);
            ViewBag.Message = Reports.SuccessSavingReport;
            return Details(id);
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

        [HttpGet]
        public IActionResult SelectColumns(int id, bool closeParent = true)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!report.IsOwner)
            {
                ViewBag.Error = Reports.ErrorOwnerOnly;
                return Details(id);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanAccessDataset(report.DatasetId))
            {
                ViewBag.Error = Reports.ErrorInvalidDatasetId;
                return Details(id);
            }
            report.AllowCloseParent = closeParent;
            return View("SelectColumns", report);
        }

        [HttpPut]
        public IActionResult SelectColumns(SelectColumn model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("SelectColumns", model.Report);
            }
            model.Update();
            ViewBag.Message = Reports.SuccessSavingReport;
            return Details(model.Report.Id);
        }

        [HttpGet]
        public IActionResult Share(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!report.IsOwner)
            {
                ViewBag.Error = Reports.ErrorOwnerOnly;
                return Details(id);
            }
            return View("Share", report);
        }

        [HttpPut]
        public IActionResult Share(SaveReportShare model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Share", model.Report);
            }
            model.Update();
            ViewBag.Message = Reports.SuccessSavingReport;
            return Details(model.Report.Id);
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
    }
}
