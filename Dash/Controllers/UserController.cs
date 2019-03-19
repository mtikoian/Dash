using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class UserController : BaseController
    {
        IActionResult CreateEditView(User model) => View("CreateEdit", model);

        IActionResult Save(User model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);

            if (!model.Save())
            {
                ViewBag.Error = Users.ErrorSavingUser;
                return CreateEditView(model);
            }
            ViewBag.Message = Users.SuccessSavingUser;
            return Index();
        }

        public UserController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create() => CreateEditView(new User(DbContext, AppConfig));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(User model) => Save(model);

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out User model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Users.SuccessDeletingUser;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out User model) ? CreateEditView(model) : Index();

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(User model) => Save(model);

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<User>().Select(x => new { x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.IsLocked }));

        [HttpPut, ParentAction("Create,Edit")]
        public IActionResult Unlock(int id)
        {
            if (!LoadModel(id, out User model))
                return Index();

            model.Unlock();
            ViewBag.Message = Users.SuccessUnlockingUser;
            return Index();
        }
    }
}
