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
        IActionResult CreateEditView(Role model) => View("CreateEdit", model);

        IActionResult Save(Role model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);

            model.Save();
            ViewBag.Message = Roles.SuccessSavingRole;
            return Index();
        }

        public RoleController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet, ParentAction("Create"), ValidModel]
        public IActionResult Copy(CopyRole model)
        {
            if (!ModelState.IsValid)
                return Index();

            model.Save();
            ViewBag.Message = Roles.SuccessCopyingRole;
            return Index();
        }

        [HttpGet]
        public IActionResult Create() => CreateEditView(new Role(DbContext));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(Role model) => Save(model);

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Role model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Roles.SuccessDeletingRole;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out Role model) ? CreateEditView(model) : Index();

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(Role model) => Save(model);

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<Role>().Select(x => new { x.Id, x.Name }));
    }
}
