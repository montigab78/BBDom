using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog;
using NLog.Web;
using System;
using System.IO;
using System.Linq;

namespace BBDom.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = LogManager.Setup().LoadConfigurationFromAppSettings(basePath: GetContentRoot(args)).GetCurrentClassLogger();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var webHostArgs = args.Where(arg => arg != "--console").ToArray();
            var hostBuilder = Host.CreateDefaultBuilder(webHostArgs)
                .UseContentRoot(GetContentRoot(args))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseNLog();

            if (!args.Contains("--console"))
            {
                hostBuilder.UseWindowsService(options => { options.ServiceName = "BBDom.Web"; });
            }

            return hostBuilder;
        }

        private static string GetContentRoot(string[] args)
        {
            if (!args.Contains("--console") && WindowsServiceHelpers.IsWindowsService())
            {
                return AppContext.BaseDirectory;
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
