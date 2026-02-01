namespace Application.DTOs.Support;

public class TicketDto { public int Id { get; set; } public string Subject { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Priority { get; set; } = string.Empty; public DateTime CreatedAt { get; set; } public DateTime? UpdatedAt { get; set; } }

public class TicketDetailDto : TicketDto { public List<TicketMessageDto> Messages { get; set; } = []; }

public class TicketMessageDto { public int Id { get; set; } public string Message { get; set; } = string.Empty; public bool IsAdminResponse { get; set; } public DateTime CreatedAt { get; set; } }

public class CreateTicketDto { public required string Subject { get; set; } public required string Priority { get; set; } public required string Message { get; set; } }

public class AddTicketMessageDto { public required string Message { get; set; } }