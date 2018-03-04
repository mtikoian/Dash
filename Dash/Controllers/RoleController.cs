using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace Dash.Controllers
{
    /// <summary>
    /// Handles CRUD for authorization roles.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class RoleController : BaseController
    {
        public RoleController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Copy a role.
        /// </summary>
        /// <param name="model">Model with new name.</param>
        /// <param name="role">Role object to copy.</param>
        /// <returns>Success or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyRole model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Roles.SuccessCopyingRole);
        }

        /// <summary>
        /// Create a new role. Redirects to the shared CreateEditView.
        /// </summary>
        /// <returns>Create role view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Role());
        }

        /// <summary>
        /// Handles form post to create a new role and save to db.
        /// </summary>
        /// <param name="model">Role object to save</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Create(Role model)
        {
            return Save(model);
        }

        /// <summary>
        /// Delete a role.
        /// </summary>
        /// <param name="id">ID of role to delete.</param>
        /// <returns>Success message.</returns>
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

        /// <summary>
        /// Edit an existing role. Redirects to the shared CreateEditView.
        /// </summary>
        /// <param name="id">Role Id</param>
        /// <returns>Edit role view.</returns>
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

        /// <summary>
        /// Handles form post to update an existing role.
        /// </summary>
        /// <param name="model">Role object</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult Edit(Role model)
        {
            return Save(model);
        }

        /// <summary>
        /// List of all roles.
        /// </summary>
        /// <returns>Index view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableRoles", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Roles.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", "Role")),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Edit")}/{{id}}", "Role"),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Role", String.Format(Core.ConfirmDeleteBody, Roles.RoleLower)),
                    Table.CopyButton($"{Url.Action("Copy")}/{{id}}", "Role", Roles.CopyBody)
                })
            }));
        }

        /// <summary>
        /// Return the role list for table to display.
        /// </summary>
        /// <returns>Array of role objects.</returns>
        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Role>());
        }

        /// <summary>
        /// Display form to create or edit a role.
        /// </summary>
        /// <param name="role">Role to display. Will be an empty role object for create.</param>
        /// <returns>Create/edit role view.</returns>
        private IActionResult CreateEditView(Role role)
        {
            return PartialView("CreateEdit", role);
        }

        /// <summary>
        /// Processes a form post to create/edit a role and save to db.
        /// </summary>
        /// <param name="model">Role object to validate and save.</param>
        /// <returns>Success or error message.</returns>
        private IActionResult Save(Role model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Roles.SuccessSavingRole);
        }
    }
}