using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Repositories;

public sealed class PaymentRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<PaymentTransaction, PaymentTransactionId>(context), IPaymentTransactionRepository
{
    public async Task<PaymentTransaction?> GetByAuthorityAsync(
        string authority, CancellationToken ct = default)
        => await Context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Authority == authority, ct);

    public async Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime, CancellationToken ct = default)
        => await Context.PaymentTransactions
            .Where(t =>
                t.Status == PaymentStatus.Pending &&
                t.ExpiresAt < cutoffTime)
            .ToListAsync(ct);

    public async Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await Context.PaymentTransactions
            .Where(t =>
                t.OrderId == orderId &&
                t.Status == PaymentStatus.Success &&
                t.RefId.HasValue)
            .FirstOrDefaultAsync(ct);

    public async Task<PaymentTransaction?> GetActiveByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await Context.PaymentTransactions
            .Where(t =>
                t.OrderId == orderId &&
                (t.Status == PaymentStatus.Pending ||
                 t.Status == PaymentStatus.Processing))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
}