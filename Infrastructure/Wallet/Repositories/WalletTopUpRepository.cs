using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletTopUpRepository(DBContext context) : IWalletTopUpRepository
{
    public async Task AddAsync(WalletTopUp topUp, CancellationToken ct = default)
        => await context.Set<WalletTopUp>().AddAsync(topUp, ct);

    public void Update(WalletTopUp topUp)
    {
        var entry = context.Entry(topUp);
        if (entry.State == EntityState.Detached)
            context.Set<WalletTopUp>().Attach(topUp);
        entry.State = EntityState.Modified;
    }

    public async Task<WalletTopUp?> GetByIdAsync(WalletTopUpId id, CancellationToken ct = default)
        => await context.Set<WalletTopUp>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<WalletTopUp?> GetByAuthorityAsync(string authority, CancellationToken ct = default)
        => await context.Set<WalletTopUp>().FirstOrDefaultAsync(x => x.GatewayAuthority == authority, ct);

    public async Task<IReadOnlyList<WalletTopUp>> GetPendingOlderThanAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct = default)
        => await context.Set<WalletTopUp>()
            .Where(x => x.Status == WalletTopUpStatus.Pending && x.CreatedAt < cutoffUtc)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WalletTopUp>> GetByUserIdAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        return await context.Set<WalletTopUp>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}