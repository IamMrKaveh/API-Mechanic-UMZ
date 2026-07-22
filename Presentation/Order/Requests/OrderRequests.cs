namespace Presentation.Order.Requests;

public record GetAdminOrdersRequest(
    Guid? UserId = null,
    string? Status = null,
    bool? IsPaid = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 10);

public record GetUserOrdersRequest(
    string? Status = null,
    int Page = 1,
    int PageSize = 10);

public record GetOrderStatusesRequest(
    bool? OnlyActive = null);

public record GetOrderStatisticsRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public record CheckoutFromCartRequest(
    Guid CartId,
    Guid ShippingId,
    Guid AddressId,
    string? DiscountCode = null,
    string? PaymentGateway = null,
    Guid? PaymentMethodId = null);

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
    IReadOnlyList<OrderItemRequest> Items);

public record OrderItemRequest(Guid VariantId, int Quantity);

public record CreateOrderStatusRequest(
    string Name,
    string DisplayName,
    string? Icon,
    string? Color,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit);

public record UpdateOrderStatusRequest(
    string DisplayName,
    string? Icon,
    string? Color,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit);
