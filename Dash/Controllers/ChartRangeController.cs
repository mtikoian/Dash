using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ChartRangeController : BaseController
    {
        private IActionResult CreateEditView(ChartRange model) => View("CreateEdit", model);

        private IActionResult Save(ChartRange model)
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
            ViewBag.Message = Charts.SuccessSavingRange;
            return Index(model.ChartId);
        }

        public ChartRangeController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult Columns(int id, int chartId, int? reportId)
        {
            var model = DbContext.Get<ChartRange>(id) ?? new ChartRange(DbContext, id);
            model.ChartId = chartId;
            model.ReportId = reportId ?? model.ReportId;
            // clear modelState so that rangeId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_Columns", model);
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Chart");
            }
            // clear modelState so that chartId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ChartRange(DbContext, id) { DisplayOrder = model.ChartRange.Count });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(ChartRange model) => Save(model);

        [HttpGet, ParentAction("Edit")]
        public IActionResult DateInterval(int id, int? xAxisColumnId)
        {
            var model = DbContext.Get<ChartRange>(id) ?? new ChartRange(DbContext, id);
            model.XAxisColumnId = xAxisColumnId ?? model.XAxisColumnId;
            // clear modelState so that rangeId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_DateInterval", model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<ChartRange>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Chart");
            }
            // db will delete filter and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Charts.SuccessDeletingRange;
            return Index(model.ChartId);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<ChartRange>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Chart");
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ChartRange model) => Save(model);

        [HttpGet]
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            return View("Index", DbContext.Get<Chart>(id));
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            var ranges = DbContext.Get<Chart>(id).ChartRange.OrderBy(x => x.DisplayOrder).ToList();
            if (ranges.Any())
            {
                ranges[ranges.Count() - 1].IsLast = true;
            }
            return Rows(ranges.Select(x => new { x.Id, x.ChartId, x.ReportName, x.XAxisColumnName, x.YAxisColumnName, x.DisplayOrder, x.IsLast }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            var model = DbContext.Get<ChartRange>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Chart");
            }
            if (!model.MoveDown(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ChartId);
            }
            ViewBag.Message = Charts.SuccessSavingRange;
            return Index(model.ChartId);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveUp(int id)
        {
            var model = DbContext.Get<ChartRange>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Chart");
            }
            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ChartId);
            }
            ViewBag.Message = Charts.SuccessSavingRange;
            return Index(model.ChartId);
        }
    }
}
