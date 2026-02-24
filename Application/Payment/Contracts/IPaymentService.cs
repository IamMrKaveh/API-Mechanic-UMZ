namespace Application.Payment.Contracts;

public interface IPaymentService
{
    Task<ServiceResult<(bool IsSuccess, string? Authority, string? PaymentUrl, string? Message)>> InitiatePaymentAsync(
        PaymentInitiationDto dto,
        CancellationToken ct = default
        );

    Task<ServiceResult<(bool IsVerified, long? RefId, string? CardPan, string? Message)>> VerifyPaymentAsync(
        string authority,
        int amount,
        CancellationToken ct = default
        );
}