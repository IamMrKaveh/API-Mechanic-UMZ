namespace DataAccessLayer.Models.User;

[Index(nameof(PhoneNumber), IsUnique = true)]
[Index(nameof(IsDeleted), nameof(IsActive))]
[Index(nameof(IsAdmin))]
public class TUsers : BaseEntity
{
    [Required, Phone, MaxLength(15), RegularExpression(@"^09\d{9}$")]
    public required string PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Required]
    public bool IsAdmin { get; set; }

    public ICollection<TUserAddress> UserAddresses { get; set; } = [];
    public ICollection<TCarts> UserCarts { get; set; } = [];
    public ICollection<TOrders> UserOrders { get; set; } = [];
    public ICollection<TUserOtp> UserOtps { get; set; } = [];
    public ICollection<TNotification> Notifications { get; set; } = [];
    public ICollection<TProductReview> Reviews { get; set; } = [];
    public ICollection<TUserSession> UserSessions { get; set; } = [];
    public ICollection<TDiscountUsage> DiscountUsages { get; set; } = [];
    public ICollection<TInventoryTransaction> InventoryTransactions { get; set; } = [];
}

[Index(nameof(UserId), nameof(IsDefault))]
[Index(nameof(IsActive))]
public class TUserAddress : BaseEntity
{
    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    [Required, MaxLength(200)]
    public required string Title { get; set; }

    [Required, MaxLength(100)]
    public required string ReceiverName { get; set; }

    [Required, Phone, MaxLength(15)]
    public required string PhoneNumber { get; set; }

    [Required, MaxLength(100)]
    public required string Province { get; set; }

    [Required, MaxLength(100)]
    public required string City { get; set; }

    [Required, MaxLength(500)]
    public required string Address { get; set; }

    [Required, StringLength(10), RegularExpression(@"^\d{10}$")]
    public required string PostalCode { get; set; }

    [Required]
    public bool IsDefault { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [Column(TypeName = "decimal(10,8)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(11,8)")]
    public decimal? Longitude { get; set; }
}

[Index(nameof(UserId), nameof(ExpiresAt))]
[Index(nameof(IsUsed), nameof(ExpiresAt))]
public class TUserOtp
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    [Required, MaxLength(512)]
    public required string OtpHash { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsUsed { get; set; }

    [Required, Range(0, 5)]
    public int AttemptCount { get; set; }

    public DateTime? LockedUntil { get; set; }
}