namespace Presentation.Payment.Requests;

public record AdminPaymentSearchRequest(
    Guid? OrderId = null,
    Guid? UserId = null,
    string? Status = null,
    string? Gateway = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public record RefundPaymentRequest(string Reason);

public record InitiatePaymentRequest(
    Guid OrderId,
    string Gateway);

public record WebhookPayloadRequest(
    string Authority,
    string Status);