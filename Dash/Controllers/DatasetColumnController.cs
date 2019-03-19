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
        IActionResult CreateEditView(DatasetColumn model)
        {
            if (!CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            return View("CreateEdit", model);
        }

        IActionResult Save(DatasetColumn model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            DbContext.Save(model);
            ViewBag.Message = Datasets.SuccessSavingColumn;
            return Index(model.DatasetId);
        }

        protected bool CanAccessDataset(Dataset model)
        {
            if (CurrentUser.CanAccessDataset(model.Id))
                return true;
            TempData["Error"] = Datasets.ErrorPermissionDenied;
            return false;
        }

        public DatasetColumnController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Dataset model, true))
                return RedirectToAction("Index", "Dataset");

            // clear modelState so that datasetId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new DatasetColumn(DbContext, id) { Dataset = model });
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(DatasetColumn model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out DatasetColumn model, true) || !CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingColumn;
            return Index(model.DatasetId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out DatasetColumn model, true) ? CreateEditView(model) : RedirectToAction("Index", "Dataset");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(DatasetColumn model) => Save(model);

        [HttpGet, ParentAction("Create")]
        public IActionResult Import(int id)
        {
            if (!LoadModel(id, out Dataset model, true) || !CanAccessDataset(model))
                return RedirectToAction("Index", "Dataset");
            if (model.ImportSchema(User.UserId(), out var error))
                ViewBag.Message = Datasets.SuccessReadingSchema;
            else
                ViewBag.Error = error;

            return Index(id);
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            if (!LoadModel(id, out Dataset model, true) || !CanAccessDataset(model))
                return RedirectToAction("Index", "Dataset");

            RouteData.Values.Remove("id");
            return View("Index", model);
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id) => Rows(DbContext?.GetAll<DatasetColumn>(new { DatasetId = id }).Select(x => new { x.Id, x.DatasetId, x.Title, x.ColumnName, x.DataTypeName }));
    }
}
