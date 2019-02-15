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
        public AlertController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, ParentAction("Create")]
        public IActionResult Copy(CopyAlert model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return Index();
            }
            model.Save();
            ViewBag.Message = Alerts.SuccessCopyingAlert;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Alert(DbContext, User.UserId()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Alert model)
        {
            model.OwnerId = model.OwnerId == 0 ? User.UserId() : model.OwnerId;
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Alert>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Alerts.SuccessDeletingAlert;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Alert>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(Alert model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Alert>(new { UserID = User.UserId() }).Select(x => new { x.Id, x.Name, x.Subject, IsActive = x.IsActive ? Core.Yes : Core.No, x.LastRunDate }));
        }

        private IActionResult CreateEditView(Alert model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(Alert model)
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
            ViewBag.Message = Alerts.SuccessSavingAlert;
            return Index();
        }
    }
}
