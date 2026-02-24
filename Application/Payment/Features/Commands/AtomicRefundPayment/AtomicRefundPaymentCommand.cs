namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public sealed record AtomicRefundPaymentCommand(
    int OrderId,
    int RequestedByUserId,
    string Reason,
    decimal? PartialAmount = null
) : IRequest<AtomicRefundResult>;

public sealed record AtomicRefundResult(
    bool IsSuccess,
    string? RefundTransactionId,
    decimal? RefundedAmount,
    string? Error);

public record GatewayRefundResultDto(
    bool IsSuccess,
    string? RefundTransactionId,
    string? Message);