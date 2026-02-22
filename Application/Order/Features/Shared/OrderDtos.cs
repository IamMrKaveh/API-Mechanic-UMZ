namespace Application.Order.Features.Shared;

public record OrderDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public int OrderStatusId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal TotalProfit { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public int ShippingId { get; init; }
    public int? DiscountCodeId { get; init; }
    public DateTime? PaymentDate { get; init; }
    public DateTime? ShippedDate { get; init; }
    public DateTime? DeliveryDate { get; init; }
    public string? CancellationReason { get; init; }
    public UserAddressDto? UserAddress { get; init; }
    public List<OrderItemDto> OrderItems { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public string StatusDisplayName { get; init; } = string.Empty;
    public ShippingDto? Shipping { get; init; }
    public bool IsPaid { get; init; }
    public bool IsCancelled { get; init; }
    public string? RowVersion { get; init; }
}

public record OrderItemDto
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public int VariantId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductIcon { get; init; }
    public string? VariantSku { get; init; }
    public string? VariantAttributes { get; init; }
    public int Quantity { get; init; }
    public decimal PurchasePriceAtOrder { get; init; }
    public decimal SellingPriceAtOrder { get; init; }
    public decimal OriginalPriceAtOrder { get; init; }
    public decimal DiscountAtOrder { get; init; }
    public decimal Amount { get; init; }
    public decimal Profit { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
}

public record OrderStatusDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Color { get; init; }
}

public record CreateOrderItemDto
{
    public int VariantId { get; init; }
    public int Quantity { get; init; }
}

public record AdminOrderDto : OrderDto
{
    public new decimal TotalProfit { get; init; }
    public UserSummaryDto? User { get; init; }
    public int OrderItemsCount { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsDeleted { get; init; }
}

public record AddressSnapshotDto
{
    public int OriginalAddressId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}

public record UserSummaryDto
{
    public int Id { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsAdmin { get; init; }
}

public record OrderStatisticsDto
{
    public int TotalOrders { get; init; }
    public int PaidOrders { get; init; }
    public int PendingOrders { get; init; }
    public int CancelledOrders { get; init; }
    public int ProcessingOrders { get; init; }
    public int ShippedOrders { get; init; }
    public int DeliveredOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TotalProfit { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal PaidOrdersPercentage { get; init; }
    public decimal CancellationRate { get; init; }
    public decimal ProfitMargin { get; init; }
    public Dictionary<string, int>? StatusBreakdown { get; init; }
}

public record CreateOrderFromCartDto
{
    public int? UserAddressId { get; init; }
    public CreateUserAddressDto? NewAddress { get; init; }
    public bool SaveNewAddress { get; init; }
    public int ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CheckoutItemPriceDto> ExpectedItems { get; init; } = new();
    public string? CallbackUrl { get; init; }
}

public record CheckoutItemPriceDto
{
    public int VariantId { get; init; }
    public decimal ExpectedPrice { get; init; }
}

public record UpdateOrderDto
{
    public int? ShippingId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record UpdateOrderItemDto
{
    public int OrderItemId { get; init; }
    public int Quantity { get; init; }
}

public record UpdateOrderStatusDto
{
    public string? DisplayName { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int? SortOrder { get; init; }
    public bool? AllowCancel { get; init; }
    public bool? AllowEdit { get; init; }
}

public record CheckoutResultDto
{
    public int OrderId { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? Error { get; init; }
    public bool Success { get; init; }
}

public record AdminCreateOrderDto
{
    public int UserId { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public int UserAddressId { get; init; }
    public int ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<AdminCreateOrderItemDto> OrderItems { get; init; } = new();
}

public record AdminCreateOrderItemDto
{
    public int VariantId { get; init; }
    public int Quantity { get; init; }
    public decimal SellingPrice { get; init; }
}

public record CreateOrderDto
{
    public int UserId { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public int UserAddressId { get; init; }
    public int ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CreateOrderItemDto> OrderItems { get; init; } = new();
}

public record UpdateOrderStatusByIdDto
{
    public string OrderStatusId { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public int UpdatedByUserId { get; init; }
}