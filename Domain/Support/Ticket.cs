namespace Domain.Support;

public class Ticket : BaseEntity
{
    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public required string Subject { get; set; }
    public required string Priority { get; set; }
    public string Status { get; set; } = "Open";

    public ICollection<TicketMessage> Messages { get; set; } = [];
}