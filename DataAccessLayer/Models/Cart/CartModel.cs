namespace DataAccessLayer.Models.Cart;

[Index(nameof(UserId), nameof(GuestToken), nameof(LastUpdated))]
[Index(nameof(GuestToken))]
public class TCarts
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }
    public TUsers? User { get; set; }

    [MaxLength(100)]
    public string? GuestToken { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ICollection<TCartItems> CartItems { get; set; } = [];
}

[Index(nameof(CartId), nameof(VariantId), IsUnique = true)]
[Index(nameof(VariantId))]
public class TCartItems
{
    [Key]
    public int Id { get; set; }

    [Required, Range(1, 1000)]
    public int Quantity { get; set; }

    [Required]
    public int CartId { get; set; }
    public TCarts Cart { get; set; } = null!;

    [Required]
    public int VariantId { get; set; }
    public TProductVariant Variant { get; set; } = null!;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}