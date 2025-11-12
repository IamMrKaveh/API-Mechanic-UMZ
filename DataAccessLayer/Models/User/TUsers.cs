namespace DataAccessLayer.Models.User;

[Index(nameof(PhoneNumber), IsUnique = true)]
[Index(nameof(IsDeleted))]
[Index(nameof(IsActive))]
public class TUsers : IAuditable, ISoftDeletable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "شماره تلفن الزامی است")]
    [Phone(ErrorMessage = "فرمت شماره تلفن نامعتبر است")]
    [MaxLength(15, ErrorMessage = "شماره تلفن نمی‌تواند بیشتر از 15 کاراکتر باشد")]
    [RegularExpression(@"^09\d{9}$")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "نام نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string? FirstName { get; set; }

    [MaxLength(100, ErrorMessage = "نام خانوادگی نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }

    public virtual ICollection<TUserAddress> UserAddresses { get; set; } = new List<TUserAddress>();
    public virtual ICollection<TCarts> UserCarts { get; set; } = new List<TCarts>();
    public virtual ICollection<TOrders> UserOrders { get; set; } = new List<TOrders>();
    public virtual ICollection<TUserOtp> UserOtps { get; set; } = new List<TUserOtp>();
    public virtual ICollection<TRefreshToken> RefreshTokens { get; set; } = new List<TRefreshToken>();
    public virtual ICollection<TNotification> Notifications { get; set; } = new List<TNotification>();
    public virtual ICollection<TProductReview> Reviews { get; set; } = new List<TProductReview>();
    public virtual ICollection<TUserSession> UserSessions { get; set; } = new List<TUserSession>();
}