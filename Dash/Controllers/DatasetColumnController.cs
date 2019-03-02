using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DatasetColumnController : BaseController
    {
        private IActionResult CreateEditView(DatasetColumn model) => View("CreateEdit", model);

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

        public DatasetColumnController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Dataset");
            }
            // clear modelState so that datasetId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new DatasetColumn(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(DatasetColumn model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<DatasetColumn>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Dataset");
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
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Dataset");
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(DatasetColumn model) => Save(model);

        [HttpGet, ParentAction("Create")]
        public IActionResult Import(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                TempData["Error"] = Core.ErrorInvalidId;
                return RedirectToAction("Index", "Dataset");
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

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id) => Rows(DbContext?.GetAll<DatasetColumn>(new { DatasetId = id }).Select(x => new { x.Id, x.DatasetId, x.Title, x.ColumnName, x.DataTypeName }));
    }
}
