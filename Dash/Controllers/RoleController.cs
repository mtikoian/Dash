using System.Collections.Generic;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission")]
    public class RoleController : BaseController
    {
        public RoleController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyRole model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            return Success(Roles.SuccessCopyingRole);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Role(DbContext));
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] Role model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Role>(id);
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return Success(Roles.SuccessDeletingRole);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Role>(id);
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            return CreateEditView(model);
        }

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Role model)
        {
            return Save(model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return Component(Dash.Component.Table, Roles.ViewAll, new Table("tableRoles", Url.Action("List"),
                new List<TableColumn> {
                    new TableColumn("name", Roles.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("role.edit"))),
                    new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("role.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Roles.RoleLower)), User.IsInRole("role.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Roles.CopyBody), User.IsInRole("role.copy")))
                },
                new List<TableHeaderButton>().AddIf(Table.CreateButton(Url.Action("Create"), Roles.CreateRole), User.IsInRole("role.create")),
                GetList()
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(GetList());
        }

        private IActionResult CreateEditView(Role model)
        {
            return PartialView("CreateEdit", model);
        }

        private IEnumerable<Role> GetList()
        {
            return DbContext.GetAll<Role>();
        }

        private IActionResult Save(Role model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            return Success(Roles.SuccessSavingRole);
        }
    }
}
