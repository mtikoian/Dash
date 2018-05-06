using System.Linq;
using System.Security.Claims;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Dash.Controllers
{
    public class AccountController : BaseController
    {
        public AccountController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet, AjaxRequestOnly]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            return PartialView(new ForgotPassword());
        }

        [HttpPost, AjaxRequestOnly]
        public IActionResult ForgotPassword([FromBody] ForgotPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (ModelState.IsValid)
            {
                if (model.Send(out var error, HttpContext, new UrlHelper(ControllerContext)))
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
        public IActionResult Login(LogOn model)
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

            if (!model.DoLogOn(out var error, DbContext, AppConfig, HttpContext))
            {
                ViewBag.Error = error;
                return View(model);
            }
            if (model.Membership.AllowSingleFactor)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View("TwoFactorLogin", model.Membership);
        }

        [HttpGet]
        public IActionResult LogOff()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return Json(new { Reload = true });
        }

        [HttpGet]
        public IActionResult ResetPassword([FromQuery] ResetPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Login", new LogOn());
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword([FromForm] ResetPassword model, bool resetting = false)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            if (ModelState.IsValid)
            {
                if (model.Reset(out var error))
                {
                    ViewBag.Error = ModelState.ToErrorString();
                    return View("Login", new LogOn());
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

        [HttpGet]
        public IActionResult TwoFactorHelp(string username)
        {
            if (User.Identity.IsAuthenticated)
            {
                return JsonError(Core.ErrorAlreadyLoggedIn);
            }
            var membership = DbContext.GetAll<UserMembership>(new { username }).FirstOrDefault();
            return View(new TwoFactorAuthenticator().GenerateSetupCode(AppConfig.Membership.AuthenticatorAppName, membership.Email, AppConfig.Membership.AuthenticatorKey, true, 2));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TwoFactorLogin(TwoFactorLogin model)
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

            if (!model.Validate(out var error, DbContext, AppConfig))
            {
                ViewBag.Error = error;
                return View(model);
            }

            model.Membership.DoLogOn(DbContext, HttpContext);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet, Authorize]
        public IActionResult Update()
        {
            return PartialView(DbContext.GetAll<User>(new { UserName = User.Identity.Name }).First());
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
