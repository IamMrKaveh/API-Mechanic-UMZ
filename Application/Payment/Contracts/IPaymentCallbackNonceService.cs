namespace Application.Payment.Contracts;

public interface IPaymentCallbackNonceService
{
    Task<string> IssueAsync(Guid paymentTransactionId, TimeSpan ttl, CancellationToken ct = default);

    Task<bool> ValidateAndConsumeAsync(Guid paymentTransactionId, string nonce, CancellationToken ct = default);
}
