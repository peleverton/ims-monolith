using IMS.Modular.Shared.Email.Templates;

namespace IMS.Modular.Shared.Email;

/// <summary>
/// US-085: Renders localized email templates for PT-BR and EN-US.
/// Locale detection order: explicit context.Locale → Accept-Language header → PT-BR fallback.
/// </summary>
public sealed class LocalizedEmailTemplateService : IEmailTemplateService
{
    private const string PtBr = "pt-BR";
    private const string EnUs = "en-US";

    public (string Subject, string HtmlBody) Render(EmailTemplateContext context)
    {
        var locale = NormalizeLocale(context.Locale);
        var key = context.Template.ToString();

        var (subjectTemplate, bodyTemplate) = GetTemplate(key, locale);

        var subject = ApplyVariables(subjectTemplate, context.Variables);
        var body = ApplyVariables(bodyTemplate, context.Variables);

        return (subject, body);
    }

    private static (string Subject, string Body) GetTemplate(string key, string locale)
    {
        var templates = locale == EnUs ? EnUsTemplates.Templates : PtBrTemplates.Templates;

        if (templates.TryGetValue(key, out var tpl))
            return tpl;

        // Fallback to PT-BR
        return PtBrTemplates.Templates.TryGetValue(key, out var ptTpl)
            ? ptTpl
            : ("[IMS]", "<p>Notification from IMS.</p>");
    }

    private static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return PtBr;

        return locale.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? EnUs : PtBr;
    }

    private static string ApplyVariables(string template, Dictionary<string, string>? variables)
    {
        if (variables is null || variables.Count == 0)
            return template;

        foreach (var (key, value) in variables)
            template = template.Replace($"{{{key}}}", value, StringComparison.OrdinalIgnoreCase);

        return template;
    }
}
