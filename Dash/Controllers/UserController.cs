using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Dash.Controllers
{
    /// <summary>
    /// Handles CRUD for users.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class UserController : BaseController
    {
        public UserController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, IAppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Create a new user. Redirects to the shared CreateEditView.
        /// </summary>
        /// <returns>Create user view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new User());
        }

        /// <summary>
        /// Handles the form post to create a new user and save to db.
        /// </summary>
        /// <param name="model">User object to create</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Create(User model)
        {
            return Save(model);
        }

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="id">ID of user to delete</param>
        /// <returns>Success message.</returns>
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

        /// <summary>
        /// Edit an existing user. Redirects to the shared CreateEditView.
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns>Edit user view.</returns>
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

        /// <summary>
        /// Handles the form post to update an existing user and save to db.
        /// </summary>
        /// <param name="user">User object to edit.</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult Edit(User user)
        {
            return Save(user);
        }

        /// <summary>
        /// List of all users.
        /// </summary>
        /// <returns>Index view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableUsers", Url.Action("List"), new List<TableColumn> {
                new TableColumn("UID", Users.UID, Table.EditLink($"{Url.Action("Edit")}/{{id}}", "User")),
                new TableColumn("firstName", Users.FirstName),
                new TableColumn("lastName", Users.LastName),
                new TableColumn("email", Users.Email),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Edit")}/{{id}}", "User"),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "User", String.Format(Core.ConfirmDeleteBody, Users.UserLower))
                })
            }));
        }

        /// <summary>
        /// Return the user list for table to display.
        /// </summary>
        /// <returns>JSON array of user objects.</returns>
        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<User>());
        }

        /// <summary>
        /// Display form to create or edit a user.
        /// </summary>
        /// <param name="user">User to display. Will be an empty user object for create.</param>
        /// <returns>Create/edit user view.</returns>
        private IActionResult CreateEditView(User user)
        {
            return PartialView("CreateEdit", user);
        }

        /// <summary>
        /// Processes a form post to create/edit a user and save to db.
        /// </summary>
        /// <param name="user">User object to validate and save.</param>
        /// <returns>Success or error message.</returns>
        private IActionResult Save(User user)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            if (!user.Save())
            {
                return JsonError(user.Error);
            }
            return JsonSuccess(Users.SuccessSavingUser);
        }
    }
}