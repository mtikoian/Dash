using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ReportGroupController : BaseController
    {
        IActionResult CreateEditView(ReportGroup model)
        {
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });
            if (model.Columns.Count() == 0)
            {
                TempData["Error"] = model.Report.Dataset.IsProc ? Reports.ErrorNoProcParams : Reports.ErrorNoGroupColumns;
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });
            }

            return View("CreateEdit", model);
        }

        IActionResult Save(ReportGroup model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            DbContext.Save(model);
            ViewBag.Message = Reports.SuccessSavingGroup;
            return Index(model.ReportId);
        }

        protected bool IsOwner(Report model)
        {
            if (model.IsOwner)
                return true;
            TempData["Error"] = Reports.ErrorOwnerOnly;
            return false;
        }

        public ReportGroupController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Report model, true))
                return RedirectToAction("Index", "Report");

            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ReportGroup(DbContext, id) { DisplayOrder = model.ReportGroup.Count });
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(ReportGroup model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out ReportGroup model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            // db will delete group and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingGroup;
            return Index(model.ReportId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out ReportGroup model, true) ? CreateEditView(model) : RedirectToAction("Index", "Report");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(ReportGroup model) => Save(model);

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

            var groups = model.ReportGroup.OrderBy(x => x.DisplayOrder).ToList();
            if (groups.Any())
                groups[groups.Count() - 1].IsLast = true;
            return Rows(groups.Select(x => new { x.Id, x.ReportId, x.DisplayOrder, x.ColumnName, x.IsLast }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            if (!LoadModel(id, out ReportGroup model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            if (!model.MoveDown(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ReportId);
            }
            ViewBag.Message = Reports.SuccessSavingGroup;
            return Index(model.ReportId);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveUp(int id)
        {
            if (!LoadModel(id, out ReportGroup model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.ReportId);
            }
            ViewBag.Message = Reports.SuccessSavingGroup;
            return Index(model.ReportId);
        }
    }
}
