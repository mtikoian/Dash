﻿using System.Linq;
using System.Security.Claims;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Dash.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        /// <summary>
        /// Show the form for a user to request a password reset.
        /// </summary>
        /// <returns>Returns forgot password form.</returns>
        [HttpGet, AjaxRequestOnly]
        public ActionResult ForgotPassword([FromServices] IActionContextAccessor actionContextAccessor)
        {
            // @todo does this verify the user is logged in?
            if (User.Identity.IsAuthenticated)
            {
                return RedirectWithMessage("Dashboard", "Index", Core.ErrorAlreadyLoggedIn);
            }
            return PartialView(new ForgotPassword(actionContextAccessor));
        }

        /// <summary>
        /// Send the email with a link to reset a password.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Success or error message.</returns>
        [HttpPost, AjaxRequestOnly]
        public IActionResult ForgotPassword(ForgotPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectWithMessage("Dashboard", "Index", Core.ErrorAlreadyLoggedIn);
            }
            if (ModelState.IsValid)
            {
                if (model.Send(out string error))
                {
                    return JsonSuccess(Account.ForgotPasswordEmailSentText);
                }
                return JsonError(error);
            }
            return JsonError(ModelState.ToErrorString());
        }

        /// <summary>
        /// Display log on form.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectWithMessage("Dashboard", "Index", Core.ErrorAlreadyLoggedIn);
            }
            return View(new LogOn());
        }

        /// <summary>
        /// Log on a user.
        /// </summary>
        /// <param name="model">LogOn object</param>
        /// <returns>LogOn view on error, else redirects to index.</returns>
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LogOn model, [FromServices] ILogger<LogOn> logger)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectWithMessage("Dashboard", "Index", Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View(model);
            }

            if (!model.DoLogOn(out string error, DbContext, HttpContext, logger))
            {
                ViewBag.Error = error;
                return View(model);
            }
            // remove any errors from previous request
            ViewBag.Error = null;
            TempData.Remove("Error");
            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>
        /// Log the user off and redirect to the login page.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult LogOff()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return Json(new { Reload = true });
        }

        /// <summary>
        /// Form to reset a password from reset password email.
        /// </summary>
        /// <returns>Returns reset password form.</returns>
        [HttpGet]
        public ActionResult ResetPassword(ResetPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectWithMessage("Dashboard", "Index", Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                return RedirectWithError("Account", "LogOn", ModelState.ToErrorString());
            }
            return View(model);
        }

        /// <summary>
        /// Reset the user's password.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Redirect to LogOn on success, or reset password form on failure.</returns>
        [HttpPost]
        public ActionResult ResetPassword(ResetPassword model, bool resetting = false)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectWithMessage("Dashboard", "Index", Core.ErrorAlreadyLoggedIn);
            }
            if (ModelState.IsValid)
            {
                if (model.Reset(out string error, User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt()))
                {
                    return RedirectWithMessage("Account", "LogOn", Account.PasswordChangedText);
                }
                ViewBag.Error = error;
            }
            else
            {
                ViewBag.Error = ModelState.ToErrorString();
            }

            return View(model);
        }

        /// <summary>
        /// Toggle the context help setting for the session.
        /// </summary>
        /// <returns>Returns json object with message and help enabled flag.</returns>
        [HttpGet, AjaxRequestOnly, Authorize]
        public ActionResult ToggleContextHelp()
        {
            var wantsHelp = HttpContext.Session.GetString("ContextHelp").ToBool();
            wantsHelp = !wantsHelp;
            HttpContext.Session.SetString("ContextHelp", wantsHelp.ToString());
            return Json(new { message = wantsHelp ? Core.HelpEnabled : Core.HelpDisabled, enabled = wantsHelp });
        }

        /// <summary>
        /// Show form to update the user's account.
        /// </summary>
        /// <returns>Change password form.</returns>
        [HttpGet, Authorize]
        public ActionResult Update()
        {
            return PartialView(DbContext.GetAll<User>(new { UID = User.Identity.Name }).First());
        }

        /// <summary>
        /// Save changes to the users account.
        /// </summary>
        /// <param name="model">User object</param>
        /// <returns>Success or error message.</returns>
        [HttpPost, Authorize]
        public IActionResult Update(User model)
        {
            if (model.UpdateProfile(out string errorMsg))
            {
                return JsonSuccess(Account.AccountUpdated);
            }
            return JsonError(errorMsg);
        }
    }
}
