using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.Interfaces;

namespace Service.Services
{
    /// <summary>
    /// BackgroundService שמפעיל את SyncService פעם ביום בשעה שנקבעת ב-appsettings.
    /// אין צורך ב-Hangfire - זה built-in ל-.NET.
    /// </summary>
    public class DailySyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailySyncBackgroundService> _logger;
        private readonly TimeOnly _runAt; // שעת הריצה היומית

        public DailySyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<DailySyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            // ← שנה לשעה שתרצי, למשל 08:00 בבוקר
            _runAt = new TimeOnly(8, 0);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[DailySync] Background service started. Will run daily at {Time}", _runAt);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                var delay = CalculateDelay(now);

                _logger.LogInformation("[DailySync] Next run in {Minutes} minutes", delay.TotalMinutes);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break; // האפליקציה נסגרת
                }

                await RunSyncAsync(stoppingToken);
            }
        }

        private async Task RunSyncAsync(CancellationToken ct)
        {
            _logger.LogInformation("[DailySync] Running daily sync...");
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                await syncService.RunDailySyncAsync();
                _logger.LogInformation("[DailySync] Completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DailySync] Error during sync.");
            }
        }

        private TimeSpan CalculateDelay(TimeOnly now)
        {
            var nextRun = _runAt > now
                ? _runAt - now                            // היום עדיין לא רץ
                : TimeSpan.FromHours(24) - (now - _runAt); // מחכה למחר

            return nextRun;
        }
    }
}