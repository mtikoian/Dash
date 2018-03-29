using System.Linq;
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

        [HttpGet, AjaxRequestOnly]
        public IActionResult ForgotPassword([FromServices] IActionContextAccessor actionContextAccessor)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            return PartialView(new ForgotPassword(actionContextAccessor));
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult ForgotPassword(ForgotPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (ModelState.IsValid)
            {
                if (model.Send(out var error))
                {
                    return JsonSuccess(Account.ForgotPasswordEmailSentText);
                }
                return JsonError(error);
            }
            return JsonError(ModelState.ToErrorString());
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            return View(new LogOn());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(LogOn model, [FromServices] ILogger<LogOn> logger)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View(model);
            }

            if (!model.DoLogOn(out var error, DbContext, HttpContext, logger))
            {
                ViewBag.Error = error;
                return View(model);
            }
            // remove any errors from previous request
            ViewBag.Error = null;
            TempData.Remove("Error");
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult LogOff()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return Json(new { Reload = true });
        }

        [HttpGet]
        public IActionResult ResetPassword(ResetPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                return RedirectWithError("Account", "Login", ModelState.ToErrorString());
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPassword model, bool resetting = false)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (ModelState.IsValid)
            {
                if (model.Reset(out var error, User.Claims.First(x => x.Type == ClaimTypes.PrimarySid).Value.ToInt()))
                {
                    return RedirectWithError("Account", "Login", Account.PasswordChangedText);
                }
                ViewBag.Error = error;
            }
            else
            {
                ViewBag.Error = ModelState.ToErrorString();
            }

            return View(model);
        }

        [HttpGet, AjaxRequestOnly, Authorize]
        public IActionResult ToggleContextHelp()
        {
            var wantsHelp = HttpContext.Session.GetString("ContextHelp").ToBool();
            wantsHelp = !wantsHelp;
            HttpContext.Session.SetString("ContextHelp", wantsHelp.ToString());
            return Json(new { message = wantsHelp ? Core.HelpEnabled : Core.HelpDisabled, enabled = wantsHelp });
        }

        [HttpGet, Authorize]
        public IActionResult Update()
        {
            return PartialView(DbContext.GetAll<User>(new { UID = User.Identity.Name }).First());
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public IActionResult Update([FromBody] User model)
        {
            if (model.UpdateProfile(out var errorMsg))
            {
                return JsonSuccess(Account.AccountUpdated);
            }
            return JsonError(errorMsg);
        }
    }
}
