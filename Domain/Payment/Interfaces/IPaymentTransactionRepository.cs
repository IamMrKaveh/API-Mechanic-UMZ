namespace Domain.Payment.Interfaces;

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default);

    Task<bool> HasSuccessfulPaymentAsync(Guid orderId, CancellationToken ct = default);

    Task<bool> HasPendingPaymentAsync(Guid orderId, CancellationToken ct = default);

    void Update(PaymentTransaction transaction);

    void SetOriginalRowVersion(PaymentTransaction entity, byte[] rowVersion);

    Task<PaymentTransaction?> GetByIdAsync(PaymentTransactionId id, CancellationToken ct = default);

    Task<PaymentTransaction?> GetByAuthorityAsync(string authority, CancellationToken ct = default);

    Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(DateTime cutoffTime, CancellationToken ct = default);

    Task<IEnumerable<PaymentTransaction>> GetSuccessfulByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<PaymentTransaction?> GetLatestByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<PaymentTransaction?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}