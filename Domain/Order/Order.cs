namespace Domain.Order;

public class Order : IAuditable, ISoftDeletable, IActivatable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public int? UserAddressId { get; set; }
    public User.UserAddress? UserAddress { get; set; }

    public required string ReceiverName { get; set; }

    public required string AddressSnapshot { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalProfit { get; set; }

    public decimal ShippingCost { get; set; }

    public int? DiscountCodeId { get; set; }
    public Discount.DiscountCode? DiscountCode { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public int OrderStatusId { get; set; }
    public OrderStatus OrderStatus { get; set; } = null!;

    public int ShippingMethodId { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = null!;

    public required string IdempotencyKey { get; set; }

    public bool IsPaid { get; set; }

    public byte[]? RowVersion { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];
    public ICollection<Payment.PaymentTransaction> PaymentTransactions { get; set; } = [];
    public ICollection<Discount.DiscountUsage> DiscountUsages { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;

    public void RecalculateTotals()
    {
        TotalAmount = OrderItems.Sum(oi => oi.Amount);
        TotalProfit = OrderItems.Sum(oi => oi.Profit);
        FinalAmount = TotalAmount + ShippingCost - DiscountAmount;
    }
}