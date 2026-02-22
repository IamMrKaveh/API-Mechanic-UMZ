namespace Application.Notification.Features.Shared;

public record NotificationDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string? RelatedEntityType { get; init; }
    public int? RelatedEntityId { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}