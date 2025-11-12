namespace DataAccessLayer.Models.Payment;

[Index(nameof(OrderId))]
[Index(nameof(Authority), IsUnique = true)]
[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
public class TPaymentTransaction : IAuditable
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    public virtual TOrders Order { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Authority { get; set; } = string.Empty;

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = string.Empty;
    // Pending, Success, Failed, Cancelled

    [Required, MaxLength(50)]
    public string Gateway { get; set; } = string.Empty; // ZarinPal, Saman, etc.

    public long? RefId { get; set; }

    [MaxLength(100)]
    public string? CardPan { get; set; }

    [MaxLength(100)]
    public string? CardHash { get; set; }

    public decimal Fee { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}