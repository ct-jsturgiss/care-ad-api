using CareAdApi;
using CareAdApi.Configuration;
using CareAdApi.Services;
using CareAdAsync.Controllers;
using Microsoft.Extensions.Logging.Configuration;
using Serilog;
using Serilog.Events;
using System.Text;

namespace CareAdAsync
{
    public class Program
    {
        private static LogEventLevel m_level = LogEventLevel.Information;
        private static LocalConfigFile m_config = null!;

        public static void Main(string[] args)
        {
            m_level = GetLogLevel(args);
            ConfigureLogging();
            PrintAppStart();

            if (!LoadConfig())
            {
                return;
            }

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog(Log.Logger, true);

            // Add services to the container.

            builder.Services.AddWindowsService(opt =>
            {
                opt.ServiceName = Constants.ServiceName;
            });

            builder.Services.AddControllers();

            // Add Services and Controllers
            builder.Services.AddSingleton<MainController>();
            builder.Services.AddSingleton<ActiveDirectoryService>();
            builder.Services.AddScoped<BasicAuthMiddleware>();

            // Add data
            builder.Services.AddSingleton(m_config);

            Log.Logger.Information("Building host...");
            var app = builder.Build();

            Log.Logger.Information("Initializing host...");

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseCors(opt =>
            {
                opt.WithMethods("POST");
                opt.WithOrigins("https://*.powerapps.com", "https://localhost:7078");
            });

            app.UseMiddleware<BasicAuthMiddleware>();

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

        private static LogEventLevel GetLogLevel(IEnumerable<string> args)
        {
            List<string> argsList = new List<string>(args);
            int idx = argsList.IndexOf(Constants.ArgLogLevel);
            if (idx == -1)
            {
                string? val = argsList.ElementAtOrDefault(idx + 1);
                if (!string.IsNullOrEmpty(val))
                {
                    return Enum.Parse<LogEventLevel>(val);
                }
            }

            return LogEventLevel.Information;
        }

        private static Serilog.ILogger ConfigureLogging()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            string logFile = Path.Combine(logDir, Constants.LogTemplateName);
            Directory.CreateDirectory(logDir);
            Serilog.ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Is(m_level)
                .WriteTo
                    .Console(m_level, outputTemplate: Constants.LoggingConstants.ConsoleFormat)
                .WriteTo
                    .File(logFile, m_level, rollingInterval: RollingInterval.Day, outputTemplate: Constants.LoggingConstants.FileFormat)
                .CreateLogger();
            Log.Logger = logger;

            return logger;
        }

        private static void PrintAppStart()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Constants.ServiceName);
            sb.AppendLine(Constants.Copyright);
            sb.AppendLine(string.Join(null, Enumerable.Repeat("=", 10)));
            Console.WriteLine(sb.ToString());

            Log.Logger.Information("--SERVICE START--");
            Log.Logger.Information("Log Level: '{ll}'", m_level);
        }
    }
}
