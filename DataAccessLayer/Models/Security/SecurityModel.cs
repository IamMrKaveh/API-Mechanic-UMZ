namespace DataAccessLayer.Models.Security;

[Index(nameof(Key), nameof(ExpiresAt))]
[Index(nameof(ExpiresAt))]
public class TRateLimit
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public required string Key { get; set; }

    [Required, Range(0, int.MaxValue)]
    public int Count { get; set; }

    [Required]
    public DateTime LastAttempt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime ExpiresAt { get; set; }
}