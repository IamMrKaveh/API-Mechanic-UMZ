using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletTransferQueryService(DBContext context) : IWalletTransferQueryService
{
    public async Task<PaginatedResult<WalletTransferDto>> GetTransfersPageAsync(
        int page,
        int pageSize,
        WalletTransferFilter? filter,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = context.Set<WalletTransfer>().AsNoTracking().IgnoreQueryFilters();

        if (filter is not null)
        {
            if (filter.UserId.HasValue && filter.UserId.Value != Guid.Empty)
            {
                var uid = UserId.From(filter.UserId.Value);
                query = query.Where(t => t.FromUserId == uid || t.ToUserId == uid);
            }
            if (!string.IsNullOrWhiteSpace(filter.Status)
                && Enum.TryParse<WalletTransferStatus>(filter.Status, true, out var st))
                query = query.Where(t => t.Status == st);
            if (filter.FromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
                query = query.Where(t => t.CreatedAt >= from);
            }
            if (filter.ToDate.HasValue)
            {
                var to = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
                query = query.Where(t => t.CreatedAt <= to);
            }
        }

        var totalCount = await query.CountAsync(ct);

        var transfers = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (transfers.Count == 0)
            return PaginatedResult<WalletTransferDto>.Create([], totalCount, page, pageSize);

        var userIds = transfers.SelectMany(t => new[] { t.FromUserId, t.ToUserId }).Distinct().ToList();

        var userNames = await context.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, FullName = $"{u.FullName.FirstName} {u.FullName.LastName}" })
            .ToDictionaryAsync(x => x.Id.Value, x => x.FullName, ct);

        var items = transfers.Select(t => new WalletTransferDto(
            t.Id.Value,
            t.FromUserId.Value,
            userNames.TryGetValue(t.FromUserId.Value, out var fn) ? fn : null,
            t.ToUserId.Value,
            userNames.TryGetValue(t.ToUserId.Value, out var tn) ? tn : null,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Description,
            t.Status.ToString(),
            t.OtpAttempts,
            t.OtpExpiresAt,
            t.CorrelationId,
            t.CreatedAt,
            t.CompletedAt,
            t.CancelledAt,
            t.FailureReason)).ToList();

        return PaginatedResult<WalletTransferDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<WalletTransferDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var transferId = WalletTransferId.From(id);
        var t = await context.Set<WalletTransfer>().AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == transferId, ct);

        if (t is null) return null;

        var userIds = new[] { t.FromUserId, t.ToUserId };
        var userNames = await context.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, FullName = $"{u.FullName.FirstName} {u.FullName.LastName}" })
            .ToDictionaryAsync(x => x.Id.Value, x => x.FullName, ct);

        return new WalletTransferDto(
            t.Id.Value,
            t.FromUserId.Value,
            userNames.TryGetValue(t.FromUserId.Value, out var fn) ? fn : null,
            t.ToUserId.Value,
            userNames.TryGetValue(t.ToUserId.Value, out var tn) ? tn : null,
            t.Amount.Amount,
            t.Amount.Currency,
            t.Description,
            t.Status.ToString(),
            t.OtpAttempts,
            t.OtpExpiresAt,
            t.CorrelationId,
            t.CreatedAt,
            t.CompletedAt,
            t.CancelledAt,
            t.FailureReason);
    }
}
