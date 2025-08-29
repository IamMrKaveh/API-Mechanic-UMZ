namespace DataAccessLayer.Models.User;

public class TUserOtp
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    [Required]
    public string OtpHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; } = false;
    public int AttemptCount { get; set; } = 0;
}