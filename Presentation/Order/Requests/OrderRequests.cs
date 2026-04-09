namespace Presentation.Order.Requests;

public record CheckoutFromCartRequest(
    Guid CartId,
    Guid ShippingId,
    Guid AddressId,
    string? DiscountCode = null,
    string? PaymentGateway = null
);

public record CancelOrderRequest(string Reason);

public record RequestReturnRequest(string Reason);

public record MarkAsShippedRequest(
    string? TrackingNumber = null,
    string? RowVersion = null
);

public record UpdateOrderStatusByIdRequest(
    Guid OrderStatusId,
    string RowVersion
);

public record AdminCreateOrderRequest(
    Guid UserId,
    Guid ShippingId,
    string ReceiverFullName,
    string ReceiverPhone,
    string Province,
    string City,
    string Street,
    string PostalCode,
    IReadOnlyList<OrderItemRequest> Items
);

public record OrderItemRequest(Guid VariantId, int Quantity);

public record CreateOrderStatusRequest(
    string Name,
    string DisplayName,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit);

public record UpdateOrderStatusRequest(
    string Name,
    string DisplayName,
    string? Description,
    int SortOrder,
    bool IsDefault
);