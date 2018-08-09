using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class UserController : BaseController
    {
        public UserController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new User(DbContext, (AppConfiguration)AppConfig));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(User model)
        {
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Users.SuccessDeletingUser;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(User model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index", new Table("tableUsers", Url.Action("List"),
                new List<TableColumn> {
                    new TableColumn("userName", Users.UserName, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("user.edit"))),
                    new TableColumn("firstName", Users.FirstName),
                    new TableColumn("lastName", Users.LastName),
                    new TableColumn("email", Users.Email),
                    new TableColumn("actions", Core.Actions, false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("user.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Users.UserLower)), User.IsInRole("user.delete"))
                        .AddIf(new TableLink($"{Url.Action("Unlock")}/{{id}}", Html.Classes(DashClasses.DashAjax, DashClasses.BtnInfo),
                            Users.Unlock, TableIcon.Unlock, HttpVerbs.Put,
                            new Dictionary<string, object>().Append("!!", new object[] { new Dictionary<string, object>().Append("var", "isLocked") })
                        ), User.IsInRole("user.unlock"))
                    )
                }
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<User>().Select(x => new { x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.IsLocked }));
        }

        [HttpPut]
        public IActionResult Unlock(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            model.Unlock();
            ViewBag.Message = Users.SuccessUnlockingUser;
            return Index();
        }

        private IActionResult CreateEditView(User model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(User model)
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
            if (!model.Save())
            {
                ViewBag.Error = model.Error;
                return CreateEditView(model);
            }
            ViewBag.Message = Users.SuccessSavingUser;
            return Index();
        }
    }
}
