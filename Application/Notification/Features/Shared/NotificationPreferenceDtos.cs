namespace Application.Notification.Features.Shared;

public sealed record NotificationTypeOptionDto
{
    public string Value { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
}

public sealed record NotificationPreferenceDto
{
    public IReadOnlyList<NotificationTypeOptionDto> Types { get; init; } = [];
}
