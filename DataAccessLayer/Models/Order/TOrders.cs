using DataAccessLayer.Models.Payment;

namespace DataAccessLayer.Models.Order;

[Index(nameof(UserId))]
[Index(nameof(IdempotencyKey), IsUnique = true)]
[Index(nameof(CreatedAt))]
[Index(nameof(OrderStatusId))]
[Index(nameof(UserId), nameof(CreatedAt), nameof(OrderStatusId))]
public class TOrders : IAuditable
{
    [Key]
    public int Id { get; set; }

    public virtual TUsers User { get; set; } = null!;
    public int UserId { get; set; }

    // Address fields are replaced by AddressSnapshot and UserAddressId
    public int? UserAddressId { get; set; }
    public virtual TUserAddress? UserAddress { get; set; }
    public string AddressSnapshot { get; set; } = string.Empty; // JSON snapshot of TUserAddress

    [Column(TypeName = "decimal(19,4)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal TotalProfit { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal ShippingCost { get; set; }

    public virtual TDiscountCode? DiscountCode { get; set; }
    public int? DiscountCodeId { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal DiscountAmount { get; set; } = 0;

    public decimal FinalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeliveryDate { get; set; }

    public virtual ICollection<TOrderItems> OrderItems { get; set; } = new List<TOrderItems>();
    public virtual TOrderStatus OrderStatus { get; set; } = null!;
    public int OrderStatusId { get; set; }

    public virtual TShippingMethod ShippingMethod { get; set; } = null!;
    public int ShippingMethodId { get; set; }

    [Required, MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    // Payment fields are moved to TPaymentTransaction
    public bool IsPaid { get; set; } = false;

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<TPaymentTransaction> PaymentTransactions { get; set; } = new List<TPaymentTransaction>();
    public virtual ICollection<TDiscountUsage> DiscountUsages { get; set; } = new List<TDiscountUsage>();
}