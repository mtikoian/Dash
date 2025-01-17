﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Dash
{
    public static class Program
    {
        public static void Main(string[] args) => WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) => {
                var env = hostingContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(config.Build())
                    .Enrich.FromLogContext()
                    .CreateLogger();
            })
            .UseStartup<Startup>()
            .UseSerilog()
            .Build()
            .Run();
    }
}
