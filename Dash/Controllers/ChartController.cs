using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ChartController : BaseController
    {
        protected bool IsOwner(Chart model)
        {
            if (model.IsOwner)
                return true;
            ViewBag.Error = Charts.ErrorOwnerOnly;
            return false;
        }

        public ChartController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult ChangeType(int id)
        {
            if (!LoadModel(id, out Chart model))
                return Index();
            if (!IsOwner(model))
                return Edit(id);

            return View("ChangeType", model);
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult ChangeType(ChangeType model)
        {
            if (!ModelState.IsValid)
                return View("ChangeType", model);
            if (!IsOwner(model.Chart))
                return Edit(model.Chart.Id);

            model.Update();
            ViewBag.Message = Charts.SuccessSavingChart;
            return Edit(model.Id);
        }

        [HttpGet, ParentAction("Create"), ValidModel]
        public IActionResult Copy(CopyChart model)
        {
            if (!ModelState.IsValid)
                return Index();

            model.Save();
            ViewBag.Message = Charts.SuccessCopyingChart;
            return Edit(model.Id);
        }

        [HttpGet]
        public IActionResult Create() => View("Create", new CreateChart(DbContext, User.UserId()));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(CreateChart model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            var chart = Chart.Create(model, User.UserId());
            DbContext.Save(chart);
            TempData["Message"] = Charts.SuccessCreatingChart;
            return RedirectToAction("Create", "ChartRange", new { chart.Id });
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult Data(int id)
        {
            if (!LoadModel(id, out Chart model))
                return Error(Core.ErrorInvalidId);
            if (!CurrentUser.CanViewChart(model))
                return Error(Charts.ErrorPermissionDenied);
            if ((model.ChartRange?.Count ?? 0) == 0)
                return Error(Charts.ErrorNoRanges);
            return Data(model.GetData(false));
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Chart model) || !IsOwner(model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Charts.SuccessDeletingChart;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!LoadModel(id, out Chart model))
                return Index();
            if (!CurrentUser.CanViewChart(model))
            {
                ViewBag.Error = Charts.ErrorPermissionDenied;
                return Index();
            }

            return View("Edit", model);
        }

        [HttpPost, ValidModel]
        public IActionResult Export(ExportChart model) => ModelState.IsValid ? File(model.Stream(), model.ContentType, model.FormattedFileName) : Error(ModelState.ToErrorString());

        [HttpGet]
        public IActionResult Index()
        {
            // @todo modify table generation via Index so it can use the IsOwner column and conditionally hide the delete button
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<Chart>(new { UserId = User.UserId() }).Select(x => new { x.Id, x.Name, x.IsOwner }));

        [HttpGet, ParentAction("Edit")]
        public IActionResult Rename(int id)
        {
            if (!LoadModel(id, out Chart model))
                return Index();
            if (!IsOwner(model))
                return Edit(id);

            return View("Rename", model);
        }

        [HttpPut, ParentAction("Edit"), ValidModel]
        public IActionResult Rename(RenameChart model)
        {
            if (!ModelState.IsValid)
                return Error(ModelState.ToErrorString());
            if (!IsOwner(model.Chart))
                return Edit(model.Chart.Id);

            model.Save();
            ViewBag.Message = Charts.NameSaved;
            return Edit(model.Chart.Id);
        }

        [HttpGet]
        public IActionResult Sql(int id) => LoadModel(id, out Chart model) && CurrentUser.CanViewChart(model) ? View("Sql", model.GetData(true)) : Index();
    }
}
