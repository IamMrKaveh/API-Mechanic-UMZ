using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletWithdrawalRepository(DBContext context) : IWalletWithdrawalRepository
{
    public async Task AddAsync(WalletWithdrawalRequest withdrawal, CancellationToken ct = default)
        => await context.Set<WalletWithdrawalRequest>().AddAsync(withdrawal, ct);

    public void Update(WalletWithdrawalRequest withdrawal)
    {
        var entry = context.Entry(withdrawal);
        if (entry.State == EntityState.Detached)
            context.Set<WalletWithdrawalRequest>().Attach(withdrawal);
        entry.State = EntityState.Modified;
    }

    public async Task<WalletWithdrawalRequest?> GetByIdAsync(
        WalletWithdrawalRequestId id,
        CancellationToken ct = default)
        => await context.Set<WalletWithdrawalRequest>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<WalletWithdrawalRequest?> GetByIdForUpdateAsync(
        WalletWithdrawalRequestId id,
        CancellationToken ct = default)
    {
        var withdrawal = await context.Set<WalletWithdrawalRequest>()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (withdrawal is not null)
        {
            var entry = context.Entry(withdrawal);
            entry.Property("xmin").IsModified = false;
            entry.OriginalValues["xmin"] = entry.CurrentValues["xmin"];
        }

        return withdrawal;
    }

    public async Task<int> CountByUserAndStatusAsync(
        UserId userId,
        WithdrawalStatus status,
        CancellationToken ct = default)
        => await context.Set<WalletWithdrawalRequest>()
            .CountAsync(x => x.UserId == userId && x.Status == status, ct);
}