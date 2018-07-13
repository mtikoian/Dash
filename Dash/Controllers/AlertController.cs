using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission")]
    public class AlertController : BaseController
    {
        public AlertController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Copy(CopyAlert model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            return Success(Alerts.SuccessCopyingAlert);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Alert(DbContext, User.UserId()));
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] Alert model)
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
                return Error(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return Success(Alerts.SuccessDeletingAlert);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Alert>(id);
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            return CreateEditView(model);
        }

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Alert model)
        {
            return Save(model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableAlerts", Url.Action("List"),
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
                },
                new List<TableHeaderButton>().AddIf(Table.CreateButton(Url.Action("Create"), Alerts.CreateAlert), User.IsInRole("alert.create"))
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(GetList());
        }

        private IActionResult CreateEditView(Alert model)
        {
            return PartialView("CreateEdit", model);
        }

        private IEnumerable<object> GetList()
        {
            return DbContext.GetAll<Alert>(new { UserID = User.UserId() }).Select(x => new { x.Name, x.Subject, x.SendTo, x.Id, IsActive = x.IsActive ? Core.Yes : Core.No, x.LastRunDate });
        }

        private IActionResult Save(Alert model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Save();
            return Success(Alerts.SuccessSavingAlert);
        }
    }
}
