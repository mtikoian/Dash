using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class RoleController : BaseController
    {
        public RoleController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, ParentAction("Create")]
        public IActionResult Copy(CopyRole model)
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
            ViewBag.Message = Roles.SuccessCopyingRole;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Role(DbContext));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Role model)
        {
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Role>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Roles.SuccessDeletingRole;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Role>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(Role model)
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
            return Rows(DbContext.GetAll<Role>().Select(x => new { x.Id, x.Name }));
        }

        private IActionResult CreateEditView(Role model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(Role model)
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
            model.Save();
            ViewBag.Message = Roles.SuccessSavingRole;
            return Index();
        }
    }
}
