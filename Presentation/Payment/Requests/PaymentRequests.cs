namespace Presentation.Payment.Requests;

public record AdminPaymentSearchRequest(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    Guid? UserId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Gateway = null
);

public record RefundPaymentRequest(string Reason);

public record InitiatePaymentRequest(
    Guid OrderId,
    string Gateway
);

public record WebhookPayload(
    string Authority,
    string Status
);