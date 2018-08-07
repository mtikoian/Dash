using System.Linq;
using Dash.Configuration;
using Dash.I18n;
using Dash.Models;
using Dash.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dash.Controllers
{
    [Pjax]
    public class AccountController : BaseController
    {
        public AccountController(IDbContext dbContext, AppConfiguration appConfig) : base(dbContext, appConfig)
        {
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return Error(Core.ErrorAlreadyLoggedIn);
            }
            return View(new ForgotPassword());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ForgotPassword([FromForm] ForgotPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Error(Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View(model);
            }
            if (!model.Send(out var error, DbContext, HttpContext, AppConfig, new UrlHelper(ControllerContext)))
            {
                ViewBag.Error = error;
                return View(model);
            }
            ViewBag.Error = Account.ForgotPasswordEmailSentText;
            return View("Login", new LogOn());
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            ViewBag.Title = Account.Login;
            return View(new LogOn { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(LogOn model)
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.Error = Core.ErrorAlreadyLoggedIn;
                return View("Login", model);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Login", model);
            }
            if (!model.DoLogOn(out var error, DbContext, AppConfig, HttpContext))
            {
                ViewBag.Error = error;
                return View("Login", model);
            }
            if (model.Membership.AllowSingleFactor)
            {
                if (!model.ReturnUrl.IsEmpty())
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }
            model.Membership.CreateHash();
            return View("TwoFactorLogin", model.Membership);
        }

        [HttpGet]
        public IActionResult LogOff()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword([FromQuery] ResetPassword model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Error(Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Login", new LogOn());
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ResetPassword([FromForm] ResetPassword model, bool resetting = false)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Error(Core.ErrorAlreadyLoggedIn);
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

        [HttpGet, Authorize]
        public IActionResult ToggleContextHelp()
        {
            HttpContext.Session.SetString("ContextHelp", (!HttpContext.Session.GetString("ContextHelp").ToBool()).ToString());
            return View("ToggleContextHelp", new Help(HttpContext.Session));
        }

        [HttpGet]
        public IActionResult TwoFactorHelp(string username)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Error(Core.ErrorAlreadyLoggedIn);
            }
            var membership = DbContext.GetAll<UserMembership>(new { username }).FirstOrDefault();
            if (membership == null)
            {
                return Error(Core.ErrorGeneric);
            }
            return View(new TwoFactorAuthenticator().GenerateSetupCode(AppConfig.Membership.AuthenticatorAppName, membership.Email, AppConfig.Membership.AuthenticatorKey, true, 2));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TwoFactorLogin([FromForm] TwoFactorLogin model)
        {
            if (User.Identity.IsAuthenticated)
            {
                return Error(Core.ErrorAlreadyLoggedIn);
            }
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View(model);
            }

            if (!model.Validate(out var error, DbContext, AppConfig))
            {
                ViewBag.Error = error;
                return View(model.Membership);
            }

            model.Membership.DoLogOn(DbContext, HttpContext);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet, Authorize]
        public IActionResult Update()
        {
            ViewBag.Title = Account.UpdateAccount;
            return PartialView(DbContext.GetAll<User>(new { UserName = User.Identity.Name }).First());
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public IActionResult Update(User model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = ModelState.ToErrorString();
                return View("Update", model);
            }
            if (!model.UpdateProfile(out var errorMsg))
            {
                ViewBag.Error = errorMsg;
                return View("Update", model);
            }
            ViewBag.Message = Account.AccountUpdated;
            return View("Update", model);
        }
    }
}
