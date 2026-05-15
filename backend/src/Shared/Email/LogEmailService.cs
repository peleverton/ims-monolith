using IMS.Modular.Shared.Abstractions;

namespace IMS.Modular.Shared.Email;

/// <summary>
/// US-089: Development/test email service — logs email content instead of sending.
/// Replace with SmtpEmailService or SendGridEmailService in production.
/// </summary>
public sealed class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Email] To={To} | Subject={Subject} | Body={Body}",
            to, subject, htmlBody);

        return Task.CompletedTask;
    }
}
