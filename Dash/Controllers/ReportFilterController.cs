using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ReportFilterController : BaseController
    {
        IActionResult CreateEditView(ReportFilter model)
        {
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });
            if (model.Columns.Count() == 0)
            {
                TempData["Error"] = model.Report.Dataset.IsProc ? Reports.ErrorNoProcParams : Reports.ErrorNoFilterColumns;
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });
            }

            return View("CreateEdit", model);
        }

        IActionResult Save(ReportFilter model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            model.Save();
            ViewBag.Message = Reports.SuccessSavingFilter;
            return Index(model.ReportId);
        }

        protected bool IsOwner(Report model)
        {
            if (model.IsOwner)
                return true;
            TempData["Error"] = Reports.ErrorOwnerOnly;
            return false;
        }

        public ReportFilterController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Report model, true))
                return RedirectToAction("Index", "Report");

            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ReportFilter(DbContext, id) { DisplayOrder = model.ReportFilter.Count });
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(ReportFilter model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out ReportFilter model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            // db will delete filter and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingFilter;
            return Index(model.ReportId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out ReportFilter model, true) ? CreateEditView(model) : RedirectToAction("Index", "Report");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(ReportFilter model) => Save(model);

        [HttpGet, ParentAction("Edit")]
        public IActionResult FilterCriteria(int id, int reportId, int? columnId, int? operatorId)
        {
            var model = DbContext.Get<ReportFilter>(id) ?? new ReportFilter(DbContext, id);
            model.ReportId = model.ReportId == 0 ? reportId : model.ReportId;
            model.ColumnId = columnId ?? model.ColumnId;
            model.OperatorId = operatorId ?? model.OperatorId;

            if (!LoadModel(model.ReportId, out Report report, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(report))
                return RedirectToAction("Edit", "Report", new { Id = report.Id });
            model.Report = report;

            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_FilterCriteria", model);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult FilterOperators(int id, int? columnId, int? reportId)
        {
            var model = DbContext.Get<ReportFilter>(id) ?? new ReportFilter(DbContext, id);
            model.ColumnId = columnId ?? model.ColumnId;
            model.ReportId = reportId ?? model.ReportId;

            if (!LoadModel(model.ReportId, out Report report, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(report))
                return RedirectToAction("Edit", "Report", new { Id = report.Id });
            model.Report = report;

            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_FilterOperators", model);
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            if (!LoadModel(id, out Report model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model))
                return RedirectToAction("Edit", "Report", new { Id = model.Id });

            RouteData.Values.Remove("id");
            return View("Index", model);
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            if (!LoadModel(id, out Report model, true))
                return Error(Core.ErrorInvalidId);
            if (!IsOwner(model))
                return Error(Reports.ErrorOwnerOnly);

            var filters = model.ReportFilter.OrderBy(x => x.DisplayOrder).ToList();
            if (filters.Any())
                filters[filters.Count() - 1].IsLast = true;
            return Rows(filters.Select(x => new { x.Id, x.IsLast, x.ReportId, x.DisplayOrder, x.ColumnName, x.CriteriaValue, x.Criteria2, x.OperatorValue }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            if (!LoadModel(id, out ReportFilter model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            if (!model.MoveDown(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ReportId);
            }
            ViewBag.Message = Reports.SuccessSavingFilter;
            return Index(model.ReportId);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveUp(int id)
        {
            if (!LoadModel(id, out ReportFilter model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ReportId);
            }
            ViewBag.Message = Reports.SuccessSavingFilter;
            return Index(model.ReportId);
        }
    }
}
