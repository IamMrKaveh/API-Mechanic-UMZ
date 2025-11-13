namespace DataAccessLayer.Models.Order;

[Index(nameof(UserId), nameof(CreatedAt), nameof(OrderStatusId))]
[Index(nameof(IdempotencyKey), IsUnique = true)]
[Index(nameof(IsPaid), nameof(OrderStatusId))]
[Index(nameof(DiscountCodeId))]
public class TOrders : IAuditable
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public TUsers User { get; set; } = null!;

    public int? UserAddressId { get; set; }
    public TUserAddress? UserAddress { get; set; }

    [Required]
    public required string AddressSnapshot { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }

    [Required, Column(TypeName = "decimal(19,4)")]
    public decimal TotalProfit { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal ShippingCost { get; set; }

    public int? DiscountCodeId { get; set; }
    public TDiscountCode? DiscountCode { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal FinalAmount { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeliveryDate { get; set; }

    [Required]
    public int OrderStatusId { get; set; }
    public TOrderStatus OrderStatus { get; set; } = null!;

    [Required]
    public int ShippingMethodId { get; set; }
    public TShippingMethod ShippingMethod { get; set; } = null!;

    [Required, MaxLength(100)]
    public required string IdempotencyKey { get; set; }

    [Required]
    public bool IsPaid { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<TOrderItems> OrderItems { get; set; } = [];
    public ICollection<TPaymentTransaction> PaymentTransactions { get; set; } = [];
    public ICollection<TDiscountUsage> DiscountUsages { get; set; } = [];
}

[Index(nameof(OrderId), nameof(VariantId))]
[Index(nameof(VariantId))]
public class TOrderItems
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public TOrders Order { get; set; } = null!;

    [Required]
    public int VariantId { get; set; }
    public TProductVariant Variant { get; set; } = null!;

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal PurchasePrice { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Profit { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<TInventoryTransaction> InventoryTransactions { get; set; } = [];
}

[Index(nameof(Name), IsUnique = true)]
public class TOrderStatus
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Icon { get; set; }

    public ICollection<TOrders> Orders { get; set; } = [];
}

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Cost))]
public class TShippingMethod
{
    [Key]
    public int Id { get; set; }

    [Required, StringLength(100)]
    public required string Name { get; set; }

    [Required, Column(TypeName = "decimal(19,4)"), Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? EstimatedDeliveryTime { get; set; }

    public ICollection<TOrders> Orders { get; set; } = [];
}