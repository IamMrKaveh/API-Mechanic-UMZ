namespace Domain.User;

public class Wishlist : BaseEntity
{
    public int UserId { get; private set; }
    public int ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User User { get; private set; } = null!;
    public Product.Product Product { get; private set; } = null!;

    private Wishlist()
    { }

    public static Wishlist Create(int userId, int productId)
    {
        return new Wishlist
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };
    }
}