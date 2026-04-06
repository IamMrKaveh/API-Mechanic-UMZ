namespace Application.Notification.Features.Shared;

public record NotificationDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}