namespace Presentation.Payment.Requests;

public record InitiatePaymentRequest(
    Guid OrderId,
    string Gateway,
    string? Description = null
);

public record RefundPaymentRequest(string Reason);