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
    /// Handles CRUD for databases.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class DatabaseController : BaseController
    {
        public DatabaseController(IHttpContextAccessor httpContextAccessor, IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig) : base(httpContextAccessor, dbContext, cache, appConfig)
        {
        }

        /// <summary>
        /// Create a new database. Redirects to the shared CreateEditView.
        /// </summary>
        /// <returns>Create database view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Database());
        }

        /// <summary>
        /// Handles the form post to create a new database and save to db.
        /// </summary>
        /// <param name="model">Database object to create.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Create(Database model)
        {
            return Save(model);
        }

        /// <summary>
        /// Delete a database.
        /// </summary>
        /// <param name="id">ID of database to delete.</param>
        /// <returns>Success message</returns>
        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return JsonSuccess(Databases.SuccessDeletingDatabase);
        }

        /// <summary>
        /// Edit an existing database. Redirects to the shared CreateEditView.
        /// </summary>
        /// <param name="id">Database Id</param>
        /// <returns>Edit database view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            model.ConnectionString = model.ConnectionString.IsEmpty() ? null : new Crypt(AppConfig).Decrypt(model.ConnectionString);
            return CreateEditView(model);
        }

        /// <summary>
        /// Handles form post to edit an existing database and save to db.
        /// </summary>
        /// <param name="model">Database object to edit.</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult Edit(Database model)
        {
            return Save(model);
        }

        /// <summary>
        /// List of all databases.
        /// </summary>
        /// <returns>Index view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableDatabases", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Databases.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", "Database", hasAccess: User.IsInRole("database.edit"))),
                new TableColumn("databaseName", Databases.DatabaseName),
                new TableColumn("host", Databases.Host),
                new TableColumn("user", Databases.User),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                        Table.EditButton($"{Url.Action("Edit")}/{{id}}", "Database", hasAccess: User.IsInRole("database.edit")),
                        Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Database", String.Format(Core.ConfirmDeleteBody, Databases.DatabaseLower), User.IsInRole("database.delete")),
                    }.AddLink($"{Url.Action("TestConnection")}/{{id}}", Html.Classes(DashClasses.DashDialog, DashClasses.BtnInfo),
                        User.IsInRole("database.testconnection"), Databases.TestConnection, TableIcon.HeartBeat)
                )}
            ));
        }

        /// <summary>
        /// Return the database list for table to display.
        /// </summary>
        /// <returns>Array of database objects.</returns>
        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Database>());
        }

        /// <summary>
        /// Test a database connection.
        /// </summary>
        /// <param name="id">ID of database to test.</param>
        /// <returns>Success or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult TestConnection(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            return model.TestConnection(out string errorMsg) ? JsonSuccess(Databases.SuccessTestingConnection) : JsonError(errorMsg);
        }

        /// <summary>
        /// Display form to create/edit a database.
        /// </summary>
        /// <param name="model">Database to display. Will be an empty database object for create.</param>
        /// <returns>Create/edit database view.</returns>
        private IActionResult CreateEditView(Database model)
        {
            return PartialView("CreateEdit", model);
        }

        /// <summary>
        /// Processes a form post to create/edit a database and save to db.
        /// </summary>
        /// <param name="model">Database object to validate and save.</param>
        /// <returns>Success or error message.</returns>
        private IActionResult Save(Database model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Databases.SuccessSavingDatabase);
        }
    }
}