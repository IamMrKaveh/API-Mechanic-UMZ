using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;

namespace Domain.Payment.Interfaces;

public interface IPaymentTransactionRepository
{
    Task AddAsync(
        PaymentTransaction transaction,
        CancellationToken ct = default);

    void Update(PaymentTransaction transaction);

    Task<PaymentTransaction?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default);

    Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default);

    Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<PaymentTransaction?> GetActiveByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);
}