namespace Application.Support.Features.Shared;

public sealed record TicketDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid? AssignedAgentId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string PriorityDisplayName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusDisplayName { get; init; } = string.Empty;
    public string? UserFullName { get; init; }
    public int MessageCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public List<TicketMessageDto> Messages { get; init; } = [];
}

public sealed record TicketListItemDto
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

public sealed record TicketMessageDto
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public Guid SenderId { get; init; }
    public string SenderType { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsAdminReply { get; init; }
    public bool IsEdited { get; init; }
    public string? SenderName { get; init; }
    public DateTime? EditedAt { get; init; }
    public DateTime? SentAt { get; init; }
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