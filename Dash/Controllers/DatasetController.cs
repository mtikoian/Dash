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
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DatasetController : BaseController
    {
        public DatasetController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Columns(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                return Data(new { });
            }
            return Data(model.AvailableColumns());
        }

        [HttpGet]
        public IActionResult Copy(CopyDataset model)
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
            ViewBag.Message = Datasets.SuccessCopyingDataset;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Dataset(DbContext));
        }

        [HttpPost, IgnoreModelErrors("Database.*"), ValidateAntiForgeryToken]
        public IActionResult Create(Dataset model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingDataset;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, IgnoreModelErrors("Database.*"), ValidateAntiForgeryToken]
        public IActionResult Edit(Dataset model)
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

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index", new Table("tableDatasets", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Datasets.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("dataset.edit"))),
                new TableColumn("databaseName", Databases.DatabaseName, Table.EditLink($"{Url.Action("Edit", "Database")}/{{databaseId}}", User.IsInRole("database.edit"))),
                new TableColumn("databaseHost", Datasets.Host),
                new TableColumn("primarySource", Datasets.PrimarySource),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("dataset.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Datasets.ConfirmDelete), User.IsInRole("dataset.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Datasets.NewName), User.IsInRole("dataset.copy"))
                )}
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Dataset>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.DatabaseHost, x.PrimarySource, x.DatabaseId }));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Sources(int? id = null, int? databaseId = null, int? typeId = null, string search = null)
        {
            if (id.HasPositiveValue())
            {
                var model = DbContext.Get<Dataset>(id.Value);
                if (model == null)
                {
                    return Data(new { });
                }
                return Data(DbContext.Get<Database>(model.DatabaseId)?.GetSourceList(true, model.TypeId == (int)DatasetTypes.Proc));
            }
            if (databaseId.HasPositiveValue())
            {
                var results = DbContext.Get<Database>(databaseId.Value)?.GetSourceList(true, typeId.HasValue && typeId == (int)DatasetTypes.Proc);
                if (!search.IsEmpty())
                {
                    search = search.ToLower();
                    results = results.Where(x => search == null || x.ToLower().Contains(search));
                }
                return Data(results);
            }
            return Data(new { });
        }

        private IActionResult CreateEditView(Dataset model)
        {
            if (model.Database == null && model.DatabaseId > 0)
            {
                model.Database = DbContext.Get<Database>(model.DatabaseId);
            }
            return View("CreateEdit", model);
        }

        private IActionResult Save(Dataset model)
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
            model.Save(false);
            ViewBag.Message = Datasets.SuccessSavingDataset;
            return CreateEditView(model);
        }
    }
}
