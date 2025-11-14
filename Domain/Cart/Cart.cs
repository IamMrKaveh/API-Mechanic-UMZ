namespace Domain.Cart;

public class Cart
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User.User? User { get; set; }

    public string? GuestToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = [];
}