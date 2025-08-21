namespace DataAccessLayer.Models.User;

public class TUserOtp : IUserOtp
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; }

    [Required]
    public string OtpHash { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUsed { get; set; } = false;
}
