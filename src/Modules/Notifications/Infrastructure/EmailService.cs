using IMS.Modular.Modules.Notifications.Application;
using IMS.Modular.Modules.Notifications.Domain;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace IMS.Modular.Modules.Notifications.Infrastructure;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string FromAddress { get; set; } = "noreply@ims.local";
    public string FromName { get; set; } = "IMS System";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class EmailService(EmailSettings settings, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var client = BuildClient();
            using var mail = BuildMailMessage(message);
            await client.SendMailAsync(mail, ct);
            logger.LogInformation("Email sent to {To} — Subject: {Subject}", message.To, message.Subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} — Subject: {Subject}", message.To, message.Subject);
            // Do not rethrow — email is best-effort; caller should not fail
        }
    }

    public async Task SendBulkAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default)
    {
        foreach (var message in messages)
            await SendAsync(message, ct);
    }

    private SmtpClient BuildClient()
    {
        var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);

        return client;
    }

    private MailMessage BuildMailMessage(EmailMessage message)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml
        };

        mail.To.Add(message.To);

        if (message.Cc is not null)
            foreach (var cc in message.Cc)
                mail.CC.Add(cc);

        if (message.Bcc is not null)
            foreach (var bcc in message.Bcc)
                mail.Bcc.Add(bcc);

        return mail;
    }
}
