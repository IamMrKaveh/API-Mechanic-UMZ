using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletDebitRequestQueryService(DBContext context) : IWalletDebitRequestQueryService
{
    public async Task<PaginatedResult<AdminDebitRequestListItemDto>> GetPageAsync(
        int page,
        int pageSize,
        WalletDebitRequestFilter? filter,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = context.Set<WalletDebitRequest>().AsNoTracking().IgnoreQueryFilters();

        if (filter is not null)
        {
            if (filter.OwnerId.HasValue && filter.OwnerId.Value != Guid.Empty)
            {
                var uid = UserId.From(filter.OwnerId.Value);
                query = query.Where(r => r.OwnerId == uid);
            }
            if (filter.RequestedBy.HasValue && filter.RequestedBy.Value != Guid.Empty)
            {
                var rby = UserId.From(filter.RequestedBy.Value);
                query = query.Where(r => r.RequestedBy == rby);
            }
            if (!string.IsNullOrWhiteSpace(filter.Status)
                && Enum.TryParse<WalletDebitRequestStatus>(filter.Status, true, out var st))
                query = query.Where(r => r.Status == st);
            if (filter.FromDate.HasValue)
            {
                var from = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
                query = query.Where(r => r.CreatedAt >= from);
            }
            if (filter.ToDate.HasValue)
            {
                var to = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
                query = query.Where(r => r.CreatedAt <= to);
            }
        }

        var totalCount = await query.CountAsync(ct);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        if (requests.Count == 0)
            return PaginatedResult<AdminDebitRequestListItemDto>.Create([], totalCount, page, pageSize);

        var userIds = requests.SelectMany(r => new[] { r.OwnerId, r.RequestedBy }).Distinct().ToList();

        var userNames = await context.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, FullName = $"{u.FullName.FirstName} {u.FullName.LastName}" })
            .ToDictionaryAsync(x => x.Id.Value, x => x.FullName, ct);

        var items = requests.Select(r => new AdminDebitRequestListItemDto(
            r.Id.Value,
            r.WalletId.Value,
            r.OwnerId.Value,
            userNames.TryGetValue(r.OwnerId.Value, out var on) ? on : null,
            r.Amount.Amount,
            r.Reason,
            r.Description,
            r.RequestedBy.Value,
            userNames.TryGetValue(r.RequestedBy.Value, out var rn) ? rn : null,
            r.Status.ToString(),
            r.RejectionReason,
            r.CreatedAt,
            r.ExpiresAt,
            r.RespondedAt)).ToList();

        return PaginatedResult<AdminDebitRequestListItemDto>.Create(items, totalCount, page, pageSize);
    }
}
