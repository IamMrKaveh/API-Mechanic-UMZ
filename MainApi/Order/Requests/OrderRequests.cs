namespace Presentation.Order.Requests;

public sealed record MarkAsShippedRequest(string RowVersion);
public sealed record CancelOrderRequest(string Reason);

public sealed record ConfirmDeliveryRequest(string RowVersion);

public sealed record RequestReturnRequest(
    string Reason,
    string RowVersion
);