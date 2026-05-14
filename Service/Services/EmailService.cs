using Common.Configurations;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Service.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings = default!;
        public async Task SendBudgetAlert(string toEmail, string category, double spent, double limit)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SmartFinance", _emailSettings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "התראת חריגה מהתקציב ⚠️";

            // שימוש ב-BodyBuilder ליצירת תוכן HTML
            var bodyBuilder = new BodyBuilder();

            // עיצוב ה-HTML
            bodyBuilder.HtmlBody = $@"
    <div dir='rtl' style='font-family: Segoe UI, Tahoma, Geneva, Verdana, sans-serif; border: 1px solid #e0e0e0; border-radius: 10px; padding: 20px; max-width: 600px;'>
        <h2 style='color: #d9534f;'>חריגה בתקציב!</h2>
        <p>שלום,</p>
        <p>זיהינו חריגה מהתקציב שהגדרת עבור הקטגוריה <strong>{category}</strong>.</p>
        
        <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
            <thead>
                <tr style='background-color: #f8f9fa;'>
                    <th style='padding: 10px; border: 1px solid #dee2e6; text-align: right;'>פירוט</th>
                    <th style='padding: 10px; border: 1px solid #dee2e6; text-align: right;'>סכום</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>תקציב שהוגדר</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{limit:N2} ₪</td>
                </tr>
                <tr style='color: #d9534f; font-weight: bold;'>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>סך הוצאות בפועל</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{spent:N2} ₪</td>
                </tr>
                <tr style='background-color: #fff3f3;'>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>חריגה כוללת</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{(spent - limit):N2} ₪</td>
                </tr>
            </tbody>
        </table>

        <p style='margin-top: 30px; font-size: 0.9em; color: #6c757d;'>
            מומלץ להיכנס למערכת ולבחון את ההוצאות האחרונות שלך.<br>
            בברכה,<br>
            צוות SmartFinance
        </p>
    </div>";

            // גרסת טקסט פשוט (למקרה שהלקוח לא תומך ב-HTML)
            bodyBuilder.TextBody = $"חריגה בתקציב קטגוריית {category}. הוצאת {spent:N2} מתוך {limit:N2}.";

            message.Body = bodyBuilder.ToMessageBody();

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