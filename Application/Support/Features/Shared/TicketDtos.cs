namespace Application.Support.Features.Shared;

public record TicketDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public record TicketDetailDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public bool IsAdminResponse { get; init; }
    public List<TicketMessageDto> Messages { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public record TicketMessageDto
{
    public int Id { get; init; }
    public int TicketId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsAdminResponse { get; init; }
}

public record CreateTicketDto(
    string Subject,
    string Priority,
    string Message
);

public record AddTicketMessageDto(
    string Message
);