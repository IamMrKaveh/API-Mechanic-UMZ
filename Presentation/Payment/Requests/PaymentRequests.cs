namespace Presentation.Payment.Requests;

public record AdminPaymentSearchRequest(
    Guid? OrderId,
    Guid? UserId,
    string? Status,
    string? Gateway,
    DateTime? FromDate,
    DateTime? ToDate
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