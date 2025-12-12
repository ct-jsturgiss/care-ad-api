using CareAdApi;
using CareAdApi.Configuration;
using CareAdApi.Services;
using CareAdAsync.Controllers;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Text;

namespace CareAdAsync
{
    public class Program
    {
        //private static LogEventLevel m_level = LogEventLevel.Information;
        private static LocalConfigFile m_config = null!;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureLogging(builder);
            PrintAppStart();

            if (!LoadConfig())
            {
                return;
            }

            builder.Host.UseSerilog(Log.Logger, true);
            builder.WebHost.UseKestrel(opt =>
            {
#if DEBUG
                opt.Listen(IPAddress.Loopback, Constants.LocalPort, lopt =>
                {
                    lopt.UseHttps();
                });
#endif
                opt.Listen(IPEndPoint.Parse(Constants.Endpoint), lopt =>
                {
                    lopt.UseHttps(m_config.Certificate, m_config.PlainCertificateKey);
                });
            });
            // Add services to the container.

            builder.Services.AddWindowsService(opt =>
            {
                opt.ServiceName = Constants.ServiceName;
            });
            builder.Services.AddHostedService<ApiBackgroundService>();

            builder.Services.AddControllers();

            // Add Services and Controllers
            builder.Services.AddSingleton<MainController>();
            builder.Services.AddSingleton<ActiveDirectoryService>();
            builder.Services.AddScoped<BasicAuthMiddleware>();

            // Add data
            builder.Services.AddSingleton(m_config);

            // Host filtering
            string allowedHosts = builder.Configuration.GetValue<string>("AllowedHosts") ?? string.Empty;
            builder.Services.AddHostFiltering(opt =>
            {
                opt.IncludeFailureMessage = false;
                opt.AllowEmptyHosts = false;
                opt.AllowedHosts = allowedHosts.Split(';');
            });

            // Add Cors
            builder.Services.AddCors(opt =>
            {
                opt.AddDefaultPolicy(p =>
                {
                    p.WithOrigins("https://powerapps.com")
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .WithMethods("POST")
                        .WithHeaders(Constants.Headers.HeaderKey);
                });
            });

            Log.Logger.Information("Building host...");
            var app = builder.Build();

            Log.Logger.Information("Initializing host...");

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseHostFiltering();

            app.UseCors(); // Before auth

            app.UseMiddleware<BasicAuthMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            Log.Logger.Information("Starting...");
            app.Run();

        }

        private static bool LoadConfig()
        {
            Log.Logger.Information("Loading config...");
            var file = LocalConfigFile.LoadFile();
            if(file == null)
            {
                LocalConfigFile.SaveFile(new LocalConfigFile());
                Log.Logger.Error("No local config file exists. A new one will be created but it must be configured.");
                return false;
            }
            if (string.IsNullOrEmpty(file.HeaderKey))
            {
                Log.Logger.Error("No header key specified. This must be specified to support basic auth.");
                return false;
            }
            m_config = file;

            return true;
        }

        private static Serilog.ILogger ConfigureLogging(IHostApplicationBuilder builder)
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string logFile = Path.Combine(logDir, Constants.LogTemplateName);
            Directory.CreateDirectory(logDir);
            LoggerConfiguration lc = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration);
            Serilog.ILogger logger = lc
                .WriteTo
                    .Console(outputTemplate: Constants.LoggingConstants.ConsoleFormat)
                .WriteTo
                    .File(logFile, rollingInterval: RollingInterval.Day, outputTemplate: Constants.LoggingConstants.FileFormat)
                .CreateLogger();
            Log.Logger = logger;

            return logger;
        }

        private static void PrintAppStart()
        {
            LogEventLevel level = Enum.GetValues<LogEventLevel>().Where(Log.IsEnabled).Min();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Constants.ServiceName);
            sb.AppendLine(Constants.Copyright);
            sb.AppendLine(string.Join(null, Enumerable.Repeat("=", 10)));
            Console.WriteLine(sb.ToString());

            Log.Logger.Information("--SERVICE START--");
            Log.Logger.Information("Log Level: '{ll}'", level);
        }
    }
}
