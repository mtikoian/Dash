using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DatasetJoinController : BaseController
    {
        IActionResult CreateEditView(DatasetJoin model)
        {
            if (!CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            return View("CreateEdit", model);
        }

        IActionResult Save(DatasetJoin model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            DbContext.Save(model);
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }

        protected bool CanAccessDataset(Dataset model)
        {
            if (CurrentUser.CanAccessDataset(model.Id))
                return true;
            TempData["Error"] = Datasets.ErrorPermissionDenied;
            return false;
        }

        public DatasetJoinController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create(int id)
        {
            if (!LoadModel(id, out Dataset model, true))
                return RedirectToAction("Index", "Dataset");

            // clear modelState so that datasetId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new DatasetJoin(DbContext, id) { JoinOrder = model.DatasetJoin.Count, Dataset = model });
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(DatasetJoin model) => Save(model);

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out DatasetJoin model, true) || !CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            // db will delete join and re-order remaining ones
            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingJoin;
            return Index(model.DatasetId);
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out DatasetJoin model, true) ? CreateEditView(model) : RedirectToAction("Index", "Dataset");

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(DatasetJoin model) => Save(model);

        [HttpGet]
        public IActionResult Index(int id)
        {
            if (!LoadModel(id, out Dataset model, true) || !CanAccessDataset(model))
                return RedirectToAction("Index", "Dataset");

            RouteData.Values.Remove("id");
            return View("Index", model);
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            if (!LoadModel(id, out Dataset model, true) || !CanAccessDataset(model))
                return RedirectToAction("Index", "Dataset");

            var joins = model.DatasetJoin.OrderBy(x => x.JoinOrder).ToList();
            if (joins.Any())
                joins[joins.Count() - 1].IsLast = true;
            return Rows(joins.Select(x => new { x.Id, x.DatasetId, x.TableName, x.JoinName, x.Keys, x.JoinOrder, x.IsLast }));
        }

        [HttpGet, ParentAction("Edit")]
        public IActionResult MoveDown(int id)
        {
            if (!LoadModel(id, out DatasetJoin model, true) || !CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

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
            if (!LoadModel(id, out DatasetJoin model, true) || !CanAccessDataset(model.Dataset))
                return RedirectToAction("Index", "Dataset");

            if (!model.MoveUp(out var error))
            {
                ViewBag.Error = error;
                return Index(model.DatasetId);
            }
            ViewBag.Message = Datasets.SuccessSavingJoin;
            return Index(model.DatasetId);
        }
    }
}
