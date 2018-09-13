using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DatabaseController : BaseController
    {
        public DatabaseController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Database(DbContext, (AppConfiguration)AppConfig));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Database model)
        {
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Databases.SuccessDeletingDatabase;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            model.ConnectionString = model.ConnectionString.IsEmpty() ? null : new Crypt(AppConfig).Decrypt(model.ConnectionString);
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(Database model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index", new Table("tableDatabases", Url.Action("List"), new List<TableColumn> {
                new TableColumn("name", Databases.Name, Table.EditLink($"{Url.Action("Edit")}/{{id}}", User.IsInRole("database.edit"))),
                new TableColumn("databaseName", Databases.DatabaseName),
                new TableColumn("host", Databases.Host),
                new TableColumn("user", Databases.User),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{id}}"), User.IsInRole("database.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{id}}", string.Format(Core.ConfirmDeleteBody, Databases.DatabaseLower)), User.IsInRole("database.delete"))
                        .AddIf(new TableLink($"{Url.Action("TestConnection")}/{{id}}", Html.Classes(DashClasses.DashAjax, DashClasses.BtnInfo), Databases.TestConnection, TableIcon.HeartBeat),
                            User.IsInRole("database.create") || User.IsInRole("database.edit"))
                )}
            ));
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List()
        {
            return Rows(DbContext.GetAll<Database>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.Host, x.User }));
        }

        [HttpGet, ParentAction("Create,Edit")]
        public IActionResult TestConnection(int id)
        {
            var model = DbContext.Get<Database>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (model.TestConnection(out var errorMsg))
            {
                ViewBag.Message = Databases.SuccessTestingConnection;
            }
            else
            {
                ViewBag.Error = errorMsg;
            }
            return Index();
        }

        private IActionResult CreateEditView(Database model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(Database model)
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
            ViewBag.Message = Databases.SuccessSavingDatabase;
            return Index();
        }
    }
}
