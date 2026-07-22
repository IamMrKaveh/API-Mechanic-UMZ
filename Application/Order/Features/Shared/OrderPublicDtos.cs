namespace Application.Order.Features.Shared;

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
    public bool IsCancellable { get; init; }
    public IReadOnlyList<string> AllowedTransitions { get; init; } = [];
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
    public string? ImageUrl { get; init; }
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
