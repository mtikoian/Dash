using System.Collections.Generic;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
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
            return CreateEditView(new DatasetColumn(DbContext, id));
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

        [HttpGet, ParentAction("Create")]
        public IActionResult Import(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return DatasetRedirect();
            }
            if (model.ImportSchema(User.UserId(), out var error))
            {
                ViewBag.Message = Datasets.SuccessReadingSchema;
            }
            else
            {
                ViewBag.Error = error;
            }
            return Index(id);
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
            return Rows(DbContext.Get<Dataset>(id).DatasetColumn);
        }

        private IActionResult CreateEditView(DatasetColumn model)
        {
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
            model.RequestUserId = User.UserId();
            DbContext.Save(model);
            ViewBag.Message = Datasets.SuccessSavingColumn;
            return Index(model.DatasetId);
        }
    }
}
