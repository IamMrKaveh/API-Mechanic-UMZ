namespace Application.Support.Features.Shared;

public record TicketDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
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
    public int Id { get; init; }
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
    public int Id { get; init; }
    public int SenderId { get; init; }
    public string SenderName { get; init; } = string.Empty;
    public bool IsAdminReply { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}