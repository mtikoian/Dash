﻿using System.Collections.Generic;
using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class ChartShareController : BaseController
    {
        public ChartShareController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create(int id)
        {
            var model = DbContext.Get<Chart>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            // clear modelState so that chartId isn't treated as the new model Id
            ModelState.Clear();
            return CreateEditView(new ChartShare(DbContext, id));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(ChartShare model)
        {
            return Save(model);
        }

        [HttpDelete, AjaxRequestOnly]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<ChartShare>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            DbContext.Delete(model);
            ViewBag.Message = Charts.SuccessDeletingShare;
            return Index(model.ChartId);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<ChartShare>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return ChartRedirect();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(ChartShare model)
        {
            return Save(model);
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            RouteData.Values.Remove("id");
            var model = DbContext.Get<Chart>(id);
            model.Table = new Table("tableChartShares", Url.Action("List", values: new { id }), new List<TableColumn> {
                new TableColumn("userName", Core.User, Table.EditLink($"{Url.Action("Edit")}/{{chartId}}/{{id}}", User.IsInRole("chartshare.edit"))),
                new TableColumn("roleName", Core.Role, Table.EditLink($"{Url.Action("Edit")}/{{chartId}}/{{id}}", User.IsInRole("chartshare.edit"))),
                new TableColumn("actions", Core.Actions, sortable: false, links: new List<TableLink>()
                        .AddIf(Table.EditButton($"{Url.Action("Edit")}/{{chartId}}/{{id}}"), User.IsInRole("chartshare.edit"))
                        .AddIf(Table.DeleteButton($"{Url.Action("Delete")}/{{chartId}}/{{id}}", Charts.ConfirmDeleteShare), User.IsInRole("chartshare.delete"))
                )}
            ) { StoreSettings = false };
            return View("Index", model);
        }

        [HttpGet, AjaxRequestOnly, ParentAction("Index")]
        public IActionResult List(int id)
        {
            return Rows(DbContext.Get<Chart>(id).ChartShare);
        }

        private IActionResult CreateEditView(ChartShare model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult ChartRedirect()
        {
            var controller = (DatasetController)HttpContext.RequestServices.GetService(typeof(ChartController));
            controller.ControllerContext = ControllerContext;
            return controller.Index();
        }

        private IActionResult Save(ChartShare model)
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
            DbContext.Save(model);
            ViewBag.Message = Charts.SuccessSavingShare;
            return Index(model.ChartId);
        }
    }
}