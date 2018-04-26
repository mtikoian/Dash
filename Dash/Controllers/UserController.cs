using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission")]
    public class UserController : BaseController
    {
        public UserController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new User(DbContext));
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] User model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return JsonSuccess(Users.SuccessDeletingUser);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            return CreateEditView(model);
        }

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] User model)
        {
            return Save(model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return JsonComponent(Component.Table, @Users.ViewAll, new Table("tableUsers", Url.Action("List"),
                new List<TableColumn> {
                    new TableColumn("uid", Users.UID, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("user.edit"))),
                    new TableColumn("firstName", Users.FirstName),
                    new TableColumn("lastName", Users.LastName),
                    new TableColumn("email", Users.Email),
                    new TableColumn("actions", Core.Actions, false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("user.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Users.UserLower)), User.IsInRole("user.delete")))
                },
                new List<TableHeaderButton>().AddIf(Table.CreateButton(Url.Action("Create"), Users.CreateUser), User.IsInRole("user.create")),
                GetList()
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(GetList());
        }

        private IActionResult CreateEditView(User model)
        {
            return PartialView("CreateEdit", model);
        }

        private IEnumerable<object> GetList()
        {
            return DbContext.GetAll<User>().Select(x => new { x.Id, x.UID, x.FirstName, x.LastName, x.Email });
        }

        private IActionResult Save(User model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            if (!model.Save())
            {
                return JsonError(model.Error);
            }
            return JsonSuccess(Users.SuccessSavingUser);
        }
    }
}
