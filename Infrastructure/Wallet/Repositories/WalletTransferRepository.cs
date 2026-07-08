using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletTransferRepository(DBContext context) : IWalletTransferRepository
{
    public async Task AddAsync(WalletTransfer transfer, CancellationToken ct = default)
        => await context.Set<WalletTransfer>().AddAsync(transfer, ct);

    public void Update(WalletTransfer transfer)
    {
        var entry = context.Entry(transfer);
        if (entry.State == EntityState.Detached)
            context.Set<WalletTransfer>().Attach(transfer);
        entry.State = EntityState.Modified;
    }

    public async Task<WalletTransfer?> GetByIdAsync(
        WalletTransferId id,
        CancellationToken ct = default)
        => await context.Set<WalletTransfer>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<WalletTransfer?> GetByIdForUpdateAsync(
        WalletTransferId id,
        CancellationToken ct = default)
    {
        var transfer = await context.Set<WalletTransfer>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (transfer is not null)
        {
            var entry = context.Entry(transfer);
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

        return await context.Set<WalletTransfer>()
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
        return await context.Set<WalletTransfer>()
            .CountAsync(x => x.FromUserId == fromUserId
                             && x.CreatedAt >= since
                             && x.Status == WalletTransferStatus.PendingOtp, ct);
    }
}