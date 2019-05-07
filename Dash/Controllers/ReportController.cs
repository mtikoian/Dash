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
        protected bool IsOwner(Report model)
        {
            if (model.IsOwner)
                return true;
            ViewBag.Error = Reports.ErrorOwnerOnly;
            return false;
        }

        public ReportController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet, ParentAction("Create"), ValidModel]
        public IActionResult Copy(CopyReport model)
        {
            if (!ModelState.IsValid)
                return Index();

            model.Save();
            ViewBag.Message = Reports.SuccessCopyingReport;
            return Edit(model.Id);
        }

        [HttpGet]
        public IActionResult Create() => View("Create", new CreateReport(DbContext, User.UserId()));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(CreateReport model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            var newReport = new Report {
                DbContext = DbContext,
                DatasetId = model.DatasetId,
                Name = model.Name,
                RequestUserId = User.UserId()
            };
            newReport.Save(false);
            ViewBag.Message = Reports.SuccessCreatingReport;
            // select columns for new report after creating
            return SelectColumns(newReport.Id);
        }

        [HttpPost, AjaxRequestOnly, ValidModel]
        public IActionResult Data([FromBody] ReportData model)
        {
            if (!ModelState.IsValid)
                return Error(ModelState.ToErrorString());
            if (!CurrentUser.CanViewReport(model.Report))
                return Error(Reports.ErrorPermissionDenied);

            model.Update();
            return Data(model.GetResult());
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Report model) || !IsOwner(model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingReport;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!LoadModel(id, out Report model))
                return Index();
            if (!CurrentUser.CanViewReport(model))
            {
                ViewBag.Error = Reports.ErrorPermissionDenied;
                return Index();
            }
            if ((model.Dataset?.DatasetColumn?.Count ?? 0) == 0 || !CurrentUser.CanAccessDataset(model.Dataset.Id))
            {
                ViewBag.Error = Reports.ErrorGeneric;
                return Index();
            }

            if (model.ReportColumn.Count > 0 && !model.ReportColumn.Any(x => x.SortDirection != null))
            {
                model.ReportColumn[0].SortDirection = "asc";
                model.ReportColumn[0].SortOrder = 1;
            }
            return View("Edit", model);
        }

        [HttpGet]
        public IActionResult Export(int id)
        {
            if (!LoadModel(id, out Report model))
                return Error(Core.ErrorInvalidId);
            if (!CurrentUser.CanViewReport(model))
                return Error(Reports.ErrorPermissionDenied);
            if (model.Dataset?.DatasetColumn.Any() != true)
                return Error(Reports.ErrorNoColumnsSelected);

            var export = new ExportData { Report = model, AppConfig = AppConfig };
            return File(export.Stream(), export.ContentType, export.FormattedFileName);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<Report>(new { UserId = User.UserId() }).Select(x => new { x.Id, x.Name, x.DatasetName, x.DatasetId, x.IsOwner }));

        [HttpGet, ParentAction("Edit")]
        public IActionResult Rename(int id)
        {
            if (!LoadModel(id, out Report model))
                return Index();
            if (!IsOwner(model))
                return Edit(id);

            return View("Rename", model);
        }

        [HttpPut, ParentAction("Edit"), ValidModel]
        public IActionResult Rename(RenameReport model)
        {
            if (!ModelState.IsValid)
                return Error(ModelState.ToErrorString());
            if (!IsOwner(model.Report))
                return Error(Reports.ErrorOwnerOnly);

            model.Save();
            ViewBag.Message = Reports.SuccessSavingReport;
            return Edit(model.Report.Id);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult SelectColumns(int id)
        {
            if (!LoadModel(id, out Report model))
                return Index();
            if (!IsOwner(model))
                return Edit(id);

            return View("SelectColumns", model);
        }

        [HttpPut, ParentAction("Edit"), ValidModel]
        public IActionResult SelectColumns(SelectColumn model)
        {
            if (!ModelState.IsValid || !IsOwner(model.Report))
                return View("SelectColumns", model.Report);

            model.Update(User.UserId());
            ViewBag.Message = Reports.SuccessSavingReport;
            return Edit(model.Report.Id);
        }

        [HttpGet]
        public IActionResult Sql(int id) => LoadModel(id, out Report model) && CurrentUser.CanViewReport(model) ? View("Sql", model.GetData(AppConfig, 0, model.RowLimit, true)) : Index();

        [HttpPost, AjaxRequestOnly, ValidModel]
        public IActionResult UpdateColumnWidths([FromBody] UpdateColumnWidth model)
        {
            if (!ModelState.IsValid)
                return Error(ModelState.ToErrorString());
            if (!IsOwner(model.Report))
                return Error(Reports.ErrorOwnerOnly);

            model.Update(User.UserId());
            return Success();
        }
    }
}
