using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.Repositories;

public sealed class PaymentRepository(DBContext context) : IPaymentTransactionRepository
{
    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default)
        => await context.PaymentTransactions.AddAsync(transaction, ct);

    public void Update(PaymentTransaction transaction)
        => context.PaymentTransactions.Update(transaction);

    public void SetOriginalRowVersion(PaymentTransaction entity, byte[] rowVersion)
        => context.Entry(entity).OriginalValues["RowVersion"] = rowVersion;

    public async Task<PaymentTransaction?> GetByIdAsync(
        PaymentTransactionId id, CancellationToken ct = default)
        => await context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PaymentTransaction?> GetByAuthorityAsync(
        string authority, CancellationToken ct = default)
        => await context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);

    public async Task<bool> HasSuccessfulPaymentAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .AnyAsync(t =>
                t.OrderId == orderId &&
                t.Status == Domain.Payment.ValueObjects.PaymentStatus.Success, ct);

    public async Task<bool> HasPendingPaymentAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .AnyAsync(t =>
                t.OrderId == orderId &&
                t.Status == Domain.Payment.ValueObjects.PaymentStatus.Pending, ct);

    public async Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime, CancellationToken ct = default)
        => await context.PaymentTransactions
            .Where(t =>
                t.Status == Domain.Payment.ValueObjects.PaymentStatus.Pending &&
                t.ExpiresAt < cutoffTime)
            .ToListAsync(ct);

    public async Task<IEnumerable<PaymentTransaction>> GetSuccessfulByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .Where(t =>
                t.OrderId == orderId &&
                t.Status == Domain.Payment.ValueObjects.PaymentStatus.Success)
            .ToListAsync(ct);

    public async Task<PaymentTransaction?> GetLatestByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

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