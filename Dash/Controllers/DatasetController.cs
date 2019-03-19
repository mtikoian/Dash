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
        IActionResult CreateEditView(Dataset model)
        {
            if (!CanAccessDataset(model))
                return Index();

            return View("CreateEdit", model);
        }

        IActionResult Save(Dataset model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);
            if (!CanAccessDataset(model))
                return Index();

            model.Save(false, rolesOnly: true);
            ViewBag.Message = Datasets.SuccessSavingDataset;
            return CreateEditView(model);
        }

        protected bool CanAccessDataset(Dataset model)
        {
            if (model.IsCreate || CurrentUser.CanAccessDataset(model.Id))
                return true;
            ViewBag.Error = Datasets.ErrorPermissionDenied;
            return false;
        }

        public DatasetController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet, AjaxRequestOnly, ParentAction("Create,Edit")]
        public IActionResult Columns(int id)
        {
            if (!LoadModel(id, out Dataset model) || !CanAccessDataset(model))
                return Data(new { });
            return Data(model.AvailableColumns());
        }

        [HttpGet, ParentAction("Create")]
        public IActionResult Copy(CopyDataset model)
        {
            if (!ModelState.IsValid || !CanAccessDataset(model.Dataset))
                return Index();

            model.Save();
            ViewBag.Message = Datasets.SuccessCopyingDataset;
            return Index();
        }

        [HttpGet]
        public IActionResult Create() => CreateEditView(new Dataset(DbContext));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(Dataset model) => Save(model);

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Dataset model) || !CanAccessDataset(model))
                return Index();

            DbContext.Delete(model);
            ViewBag.Message = Datasets.SuccessDeletingDataset;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id) => LoadModel(id, out Dataset model) ? CreateEditView(model) : Index();

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(Dataset model) => Save(model);

        [HttpGet]
        public IActionResult Index()
        {
            RouteData.Values.Remove("id");
            return View("Index");
        }

        [HttpPost, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List() => Rows(DbContext.GetAll<Dataset>(new { UserId = User.UserId() }).Select(x => new { x.Id, x.Name, x.DatabaseName, x.DatabaseHost, x.PrimarySource, x.DatabaseId }));

        [HttpGet, AjaxRequestOnly, ParentAction("Create,Edit")]
        public IActionResult Sources(int? id = null, int? databaseId = null, int? typeId = null, string search = null)
        {
            if (id.HasPositiveValue())
            {
                var model = DbContext.Get<Dataset>(id.Value);
                if (model == null || !CanAccessDataset(model))
                    return Data(new { });
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
    }
}
