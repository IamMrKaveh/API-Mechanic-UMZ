using Application.Shipping.Features.Shared;

namespace Application.Order.Features.Shared;

// === View DTOs ===
public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public int OrderStatusId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int ShippingMethodId { get; set; }
    public int? DiscountCodeId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? CancellationReason { get; set; }
    public UserAddressDto? UserAddress { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string StatusDisplayName { get; set; }
    public ShippingMethodDto? ShippingMethod { get; set; }
    public bool IsPaid { get; set; }
    public bool IsCancelled { get; set; }
    public string? RowVersion { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? ProductIcon { get; set; }
    public string? VariantSku { get; set; }
    public string? VariantAttributes { get; set; }
    public int Quantity { get; set; }

    public decimal PurchasePriceAtOrder { get; set; }
    public decimal SellingPriceAtOrder { get; set; }
    public decimal OriginalPriceAtOrder { get; set; }
    public decimal DiscountAtOrder { get; set; }
    public decimal Amount { get; set; }
    public decimal Profit { get; set; }
    public Dictionary<string, object>? Attributes { get; set; }
}

public class OrderStatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class CreateOrderItemDto
{
    public int VariantId { get; set; }
    public int Quantity { get; set; }
}

public class AdminOrderDto : OrderDto
{
    public new decimal TotalProfit { get; init; }
    public UserSummaryDto? User { get; init; }
    public int OrderItemsCount { get; init; }
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

// === Command DTOs ===

public record CreateOrderFromCartDto
{
    public int? UserAddressId { get; init; }
    public CreateUserAddressDto? NewAddress { get; init; }
    public bool SaveNewAddress { get; init; }
    public int ShippingMethodId { get; init; }
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
    public int? ShippingMethodId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpdateOrderItemDto
{
    public int OrderItemId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderStatusDto
{
    public string? DisplayName { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? SortOrder { get; set; }
    public bool? AllowCancel { get; set; }
    public bool? AllowEdit { get; set; }
}

public record CheckoutResultDto
{
    public int OrderId { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? Error { get; init; }
    public bool Success => string.IsNullOrEmpty(Error);
}

// === Admin Command DTOs ===

public record AdminCreateOrderDto
{
    public int UserId { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public int UserAddressId { get; init; }
    public int ShippingMethodId { get; init; }
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
    public int ShippingMethodId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CreateOrderItemDto> OrderItems { get; init; } = new();
}

public record UpdateOrderStatusByIdDto
{
    public string OrderStatusId { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public int UpdatedByUserId { get; init; }
}