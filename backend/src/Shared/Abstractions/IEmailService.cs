namespace IMS.Modular.Shared.Abstractions;

/// <summary>
/// US-089: Abstraction for sending transactional emails.
/// Dev: log-based. Production: pluggable SMTP/SendGrid.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
