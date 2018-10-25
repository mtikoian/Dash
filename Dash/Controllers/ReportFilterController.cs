using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ReportFilterController : BaseController
    {
        public ReportFilterController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
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
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
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
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ReportFilter model)
        {
            return Save(model);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult FilterCriteria(int id, int? columnId, int? operatorId)
        {
            var model = DbContext.Get<ReportFilter>(id) ?? new ReportFilter(DbContext, id);
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
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
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
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
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

        private IActionResult ReportRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(ReportController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
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
