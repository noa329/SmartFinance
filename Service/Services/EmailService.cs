using Common.Configurations;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Repository.Entities;
using Service.Interfaces;

namespace Service.Services
{
    public class EmailService: IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _emailSettings = options.Value;
        }

        public async Task SendEmail(string toEmail, string subject, string body)
        {
            var message = BuildMessage(toEmail, subject);
            message.Body = new BodyBuilder { TextBody = body }.ToMessageBody();
            await SendAsync(message);
        }

        public async Task SendDailySummary(string toEmail, IEnumerable<Alert> alerts)
        {
            var alertList = alerts.ToList();
            if (!alertList.Any()) return;

            var message = BuildMessage(toEmail, "📊 סיכום התראות יומי – SmartFinance");
            var rows = string.Join("\n", alertList.Select(a =>
                $"<tr><td style='padding:10px;border:1px solid #dee2e6;'>{a.Title}</td><td style='padding:10px;border:1px solid #dee2e6;'>{a.Message}</td></tr>"));

            message.Body = new BodyBuilder
            {
                HtmlBody = $@"
<div dir='rtl' style='font-family:Segoe UI,Tahoma,sans-serif;max-width:600px;border:1px solid #e0e0e0;border-radius:10px;padding:24px;'>
  <h2 style='color:#2563eb;'>📊 סיכום התראות יומי</h2>
  <p>שלום, להלן ההתראות שנצברו היום:</p>
  <table style='width:100%;border-collapse:collapse;'>
    <thead><tr style='background:#f1f5f9;'>
      <th style='padding:10px;border:1px solid #dee2e6;text-align:right;'>כותרת</th>
      <th style='padding:10px;border:1px solid #dee2e6;text-align:right;'>פרטים</th>
    </tr></thead>
    <tbody>{rows}</tbody>
  </table>
  <p style='font-size:0.85em;color:#6c757d;margin-top:20px;'>בברכה, צוות SmartFinance</p>
</div>",
                TextBody = string.Join("\n", alertList.Select(a => $"- {a.Title}: {a.Message}"))
            }.ToMessageBody();

            await SendAsync(message);
        }

        public async Task SendBudgetAlert(string toEmail, string category, double spent, double limit)
        {
            var message = BuildMessage(toEmail, "⚠️ התראת חריגה מהתקציב – SmartFinance");
            message.Body = new BodyBuilder
            {
                HtmlBody = $@"
<div dir='rtl' style='font-family:Segoe UI,Tahoma,sans-serif;border:1px solid #e0e0e0;border-radius:10px;padding:20px;max-width:600px;'>
  <h2 style='color:#d9534f;'>⚠️ חריגה בתקציב!</h2>
  <p>חריגה בקטגוריה <strong>{category}</strong>:</p>
  <table style='width:100%;border-collapse:collapse;'>
    <tr><td style='padding:10px;border:1px solid #dee2e6;'>תקציב</td><td style='padding:10px;border:1px solid #dee2e6;'>{limit:N2} ₪</td></tr>
    <tr style='color:#d9534f;font-weight:bold;'><td style='padding:10px;border:1px solid #dee2e6;'>הוצאות</td><td style='padding:10px;border:1px solid #dee2e6;'>{spent:N2} ₪</td></tr>
    <tr style='background:#fff3f3;'><td style='padding:10px;border:1px solid #dee2e6;'>חריגה</td><td style='padding:10px;border:1px solid #dee2e6;'>{(spent - limit):N2} ₪</td></tr>
  </table>
  <p style='font-size:0.85em;color:#6c757d;'>בברכה, צוות SmartFinance</p>
</div>",
                TextBody = $"חריגה בקטגוריה {category}: {spent:N2} מתוך {limit:N2}."
            }.ToMessageBody();

            await SendAsync(message);
        }

        private MimeMessage BuildMessage(string toEmail, string subject)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SmartFinance", _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            return message;
        }

        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                await client.SendAsync(message);
                Console.WriteLine($"[Email] ✅ Sent: {message.Subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email] ❌ Error: {ex.Message}");
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}