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
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
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
