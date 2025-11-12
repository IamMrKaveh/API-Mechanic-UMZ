namespace DataAccessLayer.Models.Cart;

[Index(nameof(UserId))]
[Index(nameof(GuestToken))]
[Index(nameof(LastUpdated))]
public class TCarts
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual TUsers? User { get; set; }

    [MaxLength(100, ErrorMessage = "توکن مهمان نمی‌تواند بیشتر از 100 کاراکتر باشد")]
    public string? GuestToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TCartItems> CartItems { get; set; } = new List<TCartItems>();
}