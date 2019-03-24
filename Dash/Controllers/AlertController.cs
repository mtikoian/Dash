using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class AlertController : BaseController
    {
        IActionResult CreateEditView(Alert model)
        {
            if (!model.IsCreate && !IsOwner(model))
                return Index();

            return View("CreateEdit", model);
        }

        IActionResult Save(Alert model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!model.IsCreate && !IsOwner(model))
                return Index();

            DbContext.Save(model);
            ViewBag.Message = Alerts.SuccessSavingAlert;
            return Index();
        }

        protected bool IsOwner(Alert model)
        {
            if (model.IsOwner)
                return true;
            ViewBag.Error = Alerts.ErrorOwnerOnly;
            return false;
        }

        public AlertController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet, ParentAction("Create"), ValidModel]
        public IActionResult Copy(CopyAlert model)
        {
            if (!ModelState.IsValid)
                return Index();

            model.Save();
            ViewBag.Message = Alerts.SuccessCopyingAlert;
            return Index();
        }

        [HttpGet]
        public IActionResult Create() => CreateEditView(new Alert(DbContext));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(Alert model) => Save(model);

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Alert model) || !IsOwner(model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Alerts.SuccessDeletingAlert;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out Alert model) ? CreateEditView(model) : Index();

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(Alert model) => Save(model);

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<Alert>(new { UserID = User.UserId() }).Select(x => new { x.Id, x.Name, x.Subject, IsActive = x.IsActive ? Core.Yes : Core.No, x.LastRunDate, x.IsOwner }));
    }
}
