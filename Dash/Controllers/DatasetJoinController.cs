using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
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
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            // clear modelState so that datasetId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new DatasetJoin(DbContext, id) { JoinOrder = model.DatasetJoin.Count });
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
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            var model = DbContext.Get<Dataset>(id);
            model.Table = new Table("tableDatasetJoins", Url.Action("List", values: new { id }), new List<TableColumn> {
                new TableColumn("tableName", Datasets.JoinTableName, Table.EditLink($"{Url.Action("Edit")}/{{datasetId}}/{{id}}", User.IsInRole("datasetjoin.edit")), false),
                new TableColumn("joinName", Datasets.JoinType, sortable: false),
                new TableColumn("keys", Datasets.JoinKeys, sortable: false),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{datasetId}}/{{id}}"), User.IsInRole("datasetjoin.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{datasetId}}/{{id}}", Datasets.ConfirmDeleteJoin), User.IsInRole("datasetjoin.delete"))
                        .AddIf(Table.UpButton($"{Url.Action("MoveUp")}/{{datasetId}}/{{id}}", jsonLogic: new Dictionary<string, object>().Append(">", new object[] { new Dictionary<string, object>().Append("var", "joinOrder"), 0 })), User.IsInRole("datasetjoin.edit"))
                        .AddIf(Table.DownButton($"{Url.Action("MoveDown")}/{{datasetId}}/{{id}}", jsonLogic: new Dictionary<string, object>().Append("!", new object[] { new Dictionary<string, object>().Append("var", "isLast") })), User.IsInRole("datasetjoin.edit"))
                )}
            ) { StoreSettings = false };
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            var joins = DbContext.Get<Dataset>(id).DatasetJoin.OrderBy(x => x.JoinOrder).ToList();
            if (joins.Any())
            {
                joins[joins.Count() - 1].IsLast = true;
            }
            return Rows(joins);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            var model = DbContext.Get<DatasetJoin>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            model.RequestUserId = User.UserId();
            if (!model.MoveDown(out var error))
            {
                ViewBag.Error = error;
                return Index(model.DatasetId);
            }
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveUp(int id)
        {
            var model = DbContext.Get<DatasetJoin>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            model.RequestUserId = User.UserId();
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
            model.RequestUserId = User.UserId();
            DbContext.Save(model);
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }
    }
}
