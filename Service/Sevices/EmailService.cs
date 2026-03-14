using Common.Configurations;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

public class EmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task SendBudgetAlert(string toEmail, string category, double spent, double limit)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("SmartFinance", _emailSettings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));

        message.Subject = "Budget Alert ⚠️";

        message.Body = new TextPart("plain")
        {
            Text = $"Budget alert!\nCategory: {category}\nSpent: {spent}\nLimit: {limit}"
        };

        using (var client = new SmtpClient())
        {
            client.Connect(_emailSettings.SmtpServer, _emailSettings.Port, false);
            client.Authenticate(_emailSettings.SenderEmail, _emailSettings.SenderPassword);

            client.Send(message);
            client.Disconnect(true);
        }
    }
}