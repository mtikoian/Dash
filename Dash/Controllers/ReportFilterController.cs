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
        public ReportFilterController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ReportFilter(DbContext, id) { DisplayOrder = model.ReportFilter.Count });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(ReportFilter model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<ReportFilter>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
            // db will delete filter and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingFilter;
            return Index(model.ReportId);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<ReportFilter>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ReportFilter model)
        {
            return Save(model);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult FilterCriteria(int id, int reportId, int? columnId, int? operatorId)
        {
            var model = DbContext.Get<ReportFilter>(id) ?? new ReportFilter(DbContext, id);
            model.ReportId = model.ReportId == 0 ? reportId : model.ReportId;
            model.ColumnId = columnId ?? model.ColumnId;
            model.OperatorId = operatorId ?? model.OperatorId;
            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_FilterCriteria", model);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult FilterOperators(int id, int? columnId)
        {
            var model = DbContext.Get<ReportFilter>(id) ?? new ReportFilter(DbContext, id);
            model.ColumnId = columnId ?? model.ColumnId;
            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_FilterOperators", model);
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            return View("Index", DbContext.Get<Report>(id));
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            var filters = DbContext.Get<Report>(id).ReportFilter.OrderBy(x => x.DisplayOrder).ToList();
            if (filters.Any())
            {
                filters[filters.Count() - 1].IsLast = true;
            }
            return Rows(filters.Select(x => new { x.Id, x.IsLast, x.ReportId, x.DisplayOrder, x.ColumnName, x.CriteriaValue, x.Criteria2, x.OperatorValue }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            var model = DbContext.Get<ReportFilter>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
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
            var model = DbContext.Get<ReportFilter>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ReportId);
            }
            ViewBag.Message = Reports.SuccessSavingFilter;
            return Index(model.ReportId);
        }

        private IActionResult CreateEditView(ReportFilter model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(ReportFilter model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return CreateEditView(model);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return CreateEditView(model);
            }
            DbContext.Save(model);
            ViewBag.Message = Reports.SuccessSavingFilter;
            return Index(model.ReportId);
        }
    }
}
