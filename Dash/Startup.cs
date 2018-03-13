using Dash.Configuration;
using Dash.Models;
using Dash.Utils;
using HardHat;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IDbContext dbContext)
        {
            loggerFactory.AddSerilog();

            // harden headers using HardHat - https://github.com/TerribleDev/HardHat
            // pretty locked down by default, will open up later if needed
            // Turn off dns prefetch to protect the privacy of users
            app.UseDnsPrefetch(allow: false);
            // Prevent clickjacking, by not allowing your site to be rendered in an iframe
            app.UseFrameGuard(new FrameGuardOptions(FrameGuardOptions.FrameGuard.SAMEORIGIN));
            // Tell browsers to always use https for the next 5000 seconds
            app.UseHsts(maxAge: 5000, includeSubDomains: true, preload: false);
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
                .WithFontSource(CSPConstants.Self)
                .WithFrameAncestors(CSPConstants.None)
                .BuildPolicy()
            );

            // force all requests to https
            app.UseRewriter(new RewriteOptions().AddRedirectToHttps());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            try
                            {
                                loggerFactory.CreateLogger("Application").LogError(error.Error, "Application Error");
                            }
                            catch { }
                        }

                        if (context.Request.ContentType.ToLower().Contains("json"))
                        {
                            context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{ message: 'An unexpected error occurred.' }").ConfigureAwait(false);
                        }
                        else
                        {
                            context.Response.Redirect("/Error/Index");
                        }
                    });
                });
            }

            app.UseMiddleware<SerilogMiddleware>();

            app.UseSession();
            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseRequestLocalization();
            app.UseMvc(routes => {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Dashboard}/{action=Index}/{id?}");
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddConfiguration();
            var appConfig = new AppConfiguration();
            Configuration.Bind("App", appConfig);
            services.AddSingleton(appConfig);

            //services.AddSingleton<IAppConfiguration>((IAppConfiguration)Configuration.GetSection("AppConfig"));
            services.AddAuthentication(x => {
                x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddCookie(x => {
                x.Cookie.HttpOnly = true;
                x.SessionStore = new MemoryCacheTicketStore();
            });
            services.AddMvc(options => {
                options.InputFormatters.Insert(0, new JilInputFormatter());
                options.OutputFormatters.Insert(0, new JilOutputFormatter());
                options.Filters.Add(new RequireHttpsAttribute());
            });

            services.AddAuthorization(options => {
                options.AddPolicy("HasPermission", policy => policy.Requirements.Add(new PermissionRequirement()));
            });
            services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

            services.AddDataProtection();
            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-Token");
            var cache = new MemoryCache(new MemoryCacheOptions());
            services.AddSingleton(cache);
            services.AddSession(options => {
                options.Cookie.HttpOnly = true;
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IDbContext, DbContext>();
        }
    }
}