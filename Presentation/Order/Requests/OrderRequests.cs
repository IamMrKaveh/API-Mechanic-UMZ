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

public record GetOrderStatisticsRequest(
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public record CheckoutFromCartRequest(
    Guid CartId,
    Guid ShippingId,
    Guid AddressId,
    string? DiscountCode = null,
    string? PaymentGateway = null);

public record CancelOrderRequest(string Reason);

public record RequestReturnRequest(string Reason);

public record MarkAsShippedRequest(
    string? TrackingNumber = null,
    string? RowVersion = null);

public record UpdateOrderStatusByIdRequest(
    string NewStatus,
    string RowVersion);

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
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit);

public record UpdateOrderStatusRequest(
    string Name,
    string DisplayName,
    string? Description,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit);