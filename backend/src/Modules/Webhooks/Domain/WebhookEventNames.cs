namespace IMS.Modular.Modules.Webhooks.Domain;

/// <summary>
/// US-069: Nomes canônicos dos eventos suportados por webhooks.
/// </summary>
public static class WebhookEventNames
{
    public const string IssueCreated  = "issue.created";
    public const string IssueResolved = "issue.resolved";
    public const string StockLow      = "stock.low";
    public const string UserInvited   = "user.invited";

    public static readonly string[] All =
    [
        IssueCreated,
        IssueResolved,
        StockLow,
        UserInvited,
    ];
}
