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
        IActionResult CreateEditView(ChartRange model)
        {
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

            return View("CreateEdit", model);
        }

        IActionResult Save(ChartRange model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

            DbContext.Save(model);
            ViewBag.Message = Charts.SuccessSavingRange;
            return Index(model.ChartId);
        }

        protected bool IsOwner(Chart model)
        {
            if (model.IsOwner)
                return true;
            TempData["Error"] = Charts.ErrorOwnerOnly;
            return false;
        }

        public ChartRangeController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet, ParentAction("Edit")]
        public IActionResult Columns(int id, int chartId, int? reportId)
        {
            var model = DbContext.Get<ChartRange>(id) ?? new ChartRange(DbContext, id);
            model.ChartId = chartId;
            model.ReportId = reportId ?? model.ReportId;

            if (!LoadModel(model.ChartId, out Chart chart, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(chart))
                return RedirectToAction("Edit", "Chart", new { Id = chart.Id });
            model.Chart = chart;

            // clear modelState so that rangeId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_Columns", model);
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Chart model, true))
                return RedirectToAction("Index", "Chart");

            // clear modelState so that chartId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ChartRange(DbContext, id) { DisplayOrder = model.ChartRange.Count });
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(ChartRange model) => Save(model);

        [HttpGet, ParentAction("Edit")]
        public IActionResult DateInterval(int id, int? xAxisColumnId)
        {
            var model = DbContext.Get<ChartRange>(id) ?? new ChartRange(DbContext, id);
            model.XAxisColumnId = xAxisColumnId ?? model.XAxisColumnId;

            if (!LoadModel(model.ChartId, out Chart chart, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(chart))
                return RedirectToAction("Edit", "Chart", new { Id = chart.Id });
            model.Chart = chart;

            // clear modelState so that rangeId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_DateInterval", model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out ChartRange model, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

            // db will delete filter and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Charts.SuccessDeletingRange;
            return Index(model.ChartId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out ChartRange model, true) ? CreateEditView(model) : RedirectToAction("Index", "Chart");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(ChartRange model) => Save(model);

        [HttpGet]
        public IActionResult Index(int id)
        {
            if (!LoadModel(id, out Chart model, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(model))
                return RedirectToAction("Edit", "Chart", new { Id = model.Id });

            RouteData.Values.Remove("id");
            return View("Index", model);
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            if (!LoadModel(id, out Chart model, true))
                return Error(Core.ErrorInvalidId);
            if (!IsOwner(model))
                return Error(Charts.ErrorOwnerOnly);

            var ranges = model.ChartRange.OrderBy(x => x.DisplayOrder).ToList();
            if (ranges.Any())
                ranges[ranges.Count() - 1].IsLast = true;
            return Rows(ranges.Select(x => new { x.Id, x.ChartId, x.ReportName, x.XAxisColumnName, x.YAxisColumnName, x.DisplayOrder, x.IsLast }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            if (!LoadModel(id, out ChartRange model, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

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
            if (!LoadModel(id, out ChartRange model, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

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
