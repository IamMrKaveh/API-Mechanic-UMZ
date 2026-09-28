using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletTransferRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<WalletTransfer, WalletTransferId>(context), IWalletTransferRepository
{
    public override void Update(WalletTransfer transfer)
    {
        var entry = Context.Entry(transfer);
        if (entry.State == EntityState.Detached)
            Context.Set<WalletTransfer>().Attach(transfer);
        entry.State = EntityState.Modified;
    }

    public async Task<WalletTransfer?> GetByIdForUpdateAsync(
        WalletTransferId id,
        CancellationToken ct = default)
    {
        var transfer = await Context.Set<WalletTransfer>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (transfer is not null)
        {
            var entry = Context.Entry(transfer);
            entry.Property("xmin").IsModified = false;
            entry.OriginalValues["xmin"] = entry.CurrentValues["xmin"];
        }

        return transfer;
    }

    public async Task<decimal> SumCompletedAmountForDayAsync(
        UserId fromUserId,
        DateTime dayUtc,
        CancellationToken ct = default)
    {
        var start = dayUtc.Date;
        var end = start.AddDays(1);

        return await Context.Set<WalletTransfer>()
            .Where(x => x.FromUserId == fromUserId
                        && x.Status == WalletTransferStatus.Completed
                        && x.CompletedAt != null
                        && x.CompletedAt >= start
                        && x.CompletedAt < end)
            .SumAsync(x => (decimal?)x.Amount.Amount, ct) ?? 0m;
    }

    public async Task<int> CountRecentPendingByUserAsync(
        UserId fromUserId,
        TimeSpan window,
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.Subtract(window);
        return await Context.Set<WalletTransfer>()
            .CountAsync(x => x.FromUserId == fromUserId
                             && x.CreatedAt >= since
                             && x.Status == WalletTransferStatus.PendingOtp, ct);
    }
}