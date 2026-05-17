namespace IMS.Modular.Shared.Email;

/// <summary>
/// US-085: Template keys and model for localized email generation.
/// </summary>
public enum EmailTemplateType
{
    IssueCreated,
    LowStock,
    Welcome
}

public record EmailTemplateContext(
    EmailTemplateType Template,
    string? Locale = null,
    Dictionary<string, string>? Variables = null);

/// <summary>
/// US-085: Generates localized email subject + HTML body from templates.
/// Supports PT-BR (default fallback) and EN-US.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>Returns (subject, htmlBody) for the given template and locale.</summary>
    (string Subject, string HtmlBody) Render(EmailTemplateContext context);
}
