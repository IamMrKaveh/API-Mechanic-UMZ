namespace Domain.User;

public class User : BaseEntity
{
    public required string PhoneNumber { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAdmin { get; set; }

    public ICollection<UserAddress> UserAddresses { get; set; } = [];
    public ICollection<Cart.Cart> UserCarts { get; set; } = [];
    public ICollection<Order.Order> UserOrders { get; set; } = [];
    public ICollection<UserOtp> UserOtps { get; set; } = [];
    public ICollection<Notification.Notification> Notifications { get; set; } = [];
    public ICollection<Product.ProductReview> Reviews { get; set; } = [];
    public ICollection<Auth.UserSession> UserSessions { get; set; } = [];
    public ICollection<Discount.DiscountUsage> DiscountUsages { get; set; } = [];
    public ICollection<Inventory.InventoryTransaction> InventoryTransactions { get; set; } = [];
}