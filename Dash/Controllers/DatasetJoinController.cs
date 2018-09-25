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
            return View("Index", DbContext.Get<Dataset>(id));
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
            DbContext.Save(model);
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }
    }
}
