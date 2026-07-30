using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletDebitRequestRepository(DBContext context) : IWalletDebitRequestRepository
{
    public async Task<WalletDebitRequest?> GetByIdAsync(WalletDebitRequestId id, CancellationToken ct = default)
        => await context.Set<WalletDebitRequest>()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<WalletDebitRequest>> GetByOwnerAsync(
        UserId ownerId,
        WalletDebitRequestStatus? status = null,
        CancellationToken ct = default)
    {
        var query = context.Set<WalletDebitRequest>().AsQueryable();
        query = query.Where(r => r.OwnerId == ownerId);
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WalletDebitRequest>> GetPendingByOwnerAsync(
        UserId ownerId,
        CancellationToken ct = default)
        => await context.Set<WalletDebitRequest>()
            .Where(r => r.OwnerId == ownerId && r.Status == WalletDebitRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
