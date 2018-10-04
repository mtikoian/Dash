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
        private IActionContextAccessor _ActionContextAccessor;

        public DashboardController(IDbContext dbContext, AppConfiguration appConfig, IActionContextAccessor actionContextAccessor) : base(dbContext, appConfig)
        {
            _ActionContextAccessor = actionContextAccessor;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new Widget(DbContext));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(Widget model)
        {
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var widget = DbContext.Get<Widget>(id);
            if (widget == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            if (!widget.AllowEdit)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }

            DbContext.Delete(widget);
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var widget = DbContext.Get<Widget>(id);
            if (widget == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
            }
            if (!widget.AllowEdit)
            {
                ViewBag.Error = Core.ErrorInvalidId;
            }
            return CreateEditView(widget);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(Widget model)
        {
            return Save(model);
        }

        public IActionResult Index()
        {
            return View("Index", new WidgetList(DbContext, _ActionContextAccessor, User.UserId()));
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

        private IActionResult CreateEditView(Widget model)
        {
            ViewBag.ToDateTime = model.IsCreate ? Widgets.CreateWidget : Widgets.EditWidget;
            return View("CreateEdit", model);
        }

        private IActionResult Save(Widget model)
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
            model.UserId = model.UserId > 0 ? model.UserId : User.UserId();
            model.Save();
            ViewBag.Message = Widgets.SuccessSavingWidget;
            return Index();
        }
    }
}
