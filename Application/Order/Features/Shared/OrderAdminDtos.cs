using Application.Shipping.Features.Shared;
using Application.User.Features.Shared;

namespace Application.Order.Features.Shared;

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
    public bool IsCancellable { get; init; }
    public IReadOnlyList<string> AllowedTransitions { get; init; } = [];
    public UserSummaryDto? User { get; init; }
    public int OrderItemsCount { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsDeleted { get; init; }
}

public sealed record UserSummaryDto
{
    public Guid Id { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsAdmin { get; init; }
}

public sealed record UpdateOrderDto
{
    public Guid? ShippingId { get; init; }
}

public sealed record AdminCreateOrderItemDto
{
    public Guid VariantId { get; init; }
    public int Quantity { get; init; }
    public decimal SellingPrice { get; init; }
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
