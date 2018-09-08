using System.Collections.Generic;
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
    public class ChartRangeController : BaseController
    {
        public ChartRangeController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            // clear modelState so that chartId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ChartRange(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(ChartRange model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<ChartRange>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
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
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ChartRange model)
        {
            return Save(model);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult Columns(int id, int? reportId)
        {
            var model = DbContext.Get<ChartRange>(id) ?? new ChartRange(DbContext, id);
            model.ReportId = reportId ?? model.ReportId;
            // clear modelState so that rangeId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_Columns", model);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult DateInterval(int id, int? xAxisColumnId)
        {
            var model = DbContext.Get<ChartRange>(id) ?? new ChartRange(DbContext, id);
            model.XAxisColumnId = xAxisColumnId ?? model.XAxisColumnId;
            // clear modelState so that rangeId isn't treated as the new model Id
            ModelState.Clear();
            return PartialView("_DateInterval", model);
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            var model = DbContext.Get<Chart>(id);
            model.Table = new Table("tableChartRanges", Url.Action("List", values: new { id }), new List<TableColumn> {
                new TableColumn("reportName", Charts.Report, Table.EditLink($"{Url.Action("Edit")}/{{chartId}}/{{id}}", User.IsInRole("chartrange.edit")), false),
                new TableColumn("xAxisColumnName", Charts.XAxisColumn, sortable: false),
                new TableColumn("yAxisColumnName", Charts.YAxisColumn, sortable: false),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{chartId}}/{{id}}"), User.IsInRole("reportfilter.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{chartId}}/{{id}}", Charts.ConfirmDeleteRange), User.IsInRole("chartrange.delete"))
                        .AddIf(Table.UpButton($"{Url.Action("MoveUp")}/{{chartId}}/{{id}}", jsonLogic: new Dictionary<string, object>().Append(">", new object[] { new Dictionary<string, object>().Append("var", "displayOrder"), 0 })), User.IsInRole("chartrange.edit"))
                        .AddIf(Table.DownButton($"{Url.Action("MoveDown")}/{{chartId}}/{{id}}", jsonLogic: new Dictionary<string, object>().Append("!", new object[] { new Dictionary<string, object>().Append("var", "isLast") })), User.IsInRole("chartrange.edit"))
                )}
            ) { StoreSettings = false };
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            var ranges = DbContext.Get<Chart>(id).ChartRange.OrderBy(x => x.DisplayOrder).ToList();
            if (ranges.Any())
            {
                ranges[ranges.Count() - 1].IsLast = true;
            }
            return Rows(ranges);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            var model = DbContext.Get<ChartRange>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            model.RequestUserId = User.UserId();
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
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            model.RequestUserId = User.UserId();
            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ChartId);
            }
            ViewBag.Message = Charts.SuccessSavingRange;
            return Index(model.ChartId);
        }

        private IActionResult CreateEditView(ChartRange model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult ChartRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(ChartController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
        }

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
            model.RequestUserId = User.UserId();
            DbContext.Save(model);
            ViewBag.Message = Charts.SuccessSavingRange;
            return Index(model.ChartId);
        }
    }
}
