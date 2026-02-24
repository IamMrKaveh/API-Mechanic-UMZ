namespace Application.Support.Features.Shared;

public class TicketDto
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

public class TicketDetailDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public List<TicketMessageDto> Messages { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public class TicketMessageDto
{
    public int Id { get; init; }
    public int TicketId { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsAdminResponse { get; internal init; }
}

public class CreateTicketDto
{
    public string Subject { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class AddTicketMessageDto
{
    public string Message { get; init; } = string.Empty;
}