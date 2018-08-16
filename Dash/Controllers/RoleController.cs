using System.Collections.Generic;
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
        public RoleController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly] // this might not have to be ajax only
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

        [HttpDelete, AjaxRequestOnly] // this might not have to be ajax only
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
            return View("Index", new Table("tableRoles", Url.Action("List"),
                new List<TableColumn> {
                    new TableColumn("name", Roles.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("role.edit"))),
                    new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("role.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Roles.RoleLower)), User.IsInRole("role.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Roles.CopyBody), User.IsInRole("role.copy")))
                }
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Role>());
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
