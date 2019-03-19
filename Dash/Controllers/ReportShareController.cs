using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ReportShareController : BaseController
    {
        IActionResult CreateEditView(ReportShare model)
        {
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            return View("CreateEdit", model);
        }

        IActionResult Save(ReportShare model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            DbContext.Save(model);
            ViewBag.Message = Reports.SuccessSavingShare;
            return Index(model.ReportId);
        }

        protected bool IsOwner(Report model)
        {
            if (model.IsOwner)
                return true;
            TempData["Error"] = Reports.ErrorOwnerOnly;
            return false;
        }

        public ReportShareController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Report model, true))
                return RedirectToAction("Index", "Report");

            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ReportShare(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(ReportShare model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out ReportShare model, true))
                return RedirectToAction("Index", "Report");
            if (!IsOwner(model.Report))
                return RedirectToAction("Edit", "Report", new { Id = model.ReportId });

            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingShare;
            return Index(model.ReportId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out ReportShare model, true) ? CreateEditView(model) : RedirectToAction("Index", "Report");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(ReportShare model) => Save(model);

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

            return Rows(model.ReportShare.Select(x => new { x.Id, x.ReportId, x.RoleName, x.UserName }));
        }
    }
}
