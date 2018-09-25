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
    public class ReportShareController : BaseController
    {
        public ReportShareController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Report>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
            }
            // clear modelState so that reportId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ReportShare(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(ReportShare model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<ReportShare>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
            }
            DbContext.Delete(model);
            ViewBag.Message = Reports.SuccessDeletingShare;
            return Index(model.ReportId);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<ReportShare>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ReportRedirect();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ReportShare model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            var model = DbContext.Get<Report>(id);
            model.Table = new Table("tableReportShares", Url.Action("List", values: new { id }), new List<TableColumn> {
                new TableColumn("userName", Core.User, Table.EditLink($"{Url.Action("Edit")}/{{reportId}}/{{id}}", User.IsInRole("reportshare.edit"))),
                new TableColumn("roleName", Core.Role, Table.EditLink($"{Url.Action("Edit")}/{{reportId}}/{{id}}", User.IsInRole("reportshare.edit"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{reportId}}/{{id}}"), User.IsInRole("reportshare.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{reportId}}/{{id}}", Reports.ConfirmDeleteShare), User.IsInRole("reportshare.delete"))
                )}
            ) { StoreSettings = false };
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            return Rows(DbContext.Get<Report>(id).ReportShare);
        }

        private IActionResult CreateEditView(ReportShare model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult ReportRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(ReportController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
        }

        private IActionResult Save(ReportShare model)
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
            ViewBag.Message = Reports.SuccessSavingShare;
            return Index(model.ReportId);
        }
    }
}
