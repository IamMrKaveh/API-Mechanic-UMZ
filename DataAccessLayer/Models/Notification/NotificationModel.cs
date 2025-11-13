namespace DataAccessLayer.Models.Notification;

[Index(nameof(UserId), nameof(IsRead), nameof(CreatedAt))]
[Index(nameof(Type), nameof(IsRead))]
public class TNotification : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    [Required, MaxLength(200)]
    public required string Title { get; set; }

    [Required, MaxLength(1000)]
    public required string Message { get; set; }

    [Required, MaxLength(50)]
    public required string Type { get; set; }

    [Required]
    public bool IsRead { get; set; }

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public int? RelatedEntityId { get; set; }

    [MaxLength(50)]
    public string? RelatedEntityType { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}