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
        private IActionResult CreateEditView(ReportShare model) => View("CreateEdit", model);

        private IActionResult Save(ReportShare model)
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
            ViewBag.Message = Reports.SuccessSavingShare;
            return Index(model.ReportId);
        }

        public ReportShareController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig)
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
            return CreateEditView(new ReportShare(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(ReportShare model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<ReportShare>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingShare;
            return Index(model.ReportId);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<ReportShare>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Report");
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ReportShare model) => Save(model);

        [HttpGet]
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            return View("Index", DbContext.Get<Report>(id));
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id) => Rows(DbContext.GetAll<ReportShare>(new { ReportId = id }).Select(x => new { x.Id, x.ReportId, x.RoleName, x.UserName }));
    }
}
