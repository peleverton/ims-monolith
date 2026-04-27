using IMS.Modular.Modules.Issues.Application.DTOs;
using IMS.Modular.Modules.Issues.Domain.Entities;

namespace IMS.Modular.Modules.Issues.Application.Mappings;

public static class IssueMapper
{
    public static IssueDto ToDto(Issue issue) => new(
        issue.Id,
        issue.Title,
        issue.Description,
        issue.Status.ToString(),
        issue.Priority.ToString(),
        issue.AssigneeId,
        issue.ReporterId,
        issue.CreatedAt,
        issue.UpdatedAt,
        issue.DueDate,
        issue.ResolvedAt,
        issue.Comments.Select(c => new IssueCommentDto(c.Id, c.Content, c.AuthorId, c.CreatedAt)).ToList(),
        issue.Activities.Select(a => new IssueActivityDto(a.Id, a.ActivityType.ToString(), a.Description, a.UserId, a.Timestamp)).ToList(),
        issue.Tags.Select(t => new IssueTagDto(t.Name, t.Color)).ToList());
}
