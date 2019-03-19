using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ChartShareController : BaseController
    {
        IActionResult CreateEditView(ChartShare model)
        {
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

            return View("CreateEdit", model);
        }

        IActionResult Save(ChartShare model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

            DbContext.Save(model);
            ViewBag.Message = Charts.SuccessSavingShare;
            return Index(model.ChartId);
        }

        protected bool IsOwner(Chart model)
        {
            if (model.IsOwner)
                return true;
            TempData["Error"] = Charts.ErrorOwnerOnly;
            return false;
        }

        public ChartShareController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Chart model, true))
                return RedirectToAction("Index", "Chart");

            // clear modelState so that chartId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ChartShare(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(ChartShare model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out ChartShare model, true))
                return RedirectToAction("Index", "Chart");
            if (!IsOwner(model.Chart))
                return RedirectToAction("Edit", "Chart", new { Id = model.ChartId });

            DbContext.Delete(model);
            ViewBag.Message = Charts.SuccessDeletingShare;
            return Index(model.ChartId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out ChartShare model, true) ? CreateEditView(model) : RedirectToAction("Index", "Chart");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(ChartShare model) => Save(model);

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

            return Rows(model.ChartShare.Select(x => new { x.Id, x.ChartId, x.UserName, x.RoleName }));
        }
    }
}
