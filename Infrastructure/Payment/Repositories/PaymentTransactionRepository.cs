using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Payment.Repositories;

public sealed class PaymentRepository(DBContext context) : IPaymentTransactionRepository
{
    public async Task<PaymentTransaction?> GetByIdAsync(
        PaymentTransactionId id,
        CancellationToken ct = default)
    {
        return await context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PaymentTransaction?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default)
    {
        return await context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.Authority == authority, ct);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        var results = await context.PaymentTransactions
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default)
    {
        await context.PaymentTransactions.AddAsync(transaction, ct);
    }

    public void Update(PaymentTransaction transaction)
    {
        context.PaymentTransactions.Update(transaction);
    }
}