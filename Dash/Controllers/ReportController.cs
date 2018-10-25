using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ReportController : BaseController
    {
        public ReportController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, ParentAction("Create")]
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
            return View("Create", new CreateReport(DbContext, User.UserId()));
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
                DbContext = DbContext,
                DatasetId = model.DatasetId,
                Name = model.Name,
                Width = 0,
                OwnerId = userId,
                RequestUserId = userId
            };
            newReport.Save(false);
            ViewBag.Message = Reports.SuccessSavingReport;
            // select columns for new report after creating
            return SelectColumns(newReport.Id);
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

        [HttpDelete]
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
        public IActionResult Edit(int id)
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
            return View("Edit", report);
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
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Report>(new { UserId = User.UserId() }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult Rename(int id)
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
                return Edit(id);
            }
            return View("Rename", report);
        }

        [HttpPut, ParentAction("Edit")]
        public IActionResult Rename(RenameReport model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!model.Report.IsOwner)
            {
                ViewBag.Error = Reports.ErrorOwnerOnly;
                return Edit(model.Report.Id);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            ViewBag.Message = Reports.SuccessSavingReport;
            return Edit(model.Report.Id);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult SelectColumns(int id)
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
                return Edit(id);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanAccessDataset(report.DatasetId))
            {
                ViewBag.Error = Reports.ErrorInvalidDatasetId;
                return Edit(id);
            }
            return View("SelectColumns", report);
        }

        [HttpPut, ParentAction("Edit")]
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
            model.Update(User.UserId());
            ViewBag.Message = Reports.SuccessSavingReport;
            return Edit(model.Report.Id);
        }

        [HttpGet]
        public IActionResult Sql(int id)
        {
            var report = DbContext.Get<Report>(id);
            if (report == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return View("Sql", report.GetData(AppConfig, 0, report.RowLimit, true));
        }

        [HttpPost, AjaxRequestOnly]
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
            model.Update(User.UserId());
            return Success();
        }
    }
}
