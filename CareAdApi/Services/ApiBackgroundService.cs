
using Serilog;
using ILogger = Serilog.ILogger;

namespace CareAdApi.Services
{
    public class ApiBackgroundService : BackgroundService
    {
        private ILogger m_logger = null!;

        public ApiBackgroundService(ILogger logger)
        {
            m_logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            m_logger.Information("API Service starting...");

            stoppingToken.Register(() => m_logger.Information("API Service is stopping..."));

            while (!stoppingToken.IsCancellationRequested)
            {
                m_logger.Verbose("API service is running...");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            m_logger.Information("API Service has stopped");
        }
    }
}
