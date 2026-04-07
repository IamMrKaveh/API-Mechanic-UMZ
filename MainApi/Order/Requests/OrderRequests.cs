namespace Presentation.Order.Requests;

public record CheckoutFromCartRequest(
    Guid ShippingId,
    Guid AddressId,
    string? DiscountCode = null,
    string? PaymentGateway = null
);

public record CancelOrderRequest(string Reason);

public record RequestReturnRequest(string Reason);

public record MarkAsShippedRequest(string? TrackingNumber = null);

public record UpdateOrderStatusByIdRequest(string NewStatus);

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