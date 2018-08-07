using System.Collections.Generic;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DatasetColumnController : BaseController
    {
        public DatasetColumnController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int datasetId)
        {
            return CreateEditView(new DatasetColumn(DbContext, datasetId));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(DatasetColumn model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<DatasetColumn>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingColumn;
            return Index(model.DatasetId);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<DatasetColumn>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(DatasetColumn model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index(int datasetId)
        {
            RouteData.Values.Remove("id");
            var model = DbContext.Get<Dataset>(datasetId);
            model.Table = new Table("tableDatasetColumns", Url.Action("List", values: new { datasetId }), new List<TableColumn> {
                new TableColumn("title", Datasets.ColumnTitle, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("datasetcolumn.edit"))),
                new TableColumn("columnName", Datasets.ColumnName),
                new TableColumn("dataTypeName", Datasets.ColumnDataType),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("datasetcolumn.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Datasets.ConfirmDeleteColumn), User.IsInRole("datasetcolumn.delete"))
                )}
            );
            ViewBag.Title = Datasets.Columns;
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List(int datasetId)
        {
            return Rows(DbContext.Get<Dataset>(datasetId).DatasetColumn);
        }

        private IActionResult CreateEditView(DatasetColumn model)
        {
            ViewBag.Title = model.IsCreate ? Datasets.CreateColumn : Datasets.EditColumn;
            return View("CreateEdit", model);
        }

        private IActionResult DatasetRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(DatasetController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
        }

        private IActionResult Save(DatasetColumn model)
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
            DbContext.Save(model);
            ViewBag.Message = Datasets.SuccessSavingColumn;
            return Index(model.DatasetId);
        }
    }
}
