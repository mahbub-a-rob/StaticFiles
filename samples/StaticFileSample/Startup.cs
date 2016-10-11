using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StaticFilesSample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true);

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDirectoryBrowser();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory factory, IHostingEnvironment host)
        {
            Console.WriteLine("webroot: " + host.WebRootPath);

            // Displays all log levels
            factory.AddConsole(LogLevel.Debug);

            // Just static files
            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = context => { },
                ContentTypeProvider = LoadFromConfig(new FileExtensionContentTypeProvider())
                    .SetFileType(".custom", "custom/type")
                    .RemoveFileType(".foo")
            });

            // Static files, default files, and directory browsing
            app.UseFileServer(new FileServerOptions()
            {
                EnableDirectoryBrowsing = true,
                ContentTypeProvider = new FileExtensionContentTypeProvider()
                    .SetFileType(".custom", "custom/type")
                    .RemoveFileType(".foo")
            });
        }

        private FileExtensionContentTypeProvider LoadFromConfig(FileExtensionContentTypeProvider contentTypes)
        {
            var clear = Configuration["staticfiles:extensions:clear"];
            if (string.Equals(clear, "true", StringComparison.OrdinalIgnoreCase))
            {
                contentTypes.Clear();
            }
            foreach (var pair in Configuration.GetSection("staticfiles:extensions:add")?.GetChildren())
            {
                // { ".foo": "app/bar" }
                contentTypes.SetFileType(pair.Key, pair.Value);
            }
            foreach (var pair in Configuration.GetSection("staticfiles:extensions:remove")?.GetChildren())
            {
                // { "0": ".foo" }
                contentTypes.RemoveFileType(pair.Value);
            }
            return contentTypes;
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
