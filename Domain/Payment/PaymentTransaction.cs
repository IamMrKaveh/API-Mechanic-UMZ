namespace Domain.Payment;

public class PaymentTransaction : IAuditable
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order.Order Order { get; set; } = null!;

    public required string Authority { get; set; }

    public decimal Amount { get; set; }

    public required string Status { get; set; }

    public required string Gateway { get; set; }

    public long? RefId { get; set; }

    public string? CardPan { get; set; }

    public string? CardHash { get; set; }

    public decimal Fee { get; set; }

    public string? IpAddress { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }
}