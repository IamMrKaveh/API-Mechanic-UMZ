namespace DataAccessLayer.Models.Payment;

[Index(nameof(OrderId), nameof(Status), nameof(CreatedAt))]
[Index(nameof(Authority), IsUnique = true)]
[Index(nameof(Status), nameof(CreatedAt))]
public class TPaymentTransaction : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public TOrders Order { get; set; } = null!;

    [Required, MaxLength(100)]
    public required string Authority { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, MaxLength(50)]
    public required string Status { get; set; }

    [Required, MaxLength(50)]
    public required string Gateway { get; set; }

    public long? RefId { get; set; }

    [MaxLength(100)]
    public string? CardPan { get; set; }

    [MaxLength(100)]
    public string? CardHash { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal Fee { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }
}