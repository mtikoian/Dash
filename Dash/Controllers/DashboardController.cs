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
                return Error(Core.ErrorInvalidId);
            }
            if (!widget.AllowEdit)
            {
                return Error(Core.ErrorInvalidId);
            }

            DbContext.Delete(widget);
            return Success(Widgets.SuccessDeletingWidget);
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var widget = DbContext.Get<Widget>(id);
            if (widget == null)
            {
                return Error(Core.ErrorInvalidId);
            }
            if (!widget.AllowEdit)
            {
                return Error(Core.ErrorInvalidId);
            }

            return CreateEditView(widget);
        }

        [HttpPut, AjaxRequestOnly, ValidateAntiForgeryToken]
        public IActionResult Edit([FromBody] Widget model)
        {
            return Save(model);
        }

        public IActionResult Index(bool withMenu = false)
        {
            var widgets = new WidgetList(DbContext, _ActionContextAccessor, User.UserId());
            if (Request.IsAjaxRequest())
            {
                if (withMenu)
                {
                    return PartialView("Body", widgets);
                }
                return PartialView(widgets);
            }
            return View(widgets);
        }

        [HttpGet]
        public IActionResult IndexOptions()
        {
            return Data(new WidgetList(DbContext, _ActionContextAccessor, User.UserId()).Widgets);
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult SaveDashboard([FromBody] SaveDashboard model)
        {
            if (model == null)
            {
                return Error(Core.ErrorGeneric);
            }
            if (!ModelState.IsValid)
            {
                return Error(ModelState.ToErrorString());
            }
            model.Update();
            return Success();
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
                return Error(Core.ErrorInvalidId);
            }
            return Data(model);
        }

        public IActionResult WidgetContent()
        {
            return PartialView("Index", new WidgetList(DbContext, _ActionContextAccessor, User.UserId()));
        }

        private IActionResult CreateEditView(Widget model)
        {
            return PartialView("CreateEdit", model);
        }

        private IActionResult Save(Widget model)
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
            return Success(Widgets.SuccessSavingWidget);
        }
    }
}
