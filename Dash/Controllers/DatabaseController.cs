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
    public class DatabaseController : BaseController
    {
        public DatabaseController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Database());
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] Database model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            DbContext.Delete(model);
            return Success(Databases.SuccessDeletingDatabase);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            model.ConnectionString = model.ConnectionString.IsEmpty() ? null : new Crypt(AppConfig).Decrypt(model.ConnectionString);
            return CreateEditView(model);
        }

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Database model)
        {
            return Save(model);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Index()
        {
            return PartialView(new Table("tableDatabases", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Databases.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("database.edit"))),
                new TableColumn("databaseName", Databases.DatabaseName),
                new TableColumn("host", Databases.Host),
                new TableColumn("user", Databases.User),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("database.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Databases.DatabaseLower)), User.IsInRole("database.delete"))
                        .AddIf(new TableLink($"{Url.Action("TestConnection")}/{{id}}", Html.Classes(DashClasses.DashAjax, DashClasses.BtnInfo), Databases.TestConnection, TableIcon.HeartBeat),
                            User.IsInRole("database.testconnection"))
                )},
                new List<TableHeaderButton>().AddIf(Table.CreateButton(Url.Action("Create"), Databases.CreateDatabase), User.IsInRole("database.create"))
            ));
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult List()
        {
            return Rows(GetList());
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult TestConnection(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            return model.TestConnection(out var errorMsg) ? Success(Databases.SuccessTestingConnection) : Error(errorMsg);
        }

        private IActionResult CreateEditView(Database model)
        {
            return PartialView("CreateEdit", model);
        }

        private IEnumerable<object> GetList()
        {
            return DbContext.GetAll<Database>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.Host, x.User });
        }

        private IActionResult Save(Database model)
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
            return Success(Databases.SuccessSavingDatabase);
        }
    }
}
