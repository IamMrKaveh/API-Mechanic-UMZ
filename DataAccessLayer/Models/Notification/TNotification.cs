namespace DataAccessLayer.Models.Notification;

[Index(nameof(UserId), nameof(IsRead))]
[Index(nameof(CreatedAt))]
public class TNotification : IAuditable
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty;
    // OrderStatusChanged, PaymentSuccess, ProductAvailable, Promotion

    public bool IsRead { get; set; } = false;

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public int? RelatedEntityId { get; set; }

    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}