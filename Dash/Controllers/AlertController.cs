using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class AlertController : BaseController
    {
        public AlertController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Copy(CopyAlert model)
        {
            if (model == null)
            {
                ViewBag.Error = Core.ErrorGeneric;
                return Index();
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return Index();
            }
            model.Save();
            ViewBag.Message = Alerts.SuccessCopyingAlert;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Alert(DbContext, User.UserId()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Alert model)
        {
            model.OwnerId = model.OwnerId == 0 ? User.UserId() : model.OwnerId;
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Alert>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Alerts.SuccessDeletingAlert;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Alert>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(Alert model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index", new Table("tableAlerts", Url.Action("List"),
                new List<TableColumn> {
                    new TableColumn("name", Alerts.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("alert.edit"))),
                    new TableColumn("subject", Alerts.Subject),
                    new TableColumn("sendTo", Alerts.SendTo),
                    new TableColumn("isActive", Alerts.IsActive),
                    new TableColumn("lastRunDate", Alerts.LastRunDate, dataType: TableDataType.Date),
                    new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("alert.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Alerts.AlertLower)), User.IsInRole("alert.delete"))
                        .AddIf(Table.CopyButton($"{Url.Action("Copy")}/{{id}}", Alerts.CopyBody), User.IsInRole("alert.copy")))
                }
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Alert>(new { UserID = User.UserId() }).Select(x => new { x.Name, x.Subject, x.SendTo, x.Id, IsActive = x.IsActive ? Core.Yes : Core.No, x.LastRunDate }));
        }

        private IActionResult CreateEditView(Alert model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(Alert model)
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
            model.Save();
            ViewBag.Message = Alerts.SuccessSavingAlert;
            return Index();
        }
    }
}
