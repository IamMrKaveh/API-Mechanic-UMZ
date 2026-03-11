using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;

namespace Infrastructure.Payment.Repositories;

public class PaymentTransactionRepository(DBContext context) : IPaymentTransactionRepository
{
    private readonly DBContext _context = context;

    public async Task AddAsync(
        PaymentTransaction transaction,
        CancellationToken ct = default)
    {
        await _context.PaymentTransactions.AddAsync(transaction, ct);
    }

    public async Task<bool> HasSuccessfulPaymentAsync(
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .AnyAsync(t => t.OrderId == orderId && t.Status.Value == "Success" && !t.IsDeleted, ct);
    }

    public async Task<bool> HasPendingPaymentAsync(
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .AnyAsync(t => t.OrderId == orderId && t.Status.Value == "Pending" && !t.IsDeleted, ct);
    }

    public void Update(PaymentTransaction transaction)
    {
        _context.PaymentTransactions.Update(transaction);
    }

    public void SetOriginalRowVersion(
        PaymentTransaction entity,
        byte[] rowVersion)
    {
        _context.Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}