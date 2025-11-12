namespace DataAccessLayer.Models.Auth;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ExpiresAt))]
public class TRefreshToken : IAuditable
{
    [Key]
    public int Id { get; set; }

    public virtual TUsers User { get; set; } = null!;
    public int UserId { get; set; }

    [Required(ErrorMessage = "توکن الزامی است")]
    [MaxLength(512, ErrorMessage = "توکن نمی‌تواند بیشتر از 512 کاراکتر باشد")]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(45, ErrorMessage = "آدرس IP نمی‌تواند بیشتر از 45 کاراکتر باشد")]
    public string CreatedByIp { get; set; } = string.Empty;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(512, ErrorMessage = "توکن جایگزین نمی‌تواند بیشتر از 512 کاراکتر باشد")]
    public string? ReplacedByTokenHash { get; set; }

    [MaxLength(255, ErrorMessage = "اطلاعات مرورگر نمی‌تواند بیشتر از 255 کاراکتر باشد")]
    public string? UserAgent { get; set; }
}