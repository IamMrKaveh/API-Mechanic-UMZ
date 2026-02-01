namespace Domain.Support;

public class TicketMessage : BaseEntity
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int? SenderId { get; set; }
    public required string Message { get; set; }
    public bool IsAdminResponse { get; set; }
}