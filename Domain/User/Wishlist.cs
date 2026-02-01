namespace Domain.User;

public class Wishlist : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ProductId { get; set; }
    public Product.Product Product { get; set; } = null!;
}