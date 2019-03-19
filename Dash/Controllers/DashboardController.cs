using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class DashboardController : BaseController
    {
        readonly IActionContextAccessor _ActionContextAccessor;

        IActionResult CreateEditView(Widget model)
        {
            // @todo implement IsOwner checks based on UserCreated, standardize AllowEdit usage also
            return View("CreateEdit", model);
        }

        IActionResult Save(Widget model)
        {
            if (!ModelState.IsValid)
                return CreateEditView(model);

            model.UserId = model.UserId > 0 ? model.UserId : User.UserId();
            model.Save();
            ViewBag.Message = Widgets.SuccessSavingWidget;
            return Index();
        }

        public DashboardController(IDbContext dbContext, IAppConfiguration appConfig, IActionContextAccessor actionContextAccessor) : base(dbContext, appConfig) => _ActionContextAccessor = actionContextAccessor;

        [HttpGet]
        public IActionResult Create() => CreateEditView(new Widget(DbContext));

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Create(Widget model) => Save(model);

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!LoadModel(id, out Widget model) || !model.AllowEdit)
                return Index();

            DbContext.Delete(model);
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!LoadModel(id, out Widget model) || !model.AllowEdit)
                return Index();

            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Edit(Widget model) => Save(model);

        public IActionResult Index() => View("Index", new WidgetList(DbContext, _ActionContextAccessor, User.UserId()));

        [HttpPost, AjaxRequestOnly, ValidModel]
        public IActionResult SaveDashboard([FromBody] SaveDashboard model)
        {
            if (!ModelState.IsValid)
                return Error(ModelState.ToErrorString());

            model.Update();
            return Success();
        }
    }
}
