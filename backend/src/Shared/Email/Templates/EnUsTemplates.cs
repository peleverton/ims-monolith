namespace IMS.Modular.Shared.Email.Templates;

/// <summary>
/// US-085: EN-US email template strings.
/// </summary>
internal static class EnUsTemplates
{
    public static readonly Dictionary<string, (string Subject, string Body)> Templates = new()
    {
        [nameof(EmailTemplateType.IssueCreated)] = (
            Subject: "New Issue Created: {Title}",
            Body: """
                <html><body>
                <h2>New Issue Created</h2>
                <p><strong>Title:</strong> {Title}</p>
                <p><strong>Priority:</strong> {Priority}</p>
                <p><strong>Description:</strong> {Description}</p>
                <p>Log in to the system for more details.</p>
                </body></html>
                """),
        [nameof(EmailTemplateType.LowStock)] = (
            Subject: "Low Stock Alert: {ProductName}",
            Body: """
                <html><body>
                <h2>Low Stock Alert</h2>
                <p><strong>Product:</strong> {ProductName}</p>
                <p><strong>SKU:</strong> {Sku}</p>
                <p><strong>Current stock:</strong> {CurrentStock}</p>
                <p><strong>Minimum stock:</strong> {MinimumStock}</p>
                <p>Please arrange for stock replenishment.</p>
                </body></html>
                """),
        [nameof(EmailTemplateType.Welcome)] = (
            Subject: "Welcome to IMS, {FullName}!",
            Body: """
                <html><body>
                <h2>Welcome to the Issue Management System (IMS)!</h2>
                <p>Hello, <strong>{FullName}</strong>!</p>
                <p>Your account has been created successfully.</p>
                <p><strong>Username:</strong> {Username}</p>
                <p>Log in to start using the system.</p>
                </body></html>
                """)
    };
}
