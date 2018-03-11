using Dash.Configuration;
using Dash.Models;
using HardHat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Serilog;
using Serilog.Events;
using Serilog.Configuration;

namespace Dash
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }
        public IAppConfiguration AppConfig { get; set; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
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

            AppConfig = new AppConfiguration();
            ConfigurationBinder.Bind(Configuration, AppConfig);
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
                            LogException(error.Error, context);
                        }

                        if (context.Request.ContentType.ToLower().Contains("json"))
                        {
                            context.Response.StatusCode = (int)System.Net.HttpStatusCode.OK;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{ message: 'An unexpected error occurred.' }").ConfigureAwait(false);
                        }
                        else
                        {
                            context.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                            context.Response.ContentType = "text/html";
                            await context.Response.WriteAsync("<h2>An error has occured in the website.</h2>").ConfigureAwait(false);
                        }
                    });
                });
                // app.UseExceptionHandler("/Error/Index");
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
            //services.AddSingleton<IAppConfiguration>((IAppConfiguration)Configuration.GetSection("AppConfig"));
            //services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
            services.AddMvc(options => {
                options.InputFormatters.Insert(0, new JilInputFormatter());
                options.OutputFormatters.Insert(0, new JilOutputFormatter());
                options.Filters.Add(new RequireHttpsAttribute());
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("HasPermission", policy => policy.Requirements.Add(new PermissionRequirement()));
            });
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

        private void LogException(Exception error, HttpContext context)
        {
            try
            {
                var dbContext = new DbContext(AppConfig);
                dbContext.Save(new ErrorLog {
                    Namespace = this.GetType().Namespace,
                    Host = Environment.MachineName,
                    Type = error.GetType().FullName,
                    Source = error.Source,
                    Path = context.Request.Path.Value,
                    Method = context.Request.Method,
                    Message = error.Message,
                    StackTrace = error.StackTrace,
                    Timestamp = DateTimeOffset.Now,
                    User = context.User.Identity?.Name
                });
            }
            catch { }
        }
    }
}