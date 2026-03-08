using IMS.Modular.Modules.Issues.Domain.Enums;

namespace IMS.Modular.Modules.Issues.Domain.ValueObjects;

public class IssueComment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid IssueId { get; private set; }
    public string Content { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private IssueComment() { }

    public IssueComment(Guid issueId, string content, Guid authorId)
    {
        IssueId = issueId;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        AuthorId = authorId;
    }

    public void UpdateContent(string content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        UpdatedAt = DateTime.UtcNow;
    }
}

public class IssueActivity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid IssueId { get; private set; }
    public IssueActivityType ActivityType { get; private set; }
    public Guid UserId { get; private set; }
    public string Description { get; private set; } = null!;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private IssueActivity() { }

    public IssueActivity(Guid issueId, IssueActivityType activityType, Guid userId, string description)
    {
        IssueId = issueId;
        ActivityType = activityType;
        UserId = userId;
        Description = description;
    }
}

public class IssueTag
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;

    private IssueTag() { }

    public IssueTag(string name, string color)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Color = color ?? throw new ArgumentNullException(nameof(color));
    }
}
