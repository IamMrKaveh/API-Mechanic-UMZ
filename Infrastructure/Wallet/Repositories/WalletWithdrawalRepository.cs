using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletWithdrawalRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<WalletWithdrawalRequest, WalletWithdrawalRequestId>(context), IWalletWithdrawalRepository
{
    public override void Update(WalletWithdrawalRequest withdrawal)
    {
        var entry = Context.Entry(withdrawal);
        if (entry.State == EntityState.Detached)
            Context.Set<WalletWithdrawalRequest>().Attach(withdrawal);
        entry.State = EntityState.Modified;
    }

    public async Task<WalletWithdrawalRequest?> GetByIdForUpdateAsync(
        WalletWithdrawalRequestId id,
        CancellationToken ct = default)
    {
        var withdrawal = await Context.Set<WalletWithdrawalRequest>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (withdrawal is not null)
        {
            var entry = Context.Entry(withdrawal);
            entry.Property("xmin").IsModified = false;
            entry.OriginalValues["xmin"] = entry.CurrentValues["xmin"];
        }

        return withdrawal;
    }

    public async Task<int> CountByUserAndStatusAsync(
        UserId userId,
        WalletWithdrawalStatus status,
        CancellationToken ct = default)
        => await Context.Set<WalletWithdrawalRequest>()
            .CountAsync(x => x.UserId == userId && x.Status == status, ct);
}
