using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace Dash.Controllers
{
    /// <summary>
    /// Basic controller that handles displaying/modifying user dashboards.
    /// </summary>
    [Authorize(Policy = "HasPermission")]
    public class DashboardController : BaseController
    {
        private IActionContextAccessor _ActionContextAccessor;

        public DashboardController(IDbContext dbContext, IMemoryCache cache, AppConfiguration appConfig, IActionContextAccessor actionContextAccessor) :
                    base(dbContext, cache, appConfig)
        {
            _ActionContextAccessor = actionContextAccessor;
        }

        /// <summary>
        /// Create a new widget. Redirects to the shared CreateEditView.
        /// </summary>
        /// <returns>Create widget view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Create()
        {
            return CreateEditView(new Widget(DbContext, _ActionContextAccessor));
        }

        /// <summary>
        /// Handles form post to create a new widget and save to db.
        /// </summary>
        /// <returns>Error message, or success.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult Create(Widget model)
        {
            return Save(model);
        }

        /// <summary>
        /// Delete a widget.
        /// </summary>
        /// <param name="id">ID of widget to delete.</param>
        /// <returns>Redirects to index.</returns>
        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<Widget>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.AllowEdit)
            {
                return JsonError(Core.ErrorInvalidId);
            }

            DbContext.Delete(model);
            return JsonSuccess(Widgets.SuccessDeletingWidget);
        }

        /// <summary>
        /// Edit an existing widget. Redirects to the shared CreateEditView.
        /// </summary>
        /// <param name="id">Widget Id</param>
        /// <returns>Return edit widget view.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<Widget>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            if (!model.AllowEdit)
            {
                return JsonError(Core.ErrorInvalidId);
            }

            return CreateEditView(model);
        }

        /// <summary>
        /// Handles form post to update an existing widget and save to db.
        /// </summary>
        /// <param name="model">Widget object</param>
        /// <returns>Error message, or success.</returns>
        [HttpPut, AjaxRequestOnly]
        public IActionResult Edit(Widget model)
        {
            return Save(model);
        }

        /// <summary>
        /// Displays user's personal dashboard.
        /// </summary>
        /// <returns>Returns the dashboard page.</returns>
        public IActionResult Index()
        {
            return View(new WidgetList(DbContext, _ActionContextAccessor, User.UserId()));
        }

        /// <summary>
        /// Return an object with translations and other data needed to view the dashboard.
        /// </summary>
        /// <returns>Options object for viewing the dashboard.</returns>
        [HttpGet]
        public IActionResult IndexOptions()
        {
            return Json(new WidgetList(DbContext, _ActionContextAccessor, User.UserId()).Widgets);
        }

        /// <summary>
        /// Save the size/positions of all widgets.
        /// </summary>
        /// <param name="widgets">List of widgets</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult SaveDashboard(List<Widget> widgets)
        {
            var myWidgets = DbContext.GetAll<Widget>(new { UserId = User.UserId() });
            widgets.ForEach(x => {
                myWidgets.Where(w => w.Id == x.Id).FirstOrDefault()?.SavePosition(x.Width, x.Height, x.X, x.Y);
            });

            return JsonSuccess();
        }

        /// <summary>
        /// Displays start page.
        /// </summary>
        /// <returns>Returns the start page.</returns>
        [AjaxRequestOnly]
        public IActionResult Start()
        {
            return PartialView("Start");
        }

        /// <summary>
        /// Return an object with translations and other data needed to view the dashboard.
        /// </summary>
        /// <param name="id">Widget Id</param>
        /// <returns>Return JSON options for the widget.</returns>
        [HttpGet, AjaxRequestOnly]
        public IActionResult WidgetOptions(int id)
        {
            var model = DbContext.Get<Widget>(id);
            if (model == null)
            {
                return JsonError(Core.ErrorInvalidId);
            }
            return Json(model);
        }

        /// <summary>
        /// Display form to create or edit a widget.
        /// </summary>
        /// <param name="model">Widget to display. Will be an empty widget object for create.</param>
        /// <returns>Create/update widget view.</returns>
        private IActionResult CreateEditView(Widget model)
        {
            return PartialView("CreateEdit", model);
        }

        /// <summary>
        /// Processes a form post to create/edit a widget and save to db.
        /// </summary>
        /// <param name="model">Widget object to validate and save.</param>
        /// <returns>Error message, or success.</returns>
        private IActionResult Save(Widget model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError(ModelState.ToErrorString());
            }
            model.Save();
            return JsonSuccess(Widgets.SuccessSavingWidget);
        }
    }
}