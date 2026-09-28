using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletFraudAlertRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<WalletFraudAlert, WalletFraudAlertId>(context), IWalletFraudAlertRepository
{
    public override void Update(WalletFraudAlert alert)
    {
        var entry = Context.Entry(alert);
        if (entry.State == EntityState.Detached)
            Context.Set<WalletFraudAlert>().Attach(alert);

        entry.State = EntityState.Modified;
    }

    public async Task<bool> HasRecentAlertAsync(
        WalletId walletId,
        string ruleName,
        TimeSpan cooldown,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(cooldown);

        return await Context.Set<WalletFraudAlert>()
            .AsNoTracking()
            .AnyAsync(a =>
                a.WalletId == walletId
                && a.RuleName == ruleName
                && a.TriggeredAt >= cutoff
                && a.Status == FraudAlertStatus.Open,
                ct);
    }
}