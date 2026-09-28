using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletDebitRequestRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<WalletDebitRequest, WalletDebitRequestId>(context), IWalletDebitRequestRepository
{
    public async Task<IReadOnlyList<WalletDebitRequest>> GetByOwnerAsync(
        UserId ownerId,
        WalletDebitRequestStatus? status = null,
        CancellationToken ct = default)
    {
        var query = Context.Set<WalletDebitRequest>().AsQueryable();
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
        => await Context.Set<WalletDebitRequest>()
            .Where(r => r.OwnerId == ownerId && r.Status == WalletDebitRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
}
