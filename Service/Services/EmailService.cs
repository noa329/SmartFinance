using Common.Configurations;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Service.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings?.Value ?? throw new ArgumentNullException(nameof(emailSettings));
        }

        public async Task SendBudgetAlert(string toEmail, string category, double spent, double limit)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("SmartFinance", _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));

            message.Subject = "Budget Alert ⚠️";

            message.Body = new TextPart("plain")
            {
                Text = $@"שלום,
זוהי התראת תקציב ממערכת SmartFinance.

חצית את גבול ההוצאות בקטגוריה: {category}
סכום שהוצא: {spent:N2}₪
תקציב מקורי: {limit:N2}₪

נא לשים לב להוצאות הבאות שלך."
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);

                    await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);

                    await client.SendAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending email: {ex.Message}");
                    throw; 
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}