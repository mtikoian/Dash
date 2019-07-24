using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Dash.Configuration;
using Dash.Models;
using Dash.Resources;
using Dash.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dash.Controllers
{
    [Pjax]
    public class AccountController : BaseController
    {
        void AddClaim(string claimType)
        {
            var identity = User.Identity as ClaimsIdentity;
            var claim = GetClaim(claimType);
            if (claim == null)
                identity.AddClaim(new Claim(claimType, "true"));
        }

        Claim GetClaim(string claimType) => (User.Identity as ClaimsIdentity).Claims.FirstOrDefault(x => x.Type == claimType);

        void RemoveClaim(string claimType)
        {
            var identity = User.Identity as ClaimsIdentity;
            var claim = GetClaim(claimType);
            if (claim != null)
                identity.RemoveClaim(claim);
        }

        public AccountController(IDbContext dbContext, IAppConfiguration appConfig) : base(dbContext, appConfig) { }

        [HttpGet]
        public IActionResult ForgotPassword() => User.Identity.IsAuthenticated ? Error(Core.ErrorAlreadyLoggedIn) : View("ForgotPassword", new ForgotPassword());

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult ForgotPassword(ForgotPassword model)
        {
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);
            if (!ModelState.IsValid)
                return View("ForgotPassword", model);

            if (!model.Send(out var error, DbContext, HttpContext, AppConfig, new UrlHelper(ControllerContext)))
            {
                ViewBag.Error = error;
                return View("ForgotPassword", model);
            }

            ViewBag.Error = Account.ForgotPasswordEmailSentText;
            return View("Login", new LogOn());
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);

            return View("Login", new LogOn { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult Login(LogOn model)
        {
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);
            if (!ModelState.IsValid)
                return View("Login", model);

            if (!model.DoLogOn(out var error, DbContext, AppConfig, HttpContext))
            {
                ViewBag.Error = error;
                return View("Login", model);
            }

            // add localization cookie after logon
            Response.Cookies.Append(Startup.CultureCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(new CultureInfo(model.Membership.LanguageCode))));
            if (model.Membership.AllowSingleFactor)
            {
                if (!model.ReturnUrl.IsEmpty() && !model.ReturnUrl.Contains("/Data/", System.StringComparison.CurrentCultureIgnoreCase))
                    return Redirect(model.ReturnUrl);
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

        [HttpGet, ValidModel]
        public IActionResult ResetPassword(ResetPassword model)
        {
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);
            if (!ModelState.IsValid)
                return View("Login", new LogOn());

            return View("ResetPassword", model);
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult ResetPassword(ResetPassword model, bool resetting = true)
        {
            // resetting param is only there to disambiguate the two ResetPassword routes
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);
            if (!ModelState.IsValid)
                return View("ResetPassword", model);

            if (!model.Reset(out var error))
            {
                ViewBag.Error = error;
                return View("ResetPassword", model);
            }

            ViewBag.Message = Account.PasswordChangedText;
            return View("Login", new LogOn());
        }

        [HttpGet, Authorize, ParentAction("UpdateAccount")]
        public IActionResult ToggleContextHelp()
        {
            var claim = GetClaim(CustomClaimTypes.ContextHelp);
            var wantsHelp = !(claim != null);
            if (wantsHelp)
                AddClaim(CustomClaimTypes.ContextHelp);
            else
                RemoveClaim(CustomClaimTypes.ContextHelp);
            return View("ToggleContextHelp", new Help(wantsHelp));
        }

        [HttpGet, Authorize, ParentAction("UpdateAccount")]
        public IActionResult ToggleProfiling()
        {
            var claim = GetClaim(CustomClaimTypes.Profiling);
            var wantsProfiling = !(claim != null);
            if (wantsProfiling)
                AddClaim(CustomClaimTypes.Profiling);
            else
                RemoveClaim(CustomClaimTypes.Profiling);
            return View("ToggleProfiling", new Profiling(wantsProfiling));
        }

        [HttpGet]
        public IActionResult TwoFactorHelp(string username)
        {
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);

            var membership = DbContext.GetAll<UserMembership>(new { username }).FirstOrDefault();
            if (membership == null)
                return Error(Core.ErrorGeneric);
            return View("TwoFactorHelp", TwoFactorAuthenticator.GenerateSetupCode(AppConfig.Membership.AuthenticatorAppName, membership.Email, AppConfig.Membership.AuthenticatorKey, true, 2));
        }

        [HttpPost, ValidateAntiForgeryToken, ValidModel]
        public IActionResult TwoFactorLogin([FromForm] TwoFactorLogin model)
        {
            if (User.Identity.IsAuthenticated)
                return Error(Core.ErrorAlreadyLoggedIn);
            if (!ModelState.IsValid)
                return View("TwoFactorLogin", model);

            if (!model.Validate(out var error, DbContext, AppConfig))
            {
                ViewBag.Error = error;
                return View("TwoFactorLogin", model.Membership);
            }

            model.Membership.DoLogOn(DbContext, HttpContext);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet, Authorize]
        public IActionResult UpdateAccount() => View("UpdateAccount", new UpdateAccount(DbContext, AppConfig, User.Identity.Name));

        [HttpPost, Authorize, ValidateAntiForgeryToken, ValidModel]
        public IActionResult UpdateAccount(UpdateAccount model)
        {
            if (!ModelState.IsValid)
                return View("UpdateAccount", model);

            if (!model.Save(User.Identity.Name, out var errorMsg))
            {
                ViewBag.Error = errorMsg;
                return View("UpdateAccount", model);
            }

            // add localization cookie after logon
            Response.Cookies.Append(Startup.CultureCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(new CultureInfo(model.LanguageCode))));
            ViewBag.Message = Account.AccountUpdated;
            return View("UpdateAccount", model);
        }

        [HttpGet, Authorize]
        public IActionResult UpdatePassword() => View("UpdatePassword", new UpdatePassword(DbContext, AppConfig));

        [HttpPost, Authorize, ValidateAntiForgeryToken, ValidModel]
        public IActionResult UpdatePassword(UpdatePassword model)
        {
            if (!ModelState.IsValid)
                return View("UpdatePassword", model);

            if (!model.Save(out var errorMsg))
            {
                ViewBag.Error = errorMsg;
                return View("UpdatePassword", model);
            }

            ViewBag.Message = Account.PasswordChangedText;
            return View("UpdatePassword", model);
        }
    }
}
