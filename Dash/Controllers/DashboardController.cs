using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission")]
    public class DashboardController : BaseController
    {
        private IActionContextAccessor _ActionContextAccessor;

        public DashboardController(IDbContext dbContext, AppConfiguration appConfig, IActionContextAccessor actionContextAccessor) : base(dbContext, appConfig)
        {
            _ActionContextAccessor = actionContextAccessor;
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Widget(DbContext, _ActionContextAccessor));
        }

        [HttpPost, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Create([FromBody] Widget model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var widget = DbContext.Get<Widget>(id);
            if (widget == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!widget.AllowEdit)
            {
                return JsonError(Core.ErrorInvalidId);
            }

            DbContext.Delete(widget);
            return JsonSuccess(Widgets.SuccessDeletingWidget);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var widget = DbContext.Get<Widget>(id);
            if (widget == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!widget.AllowEdit)
            {
                return JsonError(Core.ErrorInvalidId);
            }

            return CreateEditView(widget);
        }

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Widget model)
        {
            return Save(model);
        }

        public IActionResult Index()
        {
            return View(new WidgetList(DbContext, _ActionContextAccessor, User.UserId()));
        }

        [HttpGet]
        public IActionResult IndexOptions()
        {
            return JsonData(new WidgetList(DbContext, _ActionContextAccessor, User.UserId()).Widgets);
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult SaveDashboard([FromBody] SaveDashboard model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Update();
            return JsonSuccess();
        }

        [AjaxRequestOnly]
        public IActionResult Start()
        {
            return PartialView("Start");
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult WidgetOptions(int id)
        {
            var model = DbContext.Get<Widget>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            return JsonData(model);
        }

        private IActionResult CreateEditView(Widget model)
        {
            return PartialView("CreateEdit", model);
        }

        private IActionResult Save(Widget model)
        {
            if (model == null)
            {
                return JsonError(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Widgets.SuccessSavingWidget);
        }
    }
}
