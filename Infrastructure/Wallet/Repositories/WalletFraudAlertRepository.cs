using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletFraudAlertRepository(DBContext context) : IWalletFraudAlertRepository
{
    public async Task AddAsync(WalletFraudAlert alert, CancellationToken ct = default)
        => await context.Set<WalletFraudAlert>().AddAsync(alert, ct);

    public async Task<WalletFraudAlert?> GetByIdAsync(WalletFraudAlertId id, CancellationToken ct = default)
        => await context.Set<WalletFraudAlert>()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public void Update(WalletFraudAlert alert)
    {
        var entry = context.Entry(alert);
        if (entry.State == EntityState.Detached)
            context.Set<WalletFraudAlert>().Attach(alert);

        entry.State = EntityState.Modified;
    }

    public async Task<bool> HasRecentAlertAsync(
        WalletId walletId,
        string ruleName,
        TimeSpan cooldown,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(cooldown);

        return await context.Set<WalletFraudAlert>()
            .AsNoTracking()
            .AnyAsync(a =>
                a.WalletId == walletId
                && a.RuleName == ruleName
                && a.TriggeredAt >= cutoff
                && a.Status == FraudAlertStatus.Open,
                ct);
    }
}