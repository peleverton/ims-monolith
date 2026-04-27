namespace IMS.Modular.Shared.FeatureFlags;

/// <summary>
/// US-074: Feature flag name constants.
/// Values must match the keys in appsettings.json under FeatureManagement.
/// </summary>
public static class FeatureFlags
{
    public const string EnableKanbanView      = nameof(EnableKanbanView);
    public const string EnableWebhooks        = nameof(EnableWebhooks);
    public const string EnableFullTextSearch  = nameof(EnableFullTextSearch);
}
