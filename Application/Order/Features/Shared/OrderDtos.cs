using Application.Shipping.Features.Shared;
using Application.User.Features.Shared;

namespace Application.Order.Features.Shared;

public sealed record CreateOrderItemDto
{
    public Guid VariantId { get; init; }
    public int Quantity { get; init; }
}

public sealed record AdminOrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public Guid OrderStatusId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal TotalProfit { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public Guid ShippingId { get; init; }
    public Guid? DiscountCodeId { get; init; }
    public DateTime? PaymentDate { get; init; }
    public DateTime? ShippedDate { get; init; }
    public DateTime? DeliveryDate { get; init; }
    public string? CancellationReason { get; init; }
    public UserAddressDto? UserAddress { get; init; }
    public List<OrderItemDto> OrderItems { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public string StatusDisplayName { get; init; } = string.Empty;
    public ShippingDto? Shipping { get; init; }
    public bool IsPaid { get; init; }
    public bool IsCancelled { get; init; }
    public string? RowVersion { get; init; }
    public UserSummaryDto? User { get; init; }
    public int OrderItemsCount { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsDeleted { get; init; }
}

public sealed record AddressSnapshotDto
{
    public Guid OriginalAddressId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ReceiverName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Province { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}

public sealed record UserSummaryDto
{
    public Guid Id { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsAdmin { get; init; }
}

public sealed record CreateOrderFromCartDto
{
    public Guid? UserAddressId { get; init; }
    public CreateUserAddressDto? NewAddress { get; init; }
    public bool SaveNewAddress { get; init; }
    public Guid ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CheckoutItemPriceDto> ExpectedItems { get; init; } = [];
    public string? CallbackUrl { get; init; }
}

public sealed record CheckoutItemPriceDto
{
    public Guid VariantId { get; init; }
    public decimal ExpectedPrice { get; init; }
}

public sealed record UpdateOrderDto
{
    public Guid? ShippingId { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed record UpdateOrderItemDto
{
    public Guid OrderItemId { get; init; }
    public int Quantity { get; init; }
}

public sealed record UpdateOrderStatusDto
{
    public string? DisplayName { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int? SortOrder { get; init; }
    public bool? AllowCancel { get; init; }
    public bool? AllowEdit { get; init; }
}

public sealed record AdminCreateOrderDto
{
    public Guid UserId { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public Guid UserAddressId { get; init; }
    public Guid ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<AdminCreateOrderItemDto> OrderItems { get; init; } = [];
}

public sealed record AdminCreateOrderItemDto
{
    public Guid VariantId { get; init; }
    public int Quantity { get; init; }
    public decimal SellingPrice { get; init; }
}

public sealed record CreateOrderDto
{
    public Guid UserId { get; init; }
    public string ReceiverName { get; init; } = string.Empty;
    public Guid UserAddressId { get; init; }
    public Guid ShippingId { get; init; }
    public string? DiscountCode { get; init; }
    public List<CreateOrderItemDto> OrderItems { get; init; } = [];
}

public sealed record UpdateOrderStatusByIdDto
{
    public Guid OrderStatusId { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public int UpdatedByUserId { get; init; }
}

public record OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusDisplayName { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public bool IsPaid { get; init; }
    public bool IsCancelled { get; init; }
    public string? CancellationReason { get; init; }
    public ReceiverInfoDto? ReceiverInfo { get; init; }
    public DeliveryAddressDto? DeliveryAddress { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record OrderListItemDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string StatusDisplayName { get; init; } = string.Empty;
    public decimal FinalAmount { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
}

public record ReceiverInfoDto
{
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}

public record DeliveryAddressDto
{
    public string Province { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
}

public record OrderStatusDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool AllowCancel { get; init; }
    public bool AllowEdit { get; init; }
}

public record OrderStatisticsDto
{
    public int TotalOrders { get; init; }
    public int PendingOrders { get; init; }
    public int ProcessingOrders { get; init; }
    public int CompletedOrders { get; init; }
    public int CancelledOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageOrderValue { get; init; }
}

public record CheckoutResultDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal FinalAmount { get; init; }
    public string? PaymentUrl { get; init; }
    public string? PaymentAuthority { get; init; }
}