using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Interfaces;

public interface IPaymentTransactionRepository
{
    Task AddAsync(
        PaymentTransaction transaction,
        CancellationToken ct = default);

    Task<bool> HasSuccessfulPaymentAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<bool> HasPendingPaymentAsync(
        OrderId orderId,
        CancellationToken ct = default);

    void Update(PaymentTransaction transaction);

    void SetOriginalRowVersion(
        PaymentTransaction entity,
        byte[] rowVersion);

    Task<PaymentTransaction?> GetByIdAsync(
        PaymentTransactionId id,
        CancellationToken ct = default);

    Task<PaymentTransaction?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default);

    Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default);

    Task<IEnumerable<PaymentTransaction>> GetSuccessfulByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<PaymentTransaction?> GetLatestByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<PaymentTransaction?> GetActiveByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);
}