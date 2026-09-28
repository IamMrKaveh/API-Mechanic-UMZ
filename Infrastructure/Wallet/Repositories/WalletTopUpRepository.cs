using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Interfaces;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.Repositories;

public sealed class WalletTopUpRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<WalletTopUp, WalletTopUpId>(context), IWalletTopUpRepository
{
    public override void Update(WalletTopUp topUp)
    {
        var entry = Context.Entry(topUp);

        if (entry.State == EntityState.Detached)
        {
            var local = Context.Set<WalletTopUp>().Local.FirstOrDefault(e => e.Id == topUp.Id);
            if (local is not null)
            {
                if (!ReferenceEquals(local, topUp))
                {
                    Context.Entry(local).CurrentValues.SetValues(topUp);
                    Context.Entry(local).State = EntityState.Modified;
                }
                else
                {
                    Context.Entry(local).State = EntityState.Modified;
                }
                return;
            }

            var currentXmin = Context.Set<WalletTopUp>()
                .AsNoTracking()
                .Where(x => x.Id == topUp.Id)
                .Select(x => EF.Property<uint>(x, "xmin"))
                .FirstOrDefault();

            Context.Set<WalletTopUp>().Attach(topUp);
            entry = Context.Entry(topUp);
            entry.Property("xmin").OriginalValue = currentXmin;
            entry.State = EntityState.Modified;
            return;
        }

        if (entry.State == EntityState.Unchanged)
            entry.State = EntityState.Modified;
    }

    public async Task<WalletTopUp?> GetByAuthorityAsync(string authority, CancellationToken ct = default)
        => await Context.Set<WalletTopUp>()
            .FirstOrDefaultAsync(x => x.GatewayAuthority == authority, ct);

    public async Task<IReadOnlyList<WalletTopUp>> GetPendingOlderThanAsync(
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken ct = default)
        => await Context.Set<WalletTopUp>()
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
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        return await Context.Set<WalletTopUp>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
