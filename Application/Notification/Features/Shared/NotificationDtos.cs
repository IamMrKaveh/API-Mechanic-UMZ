namespace Application.Notification.Features.Shared;

public record NotificationDto(
    int Id,
    int UserId,
    string Title,
    string Message,
    string Type,
    string? ActionUrl,
    string? RelatedEntityType,
    int? RelatedEntityId,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);