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
    [Authorize(Policy = "HasPermission")]
    public class DatasetController : BaseController
    {
        public DatasetController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Columns(int id, string tables)
        {
            return Json(new { columns = DbContext.Get<Dataset>(id)?.AvailableColumns(tables.Split(',')) ?? new List<object>() });
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyDataset model)
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
            return JsonSuccess(Datasets.SuccessCopyingDataset);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Dataset(DbContext));
        }

        [HttpPost, AjaxRequestOnly]
        [IgnoreModelErrors("Database.*"), ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] Dataset model)
        {
            return Save(model);
        }

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

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        [IgnoreModelErrors("Database.*")]
        public IActionResult Edit([FromBody] Dataset model)
        {
            return Save(model);
        }

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

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return JsonRows(DbContext.GetAll<Dataset>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.DatabaseHost, x.PrimarySource, x.DatabaseId }));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult ReadSchema(int databaseId, List<string> sources)
        {
            return JsonData(new { columns = new Dataset(DbContext).ImportSchema(databaseId, sources) });
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Sources(int databaseId, int typeId)
        {
            return JsonData(DbContext.Get<Database>(databaseId)?.GetSourceList(true, typeId == (int)DatasetTypes.Proc));
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult TableColumns([FromBody] TableColumnList model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            if (model.Tables?.Any() != true)
            {
                return JsonData("");
            }
            return JsonData(model.GetList());
        }

        private IActionResult CreateEditView(Dataset model)
        {
            if (model.Database == null && model.DatabaseId > 0)
            {
                model.Database = DbContext.Get<Database>(model.DatabaseId);
            }
            return PartialView("CreateEdit", model);
        }

        private IActionResult Save(Dataset model)
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
            return JsonSuccess(Datasets.SuccessSavingDataset);
        }
    }
}
