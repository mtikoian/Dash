using System.Collections.Generic;
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
        public ChartController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult ChangeType(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Edit(id);
            }
            return View("ChangeType", chart);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangeType(ChangeType model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("ChangeType", model);
            }
            model.Update();
            ViewBag.Message = Charts.SuccessSavingChart;
            return Edit(model.Id);
        }

        [HttpGet, ParentAction("Create")]
        public IActionResult Copy(CopyChart model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return Index();
            }
            model.Save();
            ViewBag.Message = Charts.SuccessCopyingChart;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("Create", new CreateChart(DbContext, User.UserId()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(CreateChart model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Create", model);
            }

            DbContext.Save(Chart.Create(model, User.UserId()));
            ViewBag.Message = Charts.SuccessSavingChart;
            return Index();
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult Data(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                return Error(Charts.ErrorPermissionDenied);
            }
            if ((chart.ChartRange?.Count ?? 0) == 0)
            {
                return Error(Charts.ErrorNoRanges);
            }
            return Data(chart.GetData(User.IsInRole("dataset.create")));
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Index();
            }
            DbContext.Delete(chart);
            ViewBag.Message = Charts.SuccessDeletingChart;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            var user = DbContext.Get<User>(User.UserId());
            if (!user.CanViewChart(chart))
            {
                ViewBag.Error = Charts.ErrorPermissionDenied;
                return Index();
            }
            return View("Edit", chart);
        }

        [HttpPost]
        public IActionResult Export(ExportChart model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            return File(model.Stream(), model.ContentType, model.FormattedFileName);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Chart>(new { UserId = User.UserId() }));
        }


        [HttpGet, ParentAction("Edit")]
        public IActionResult Rename(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Edit(id);
            }
            return View("Rename", chart);
        }

        [HttpPut, ParentAction("Edit")]
        public IActionResult Rename(RenameChart model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!model.Chart.IsOwner)
            {
                ViewBag.Error = Charts.ErrorOwnerOnly;
                return Edit(model.Chart.Id);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            ViewBag.Message = Charts.NameSaved;
            return Edit(model.Chart.Id);
        }

        [HttpGet]
        public IActionResult Sql(int id)
        {
            var chart = DbContext.Get<Chart>(id);
            if (chart == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return View("Sql", chart.GetData(true));
        }
    }
}
