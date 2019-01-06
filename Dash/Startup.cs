using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AspNetCoreRateLimit;
using Dash.Configuration;
using Dash.Models;
using Dash.Utils;
using Hangfire;
using HardHat;
using HeyRed.Mime;
using Jil;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Dash
{
    public class Startup
    {
        private bool CanAccessMiniProfiler(HttpRequest request)
        {
            return request.HttpContext.User.HasAccess("Profiler", "Dashboard");
        }

        /// <summary>
        /// Scans the assembly for all controllers and updates the permissions table to match the list of available actions.
        /// </summary>
        /// <param name="dbContext"></param>
        private void UpdatePermissions(IDbContext dbContext)
        {
            // build a list of all available actions
            var actionList = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => typeof(Controller).IsAssignableFrom(x)) //filter controllers
                .SelectMany(x => x.GetMethods())
                .Where(x => x.IsPublic && !x.IsDefined(typeof(NonActionAttribute))
                     && (x.IsDefined(typeof(AuthorizeAttribute)) || (x.DeclaringType.IsDefined(typeof(AuthorizeAttribute)))) && !x.IsDefined(typeof(AllowAnonymousAttribute)) &!x.IsDefined(typeof(ParentActionAttribute)))
                .Select(x => $"{x.DeclaringType.FullName.Split('.').Last().Replace("Controller", "")}.{x.Name}")
                .Distinct()
                .ToDictionary(x => x.ToLower(), x => x);

            actionList.Add("hangfire.dashboard", "Hangfire.Dashboard");
            actionList.Add("profiler.dashboard", "Profiler.Dashboard");
            // query all permissions from db
            var permissions = dbContext.GetAll<Permission>().ToDictionary(x => x.FullName.ToLower(), x => x);

            // save any actions not in db
            actionList.Where(x => !permissions.ContainsKey(x.Key)).Each(x => {
                var parts = x.Value.Split('.');
                dbContext.Save(new Permission { ControllerName = parts[0], ActionName = parts[1] });
            });
            // delete any permission not in action list
            permissions.Where(x => !actionList.ContainsKey(x.Key)).Each(x => {
                dbContext.Delete(x.Value);
            });
        }

        private string MiniProfilerUser(HttpRequest request)
        {
            return request.HttpContext.User.Identity.Name;
        }

        public static string CultureCookieName = ".Dash.Culture";
        public static string AuthCookieName = ".Dash.Auth";
        public static string AntiforgeryCookieName = ".Dash.AntiForgery";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IDbContext dbContext)
        {
            // enable logging
            app.UseMiddleware<SerilogMiddleware>();

            // enable rate limiting
            app.UseCustomIpRateLimiting();

            // harden headers using HardHat - https://github.com/TerribleDev/HardHat
            // pretty locked down by default, will open up later if needed
            // Turn off dns prefetch to protect the privacy of users
            app.UseDnsPrefetch(allow: false);
            // Prevent clickjacking, by not allowing your site to be rendered in an iframe
            app.UseFrameGuard(new FrameGuardOptions(FrameGuardOptions.FrameGuard.SAMEORIGIN));
            // Tell browsers to always use https for the next 15 minutes
            app.UseHsts(maxAge: 900, includeSubDomains: true, preload: false);
            // Do not include the referrer header when linking away from your site to protect your users privacy
            app.UseReferrerPolicy(ReferrerPolicy.NoReferrer);
            // Don't allow old ie to open files in the context of your site
            app.UseIENoOpen();
            // Prevent MIME sniffing https://en.wikipedia.org/wiki/Content_sniffing
            app.UseNoMimeSniff();
            // Add headers to have the browsers auto detect and block some xss attacks
            app.UseCrossSiteScriptingFilters();
            // Provide a security policy so only content can come from trusted sources
            app.UseContentSecurityPolicy(new ContentSecurityPolicyBuilder()
                .WithDefaultSource(CSPConstants.Self)
                .WithImageSource("'self'", "data:") // allow images from self, including base64 encoding images aka icon fonts
                .WithStyleSource("'self'", "'unsafe-inline'") // allow styles from self and inline from js
                .WithFontSource(CSPConstants.Self)
                .WithFrameAncestors(CSPConstants.None)
                .WithScriptSource("'self'", "'unsafe-eval'")
                .BuildPolicy()
            );

            app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

            // force all requests to https
            app.UseRewriter(new RewriteOptions().AddRedirectToHttps());

            app.UseExceptionHandler(builder => {
                builder.Run(async context => {
                    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                    if (error != null)
                    {
                        try
                        {
                            Log.Error(error.Error, error.Error.Message);
                        }
                        catch { }
                    }

                    if (context.Request.ContentType?.ToLower()?.Contains("json") == true)
                    {
                        context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                        context.Response.ContentType = "application/json";

                        using (var writer = new StreamWriter(context.Response.Body))
                        {
                            writer.Write(JSON.SerializeDynamic(new { Error = "An unexpected error occurred." }, JilOutputFormatter.Options));
                            await writer.FlushAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        context.Response.Redirect("/Error/Index");
                    }
                });
            });

            app.UseSession();
            app.UseStaticFiles(new StaticFileOptions {
                OnPrepareResponse = content => {
                    if (content.File.Name.EndsWith(".gz"))
                    {
                        content.Context.Response.Headers["Content-Type"] = MimeTypesMap.GetMimeType(content.File.Name.Replace(".gz", ""));
                        content.Context.Response.Headers["Content-Encoding"] = "gzip";
                    }
                }
            });
            app.UseAuthentication();

            var cultures = new List<CultureInfo> {
                new CultureInfo("en"),
                new CultureInfo("es")
            };
            var localizationOptions = new RequestLocalizationOptions {
                DefaultRequestCulture = new RequestCulture("en"),
                SupportedCultures = cultures,
                SupportedUICultures = cultures,
            };
            localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider() { CookieName = CultureCookieName });
            app.UseRequestLocalization(localizationOptions);

            // Configure hangfire to use the new JobActivator we defined.
            app.UseHangfireDashboard("/hangfire", new DashboardOptions() {
                Authorization = new[] { new HangfireAuthorizeFilter() }
            });
            app.UseHangfireServer();

            app.UseMiniProfiler();

            app.UseMvc(routes => {
                routes.MapRoute("parentChild", "{controller}/{action}/{parentId:int}/{id:int}");
                routes.MapRoute("default", "{controller=Dashboard}/{action=Index}/{id:int?}");
            });

            // use reflection to check for added/removed permissions and update db accordingly
            UpdatePermissions(dbContext);

            // start hangfire background job
            RecurringJob.AddOrUpdate<JobHelper>(x => x.ProcessAlerts(), Cron.Minutely);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appConfig = new AppConfiguration();
            Configuration.Bind("App", appConfig);
            services.AddSingleton((IAppConfiguration)appConfig);

            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddAuthentication(x => {
                x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(x => {
                x.Cookie.HttpOnly = true;
                x.Cookie.Name = AuthCookieName;
                x.SessionStore = new MemoryCacheTicketStore();
            });

            services.AddHangfire(x => x.UseSqlServerStorage(appConfig.Database.ConnectionString));

            services.AddMvc(options => {
                options.OutputFormatters.Insert(0, new JilOutputFormatter());
                options.Filters.Add(new RequireHttpsAttribute());
            });

            services.AddAuthorization(options => {
                options.AddPolicy("HasPermission", policy => policy.Requirements.Add(new PermissionRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

            services.AddDataProtection();
            services.AddAntiforgery(options => {
                options.HeaderName = "X-XSRF-TOKEN";
                options.Cookie.Name = AntiforgeryCookieName;
            });

            services.AddSingleton(new MemoryCache(new MemoryCacheOptions()));

            services.AddSession(options => {
                options.Cookie.HttpOnly = true;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IDbContext, DbContext>();

            services.AddLocalization(x => {
                x.ResourcesPath = "Resources";
            });

            // https://stackoverflow.com/questions/39276939/how-to-inject-dependencies-into-models-in-asp-net-core
            services.AddMvc().AddMvcOptions(options => {
                // replace ComplexTypeModelBinderProvider with its descendent - IoCModelBinderProvider
                var provider = options.ModelBinderProviders.FirstOrDefault(x => x.GetType() == typeof(ComplexTypeModelBinderProvider));
                var binderIndex = options.ModelBinderProviders.IndexOf(provider);
                options.ModelBinderProviders.Remove(provider);
                options.ModelBinderProviders.Insert(binderIndex, new DiModelBinderProvider());
            }).AddDataAnnotationsLocalization();

            services.AddMiniProfiler(options => {
                options.RouteBasePath = "/dash.profiler";
                options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.SqlServerFormatter();
                options.PopupMaxTracesToShow = 10;
                options.ResultsAuthorize = request => CanAccessMiniProfiler(request);
                options.ResultsListAuthorize = request => CanAccessMiniProfiler(request);
                options.UserIdProvider = request => MiniProfilerUser(request);
            });
        }
    }
}
