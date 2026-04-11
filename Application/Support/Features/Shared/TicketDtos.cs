namespace Application.Support.Features.Shared;

public record TicketDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public List<TicketMessageDto> Messages { get; init; } = [];
}

public record TicketListItemDto
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MessageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastReplyAt { get; init; }
}

public record TicketMessageDto
{
    public Guid Id { get; init; }
    public Guid SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public bool IsAdminReply { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record CreateTicketDto(
    string Subject,
    string Category,
    string Message,
    string Priority
);

public sealed record ReplyToTicketDto(
    string Message
);

public sealed record CloseTicketDto(
    bool IsAdmin = false
);