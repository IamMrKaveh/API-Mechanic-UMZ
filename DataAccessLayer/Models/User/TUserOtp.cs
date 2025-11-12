namespace DataAccessLayer.Models.User;

[Index(nameof(UserId))]
[Index(nameof(ExpiresAt))]
public class TUserOtp
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    [Required(ErrorMessage = "کد تایید الزامی است")]
    [MaxLength(512, ErrorMessage = "کد تایید نمی‌تواند بیشتر از 512 کاراکتر باشد")]
    public string OtpHash { get; set; } = string.Empty;

    [Required(ErrorMessage = "زمان انقضا الزامی است")]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsUsed { get; set; } = false;

    [Range(0, 5, ErrorMessage = "تعداد تلاش باید بین 0 تا 5 باشد")]
    public int AttemptCount { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }
}