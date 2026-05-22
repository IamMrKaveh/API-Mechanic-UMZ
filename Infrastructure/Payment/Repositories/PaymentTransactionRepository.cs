using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;

namespace Infrastructure.Payment.Repositories;

public sealed class PaymentRepository(DBContext context) : IPaymentTransactionRepository
{
    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default)
        => await context.PaymentTransactions.AddAsync(transaction, ct);

    public void Update(PaymentTransaction transaction)
        => context.PaymentTransactions.Update(transaction);

    public async Task<PaymentTransaction?> GetByAuthorityAsync(
        string authority, CancellationToken ct = default)
        => await context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);

    public async Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime, CancellationToken ct = default)
        => await context.PaymentTransactions
            .Where(t =>
                t.Status == Domain.Payment.ValueObjects.PaymentStatus.Pending &&
                t.ExpiresAt < cutoffTime)
            .ToListAsync(ct);

    public async Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .Where(t =>
                t.OrderId == orderId &&
                t.Status == Domain.Payment.ValueObjects.PaymentStatus.Success &&
                t.RefId.HasValue)
            .FirstOrDefaultAsync(ct);

    public async Task<PaymentTransaction?> GetActiveByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .Where(t =>
                t.OrderId == orderId &&
                (t.Status == Domain.Payment.ValueObjects.PaymentStatus.Pending ||
                 t.Status == Domain.Payment.ValueObjects.PaymentStatus.Processing))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
}