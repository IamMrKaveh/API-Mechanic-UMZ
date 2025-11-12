namespace DataAccessLayer.Models.Security;

[Index(nameof(Key), nameof(ExpiresAt))]
public class TRateLimit
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "کلید الزامی است")]
    [MaxLength(200, ErrorMessage = "کلید نمی‌تواند بیشتر از 200 کاراکتر باشد")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "تعداد الزامی است")]
    public int Count { get; set; }

    [Required(ErrorMessage = "زمان آخرین تلاش الزامی است")]
    public DateTime LastAttempt { get; set; }

    [Required(ErrorMessage = "زمان انقضا الزامی است")]
    public DateTime ExpiresAt { get; set; }
}