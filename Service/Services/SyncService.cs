using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Services
{
    /// <summary>
    /// מופעל פעם ביום (Hangfire/BackgroundService).
    /// לכל משתמש: שולף חשבוניות + שולח מייל מרוכז עם כל ההתראות.
    /// </summary>
    public class SyncService : ISyncService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Alert> _alertRepository;
        private readonly GmailInvoiceService _gmailService;
        private readonly EmailService _emailService;

        public SyncService(
            IRepository<User> userRepository,
            IRepository<Alert> alertRepository,
            GmailInvoiceService gmailService,
            EmailService emailService)
        {
            _userRepository = userRepository;
            _alertRepository = alertRepository;
            _gmailService = gmailService;
            _emailService = emailService;
        }

        public async Task RunDailySyncAsync()
        {
            Console.WriteLine($"[Sync] Daily sync started at {DateTime.Now:HH:mm:ss}");
            var users = await _userRepository.GetAll();

            foreach (var user in users)
            {
                try { await ProcessUserAsync(user); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Sync] Error for user {user.Id}: {ex.Message}");
                }
            }

            Console.WriteLine("[Sync] Daily sync completed.");
        }

        private async Task ProcessUserAsync(User user)
        {
            // שלב 1: שליפת חשבוניות חדשות מ-Gmail
            if (!string.IsNullOrEmpty(user.GmailToken))
            {
                int newCount = await _gmailService.ProcessUserInvoicesAsync(user.GmailToken, user.Id);
                Console.WriteLine($"[Sync] User {user.Id}: {newCount} new invoices.");
            }

            // שלב 2: שליחת מייל מרוכז עם כל ההתראות שנצברו
            if (!user.IsAlertEnabled) return;

            var unsent = await _alertRepository.Find(a => a.UserId == user.Id && !a.IsSent);
            if (unsent == null || !unsent.Any()) return;

            await _emailService.SendDailySummary(user.Email, unsent);

            foreach (var alert in unsent)
            {
                alert.IsSent = true;
                await _alertRepository.Update(alert.Id, alert);
            }

            Console.WriteLine($"[Sync] User {user.Id}: Sent {unsent.Count()} alerts.");
        }
    }
}