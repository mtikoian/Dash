using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Http;
using Dash.Models;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Dash.Configuration;
using Dash.Models;
using Dash.Utils;

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
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => {
                options.InputFormatters.Insert(0, new JilInputFormatter());
                options.OutputFormatters.Insert(0, new JilOutputFormatter());
            });
            services.AddDataProtection();
            services.AddAntiforgery(options => options.HeaderName = "X-XSRF-Token");
            services.AddMemoryCache();
            services.AddSession(options => {
                options.Cookie.HttpOnly = true;
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(Configuration, appConfig);
            appConfig.IsDevelopment = env.IsDevelopment();
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
                            LogException(appConfig, error.Error, context);
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

                app.UseExceptionHandler("/Error/Index");
            }

            app.UseSession();
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Dashboard}/{action=Index}/{id?}");
            });
        }

        private void LogException(AppConfiguration config, Exception error, HttpContext context)
        {
            try
            {
                var dbContext = new DbContext(config);
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
