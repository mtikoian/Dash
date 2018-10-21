using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DatasetController : BaseController
    {
        public DatasetController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Create,Edit")]
        public IActionResult Columns(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                return Data(new { });
            }
            return Data(model.AvailableColumns());
        }

        [HttpGet, ParentAction("Create")]
        public IActionResult Copy(CopyDataset model)
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
            ViewBag.Message = Datasets.SuccessCopyingDataset;
            return Index();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Dataset(DbContext));
        }

        [HttpPost, IgnoreModelErrors("Database.*"), ValidateAntiForgeryToken]
        public IActionResult Create(Dataset model)
        {
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingDataset;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Dataset>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, IgnoreModelErrors("Database.*"), ValidateAntiForgeryToken]
        public IActionResult Edit(Dataset model)
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
            return Rows(DbContext.GetAll<Dataset>().Select(x => new { x.Id, x.Name, x.DatabaseName, x.DatabaseHost, x.PrimarySource, x.DatabaseId }));
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Create,Edit")]
        public IActionResult Sources(int? id = null, int? databaseId = null, int? typeId = null, string search = null)
        {
            if (id.HasPositiveValue())
            {
                var model = DbContext.Get<Dataset>(id.Value);
                if (model == null)
                {
                    return Data(new { });
                }
                return Data(DbContext.Get<Database>(model.DatabaseId)?.GetSourceList(true, model.TypeId == (int)DatasetTypes.Proc));
            }
            if (databaseId.HasPositiveValue())
            {
                var results = DbContext.Get<Database>(databaseId.Value)?.GetSourceList(true, typeId.HasValue && typeId == (int)DatasetTypes.Proc);
                if (!search.IsEmpty())
                {
                    search = search.ToLower();
                    results = results.Where(x => search == null || x.ToLower().Contains(search));
                }
                return Data(results);
            }
            return Data(new { });
        }

        private IActionResult CreateEditView(Dataset model)
        {
            if (model.Database == null && model.DatabaseId > 0)
            {
                model.Database = DbContext.Get<Database>(model.DatabaseId);
            }
            return View("CreateEdit", model);
        }

        private IActionResult Save(Dataset model)
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
            model.Save(false, rolesOnly: true);
            ViewBag.Message = Datasets.SuccessSavingDataset;
            return CreateEditView(model);
        }
    }
}
