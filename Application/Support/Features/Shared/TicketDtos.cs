namespace Application.Support.Features.Shared;

public class TicketDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class TicketDetailDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public List<TicketMessageDto> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class TicketMessageDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAdminResponse { get; internal set; }
}

public class CreateTicketDto
{
    public string Subject { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AddTicketMessageDto
{
    public string Message { get; set; } = string.Empty;
}