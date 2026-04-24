namespace IMS.Modular.Modules.Notifications.Application.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    DateTime SentAt,
    bool IsRead,
    DateTime? ReadAt);

public record NotificationPagedDto(
    List<NotificationDto> Items,
    int TotalUnread,
    int PageNumber,
    int PageSize);
