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
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Roles.SuccessCopyingRole);
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
                return JsonError(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return JsonSuccess(Roles.SuccessDeletingRole);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Role>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
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
            return PartialView(new Table("tableRoles", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Roles.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", "Role", hasAccess: User.IsInRole("role.edit"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Edit")}/{{id}}", "Role", hasAccess: User.IsInRole("role.edit")),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Role", string.Format(Core.ConfirmDeleteBody, Roles.RoleLower), User.IsInRole("role.delete")),
                    Table.CopyButton($"{Url.Action("Copy")}/{{id}}", "Role", Roles.CopyBody, User.IsInRole("role.copy"))
                })
            }));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Role>());
        }

        private IActionResult CreateEditView(Role model)
        {
            return PartialView("CreateEdit", model);
        }

        private IActionResult Save(Role model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Roles.SuccessSavingRole);
        }
    }
}
