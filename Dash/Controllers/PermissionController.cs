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
    /// Handles CRUD for authorization permissions.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class PermissionController : BaseController
    {
        public PermissionController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Create a new permission. Redirects to the shared CreateEditView.
        /// </summary>
        /// <returns>Create permission view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Permission());
        }

        /// <summary>
        /// Handles form post to create a new permission and save to db.
        /// </summary>
        /// <param name="model">Permission object to create.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Create([FromBody]Permission model)
        {
            return Save(model);
        }

        /// <summary>
        /// Create multiple permissions for a controller.
        /// </summary>
        /// <returns>Create controller view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult CreateController()
        {
            return PartialView(new CreateController());
        }

        /// <summary>
        /// Create new permissions for a controller.
        /// </summary>
        /// <param name="controllerName">Name of the controller for the new actions.</param>
        /// <param name="controllerActions">List of action names to add.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult CreateController(CreateController model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Permissions.SuccessCreatingController);
        }

        /// <summary>
        /// Delete a permission.
        /// </summary>
        /// <param name="id">ID of permission to delete.</param>
        /// <returns>Success message.</returns>
        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Permission>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return JsonSuccess(Permissions.SuccessDeletingPermission);
        }

        /// <summary>
        /// Edit an existing permission. Redirects to the shared CreateEditView.
        /// </summary>
        /// <param name="id">Permission Id</param>
        /// <returns>Edit permission view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Permission>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            return CreateEditView(model);
        }

        /// <summary>
        /// Handles form post to update an existing permission and save to db.
        /// </summary>
        /// <param name="permission">Permission object</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult Edit([FromBody]Permission model)
        {
            return Save(model);
        }

        /// <summary>
        /// List of all permissions.
        /// </summary>
        /// <returns>Index view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tablePermissions", Url.Action("List"), new List<TableColumn>() {
                new TableColumn("fullName", Permissions.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", "Permission", hasAccess: User.IsInRole("permission.edit"))),
                new TableColumn("controllerName", Permissions.Controller),
                new TableColumn("actionName", Permissions.Action),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Edit")}/{{id}}", "Permission", hasAccess: User.IsInRole("permission.edit")),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Permission", String.Format(Core.ConfirmDeleteBody, Permissions.PermissionLower), User.IsInRole("permission.delete"))
                })
            }));
        }

        /// <summary>
        /// Return the permission list for table to display.
        /// </summary>
        /// <returns>Array of permission objects.</returns>
        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Permission>());
        }

        /// <summary>
        /// Display form to create or edit a permission.
        /// </summary>
        /// <param name="permission">Permission to display. Will be an empty permission object for create.</param>
        /// <returns>Create/update permission view.</returns>
        private IActionResult CreateEditView(Permission model)
        {
            return PartialView("CreateEdit", model);
        }

        /// <summary>
        /// Processes a form post to create/edit a permission and save to db.
        /// </summary>
        /// <param name="model">Permission object to validate and save.</param>
        /// <returns>Success or error message.</returns>
        private IActionResult Save(Permission model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            DbContext.Save(model);
            return JsonSuccess(Permissions.SuccessSavingPermission);
        }
    }
}