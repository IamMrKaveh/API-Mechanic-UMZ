namespace DataAccessLayer.Models.User;

[Index(nameof(UserId))]
[Index(nameof(IsDefault))]
public class TUserAddress : IAuditable, ISoftDeletable
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty; // e.g., Home, Work

    [Required, MaxLength(100)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required, Phone, MaxLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(10)]
    [RegularExpression(@"^\d{10}$")]
    public string PostalCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
}