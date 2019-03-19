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
        IActionResult CreateEditView(Database model) => View("CreateEdit", model);

        IActionResult Save(Database model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);

            model.Save();
            ViewBag.Message = Databases.SuccessSavingDatabase;
            return Index();
        }

        public DatabaseController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult Create() => CreateEditView(new Database(DbContext, AppConfig));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(Database model) => Save(model);

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Database model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Databases.SuccessDeletingDatabase;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!LoadModel(id, out Database model))
                return Index();

            model.ConnectionString = model.ConnectionString.IsEmpty() ? null : new Crypt(AppConfig).Decrypt(model.ConnectionString);
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(Database model) => Save(model);

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<Database>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.Host, x.User }));

        [HttpGet, ParentAction("Create,Edit")]
        public IActionResult TestConnection(int id)
        {
            if (!LoadModel(id, out Database model))
                return Index();
            if (model.TestConnection(out var errorMsg))
                ViewBag.Message = Databases.SuccessTestingConnection;
            else
                ViewBag.Error = errorMsg;
            return Index();
        }
    }
}
