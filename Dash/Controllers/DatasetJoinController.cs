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

        [HttpGet]
        public IActionResult Create(int datasetId)
        {
            var model = DbContext.Get<Dataset>(datasetId);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            return CreateEditView(new DatasetJoin(DbContext, datasetId) { JoinOrder = model.DatasetJoin.Count });
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
            // db will delete join and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingJoin;
            return Index(model.DatasetId);
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
                new TableColumn("tableName", Datasets.JoinTableName, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("datasetjoin.edit")), false),
                new TableColumn("keys", Datasets.JoinKeys, sortable: false),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("datasetjoin.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", Datasets.ConfirmDeleteJoin), User.IsInRole("datasetjoin.delete"))
                        .AddIf(Table.UpButton($"{Url.Action("MoveUp")}/{{id}}"), User.IsInRole("datasetjoin.moveup"))
                        .AddIf(Table.DownButton($"{Url.Action("MoveDown")}/{{id}}"), User.IsInRole("datasetjoin.movedown"))
                )}
            );
            ViewBag.Title = Datasets.Joins;
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List(int datasetId)
        {
            return Rows(DbContext.Get<Dataset>(datasetId).DatasetJoin.OrderBy(x => x.JoinOrder));
        }

        [HttpGet]
        public IActionResult MoveDown(int id)
        {
            var model = DbContext.Get<DatasetJoin>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            if (!model.MoveDown(out var error))
            {
                ViewBag.Error = error;
                return Index(model.DatasetId);
            }
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }

        [HttpGet]
        public IActionResult MoveUp(int id)
        {
            var model = DbContext.Get<DatasetJoin>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.DatasetId);
            }
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }

        private IActionResult CreateEditView(DatasetJoin model)
        {
            ViewBag.Title = model.IsCreate ? Datasets.CreateJoin : Datasets.EditJoin;
            return View("CreateEdit", model);
        }

        private IActionResult DatasetRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(DatasetController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
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
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }
    }
}
