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
    public class DatasetJoinController : BaseController
    {
        public DatasetJoinController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Columns(int id, string tables)
        {
            return Json(new { columns = DbContext.Get<Dataset>(id)?.AvailableColumns(tables.Split(',')) ?? new List<object>() });
        }

        [HttpGet]
        public IActionResult Create(int datasetId)
        {
            return CreateEditView(new DatasetJoin(DbContext, datasetId));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(DatasetJoin model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<DatasetJoin>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingDataset;
            return Index(model.DatasetId);
        }

        private IActionResult DatasetRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(DatasetController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<DatasetJoin>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(DatasetJoin model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index(int datasetId)
        {
            RouteData.Values.Remove("id");
            var model = DbContext.Get<Dataset>(datasetId);
            model.Table = new Table("tableDatasetJoins", Url.Action("List", values: new { datasetId }), new List<TableColumn> {
                new TableColumn("name", Datasets.JoinTableName, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("dataset.edit"))),
                new TableColumn("keys", Datasets.JoinKeys),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("dataset.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Datasets.ConfirmDelete), User.IsInRole("dataset.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Datasets.NewName), User.IsInRole("dataset.copy"))
                )}
            );
            ViewBag.Title = Datasets.Joins;
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List(int datasetId)
        {
            return Rows(DbContext.Get<Dataset>(datasetId).DatasetJoin);
        }

        private IActionResult CreateEditView(DatasetJoin model)
        {
            ViewBag.Title = model.IsCreate ? Datasets.CreateDataset : Datasets.EditDataset;
            return View("CreateEdit", model);
        }

        private IActionResult Save(DatasetJoin model)
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
            ViewBag.Message = Datasets.SuccessSavingDataset;
            return Index(model.DatasetId);
        }
    }
}
