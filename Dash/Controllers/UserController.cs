﻿using System.Linq;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dash.Controllers
{
    [Authorize(Policy = "HasPermission"), Pjax]
    public class UserController : BaseController
    {
        public UserController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult Create()
        {
            return CreateEditView(new User(DbContext, (AppConfiguration)AppConfig));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Create(User model)
        {
            return Save(model);
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            DbContext.Delete(model);
            ViewBag.Message = Users.SuccessDeletingUser;
            return Index();
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            return CreateEditView(model);
        }

        [HttpPut, ValidateAntiForgeryToken]
        public IActionResult Edit(User model)
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
            return Rows(DbContext.GetAll<User>().Select(x => new { x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.IsLocked }));
        }

        [HttpPut, ParentAction("Create,Edit")]
        public IActionResult Unlock(int id)
        {
            var model = DbContext.Get<User>(id);
            if (model == null)
            {
                ViewBag.Error = Core.ErrorInvalidId;
                return Index();
            }
            model.Unlock();
            ViewBag.Message = Users.SuccessUnlockingUser;
            return Index();
        }

        private IActionResult CreateEditView(User model)
        {
            return View("CreateEdit", model);
        }

        private IActionResult Save(User model)
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
            if (!model.Save())
            {
                ViewBag.Error = model.Error;
                return CreateEditView(model);
            }
            ViewBag.Message = Users.SuccessSavingUser;
            return Index();
        }
    }
}
