using CareAdApi;
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
        private static LogEventLevel level = LogEventLevel.Information;

        public static void Main(string[] args)
        {
            level = GetLogLevel(args);
            ConfigureLogging();
            PrintAppStart();

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

            Log.Logger.Information("Building host...");
            var app = builder.Build();

            Log.Logger.Information("Initializing host...");

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            Log.Logger.Information("Starting...");
            app.Run();

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
                .MinimumLevel.Is(level)
                .WriteTo
                    .Console(level, outputTemplate: Constants.LoggingConstants.ConsoleFormat)
                .WriteTo
                    .File(logFile, level, rollingInterval: RollingInterval.Day, outputTemplate: Constants.LoggingConstants.FileFormat)
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
            Log.Logger.Information("Log Level: '{ll}'", level);
        }
    }
}
