using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    /// <summary>
    /// Handles CRUD for datasets.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class DatasetController : BaseController
    {
        public DatasetController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        /// <summary>
        /// Get a list of all columns available to a dataset.
        /// </summary>
        /// <param name="id">ID of dataset to get tables for</param>
        /// <param name="table">Table name to get columns for</param>
        /// <returns>List of columns.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Columns(int id, string tables)
        {
            return Json(new { columns = DbContext.Get<Dataset>(id)?.AvailableColumns(tables.Split(',')) ?? new List<object>() });
        }

        /// <summary>
        /// Copy a dataset.
        /// </summary>
        /// <param name="model">Model with new name.</param>
        /// <returns>Success or error message.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyDataset model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Datasets.SuccessCopyingDataset);
        }

        /// <summary>
        /// Create a new data set. Redirects to the shared CreateEditView.
        /// </summary>
        /// <returns>Create dataset view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Dataset());
        }

        /// <summary>
        /// Handles the form post to create a new dataset and save to db.
        /// </summary>
        /// <param name="model">Dataset object to create</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        [IgnoreModelErrors("Database.*")]
        public IActionResult Create(Dataset model)
        {
            return Save(model);
        }

        /// <summary>
        /// Display a dataset to confirm delete.
        /// </summary>
        /// <param name="id">ID of dataset to display</param>
        /// <returns>Success message.</returns>
        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return JsonSuccess(Datasets.SuccessDeletingDataset);
        }

        /// <summary>
        /// Edit an existing dataset. Redirects to the shared CreateEditView.
        /// </summary>
        /// <param name="id">Dataset Id</param>
        /// <returns>Create dataset view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            return CreateEditView(model);
        }

        /// <summary>
        /// Handles form post to edit an existing dataset and save to db.
        /// </summary>
        /// <param name="model">Dataset object to update</param>
        /// <returns>Success or error message.</returns>
        [HttpPut, AjaxRequestOnly]
        [IgnoreModelErrors("Database.*")]
        public IActionResult Edit(Dataset model)
        {
            return Save(model);
        }

        /// <summary>
        /// Return a JSON object with translations and other data needed to edit a dataset.
        /// </summary>
        /// <param name="id">Dataset ID</param>
        /// <returns>Object with all the options for the dataset form.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult FormOptions(int? id)
        {
            var model = id.HasPositiveValue() ? DbContext.Get<Dataset>(id.Value) : null;
            return Json(new {
                joinTypes = typeof(JoinTypes).TranslatedList().Prepend(new { Id = 0, Name = Datasets.JoinType }),
                joins = model?.DatasetJoin,
                dataTypes = DbContext.GetAll<DataType>().OrderBy(d => d.Name).Select(x => new { x.Id, x.Name }).ToList()
                    .Prepend(new { Id = 0, Name = Datasets.ColumnDataType }),
                filterTypes = FilterType.FilterTypeList,
                columns = model?.DatasetColumn,
                wantsHelp = HttpContext.Session.GetString("ContextHelp").ToBool()
            });
        }

        /// <summary>
        /// List of all datasets.
        /// </summary>
        /// <returns>Index view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableDatasets", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Datasets.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", "Dataset", hasAccess: User.IsInRole("dataset.edit"))),
                new TableColumn("databaseName", Datasets.Database, Table.EditLink($"{Url.Action("Edit", "Database")}/{{databaseId}}", "Database", hasAccess: User.IsInRole("dataset.edit"))),
                new TableColumn("databaseHost", Datasets.Host),
                new TableColumn("primarySource", Datasets.PrimarySource),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink> {
                    Table.EditButton($"{Url.Action("Edit")}/{{id}}", "Dataset", hasAccess: User.IsInRole("dataset.edit")),
                    Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", "Dataset", Datasets.ConfirmDelete, hasAccess: User.IsInRole("dataset.delete")),
                    Table.CopyButton($"{Url.Action("Copy")}/{{id}}", "Dataset", Datasets.NewName, User.IsInRole("dataset.copy"))
                }),
            }));
        }

        /// <summary>
        /// Return the dataset list for table to display.
        /// </summary>
        /// <returns>Array of dataset objects.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Dataset>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.DatabaseHost, x.PrimarySource, x.DatabaseId }));
        }

        /// <summary>
        /// Read the schema from the database and create DatasetColumns.
        /// </summary>
        /// <param name="id">ID of database to connect to</param>
        /// <param name="sources">Tables to read schema for</param>
        /// <returns>Success message and list of new columns.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult ReadSchema(int databaseId, List<string> sources)
        {
            return Json(new { columns = new Dataset().ImportSchema(databaseId, sources) });
        }

        /// <summary>
        /// Get a list of tables/procs in a database.
        /// </summary>
        /// <param name="id">ID of database to get tables/procs for</param>
        /// <returns>List of tables or procs.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Sources(int databaseId, int typeId)
        {
            return Json(DbContext.Get<Database>(databaseId)?.GetSourceList(AppConfig, true, typeId == (int)DatasetTypes.Proc));
        }

        /// <summary>
        /// Get a list of columns in a table.
        /// </summary>
        /// <param name="id">ID of dataset to get tables for</param>
        /// <param name="table">Table name to get columns for</param>
        /// <returns>List of columns.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult TableColumns(int databaseId, List<string> tables)
        {
            // find table list
            var database = DbContext.Get<Database>(databaseId);
            if (database == null || tables == null || tables.Count == 0)
            {
                return Json("");
            }

            var list = new List<string>();
            tables.ForEach(x => {
                var schema = database.GetTableSchema(AppConfig, x);
                if (schema.Rows.Count > 0)
                {
                    foreach (System.Data.DataRow row in schema.Rows)
                    {
                        list.Add(row.ToColumnName(database.IsSqlServer));
                    }
                }
            });
            return Json(list.Distinct().OrderBy(x => x));
        }

        /// <summary>
        /// Displays form to create/edit a dataset.
        /// </summary>
        /// <param name="dataset">Dataset to display</param>
        /// <returns>Returns create/edit dataset view.</returns>
        private IActionResult CreateEditView(Dataset model)
        {
            if (model.Database == null && model.DatabaseId > 0)
            {
                model.Database = DbContext.Get<Database>(model.DatabaseId);
            }
            return PartialView("CreateEdit", model);
        }

        /// <summary>
        /// Processes a form post to create/edit a dataset and save to db.
        /// </summary>
        /// <param name="model">Dataset object to validate and save</param>
        /// <returns>Success or error message.</returns>
        private IActionResult Save(Dataset model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Datasets.SuccessSavingDataset);
        }
    }
}