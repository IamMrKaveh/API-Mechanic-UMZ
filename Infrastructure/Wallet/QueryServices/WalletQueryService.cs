using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletQueryService(DBContext context) : IWalletQueryService
{
    private const string AdminAdjustmentDescriptionPrefix = "[ADMIN-";

    public async Task<PaginatedResult<WalletLedgerEntryDto>> GetLedgerPageAsync(
        UserId userId,
        int page,
        int pageSize,
        WalletLedgerFilter? filter = null,
        bool includeInactiveUsers = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var query = context.WalletLedgerEntries.AsNoTracking();

        if (includeInactiveUsers)
            query = query.IgnoreQueryFilters();

        query = query.Where(e => e.OwnerId == userId);
        query = ApplyFilter(query, filter);

        var totalCount = await query.CountAsync(ct);

        var dtos = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new WalletLedgerEntryDto(
                e.Id.Value,
                e.WalletId.Value,
                e.OwnerId.Value,
                e.Amount.Amount,
                e.BalanceAfter.Amount,
                e.TransactionType.ToString(),
                string.Empty,
                Guid.Empty,
                e.Description,
                e.OccurredAt,
                e.Description != null && e.Description.StartsWith(AdminAdjustmentDescriptionPrefix)
            ))
            .ToListAsync(ct);

        return PaginatedResult<WalletLedgerEntryDto>.Create(dtos, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<WalletLedgerEntryDto>> ExportLedgerAsync(
        UserId userId,
        WalletLedgerFilter filter,
        bool includeInactiveUsers = false,
        CancellationToken ct = default)
    {
        var query = context.WalletLedgerEntries.AsNoTracking();

        if (includeInactiveUsers)
            query = query.IgnoreQueryFilters();

        query = query.Where(e => e.OwnerId == userId);
        query = ApplyFilter(query, filter);

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new WalletLedgerEntryDto(
                e.Id.Value,
                e.WalletId.Value,
                e.OwnerId.Value,
                e.Amount.Amount,
                e.BalanceAfter.Amount,
                e.TransactionType.ToString(),
                string.Empty,
                Guid.Empty,
                e.Description,
                e.OccurredAt,
                e.Description != null && e.Description.StartsWith(AdminAdjustmentDescriptionPrefix)
            ))
            .ToListAsync(ct);
    }

    public async Task<PaginatedResult<WalletOverviewDto>> GetOverviewPageAsync(
        int page,
        int pageSize,
        WalletOverviewFilter? filter = null,
        bool includeInactiveUsers = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var walletsBase = context.Wallets.AsNoTracking();
        var usersBase = context.Users.AsNoTracking();

        if (includeInactiveUsers)
        {
            walletsBase = walletsBase.IgnoreQueryFilters();
            usersBase = usersBase.IgnoreQueryFilters();
        }

        var baseQuery =
            from w in walletsBase
            join u in usersBase on w.OwnerId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            select new WalletOverviewRow
            {
                Wallet = w,
                User = u
            };

        baseQuery = ApplyOverviewFilter(baseQuery, filter);

        var totalCount = await baseQuery.CountAsync(ct);

        baseQuery = ApplyOverviewSort(baseQuery, filter?.SortBy);

        var rows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                WalletId = x.Wallet.Id,
                UserId = x.Wallet.OwnerId,
                FirstName = x.User != null && x.User.FullName != null ? x.User.FullName.FirstName : null,
                LastName = x.User != null && x.User.FullName != null ? x.User.FullName.LastName : null,
                Email = x.User != null && x.User.Email != null ? x.User.Email.Value : null,
                IsUserActive = x.User != null && x.User.IsActive,
                Balance = x.Wallet.Balance.Amount,
                x.Wallet.IsActive,
                x.Wallet.FreezeReason,
                x.Wallet.CreatedAt,
                UpdatedAt = (DateTime?)x.Wallet.UpdatedAt
            })
            .ToListAsync(ct);

        if (rows.Count == 0)
            return PaginatedResult<WalletOverviewDto>.Create([], totalCount, page, pageSize);

        var walletIds = rows.Select(r => r.WalletId).ToList();

        var reservationTotals = await context.WalletReservations
            .AsNoTracking()
            .Where(r => walletIds.Contains(r.WalletId) && r.Status == WalletReservationStatus.Active)
            .GroupBy(r => r.WalletId)
            .Select(g => new { WalletId = g.Key, Total = g.Sum(x => x.Amount.Amount) })
            .ToDictionaryAsync(x => x.WalletId.Value, x => x.Total, ct);

        var lastActivityDates = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => walletIds.Contains(e.WalletId))
            .GroupBy(e => e.WalletId)
            .Select(g => new { WalletId = g.Key, LastAt = g.Max(x => x.OccurredAt) })
            .ToDictionaryAsync(x => x.WalletId.Value, x => (DateTime?)x.LastAt, ct);

        var items = rows.Select(r =>
        {
            var reserved = reservationTotals.TryGetValue(r.WalletId.Value, out var res) ? res : 0m;
            var available = r.Balance - reserved;
            var first = r.FirstName ?? string.Empty;
            var last = r.LastName ?? string.Empty;
            var fullName = $"{first} {last}".Trim();
            var displayName = string.IsNullOrWhiteSpace(fullName) ? "کاربر حذف‌شده" : fullName;
            var lastActivity = lastActivityDates.TryGetValue(r.WalletId.Value, out var la) ? la : r.UpdatedAt;

            return new WalletOverviewDto(
                r.WalletId.Value,
                r.UserId.Value,
                displayName,
                r.Email ?? string.Empty,
                r.Balance,
                reserved,
                available,
                r.IsActive,
                r.FreezeReason,
                r.CreatedAt,
                lastActivity);
        }).ToList();

        return PaginatedResult<WalletOverviewDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        var orderRef = orderId.Value.ToString();
        var entry = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId && e.Description != null && e.Description.Contains(orderRef))
            .OrderByDescending(e => e.OccurredAt)
            .FirstOrDefaultAsync(ct);

        if (entry is null) return null;

        return new WalletLedgerEntryDto(
            entry.Id.Value, entry.WalletId.Value, entry.OwnerId.Value,
            entry.Amount.Amount, entry.BalanceAfter.Amount,
            entry.TransactionType.ToString(), string.Empty, Guid.Empty,
            entry.Description, entry.OccurredAt,
            entry.Description != null && entry.Description.StartsWith(AdminAdjustmentDescriptionPrefix));
    }

    public async Task<WalletStatisticsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var last7 = DateTime.UtcNow.AddDays(-7);

        var wallets = await context.Wallets.AsNoTracking().IgnoreQueryFilters()
            .Select(w => new { w.Balance.Amount, w.IsActive })
            .ToListAsync(ct);

        var totalBalance = wallets.Sum(x => x.Amount);
        var activeCount = wallets.Count(x => x.IsActive);
        var frozenCount = wallets.Count(x => !x.IsActive);
        var totalCount = wallets.Count;

        var totalReserved = await context.WalletReservations.AsNoTracking()
            .Where(r => r.Status == WalletReservationStatus.Active)
            .SumAsync(r => (decimal?)r.Amount.Amount, ct) ?? 0m;

        var todayCredit = await context.WalletLedgerEntries.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.OccurredAt >= today && e.TransactionType == WalletTransactionType.Credit)
            .SumAsync(e => (decimal?)e.Amount.Amount, ct) ?? 0m;

        var todayDebit = await context.WalletLedgerEntries.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.OccurredAt >= today && e.TransactionType == WalletTransactionType.Debit)
            .SumAsync(e => (decimal?)e.Amount.Amount, ct) ?? 0m;

        var last7Count = await context.WalletLedgerEntries.AsNoTracking().IgnoreQueryFilters()
            .CountAsync(e => e.OccurredAt >= last7, ct);

        var pendingWithdrawals = await context.WalletWithdrawalRequests.AsNoTracking()
            .CountAsync(w => w.Status == WithdrawalStatus.Pending, ct);

        var openAlerts = await context.Set<Domain.Wallet.Aggregates.WalletFraudAlert>().AsNoTracking()
            .CountAsync(a => a.Status == FraudAlertStatus.Open, ct);

        return new WalletStatisticsDto(
            totalBalance, totalReserved, totalBalance - totalReserved,
            activeCount, frozenCount, totalCount,
            todayCredit, todayDebit, last7Count,
            pendingWithdrawals, openAlerts, DateTime.UtcNow);
    }

    private static IQueryable<Domain.Wallet.Entities.WalletLedgerEntry> ApplyFilter(
        IQueryable<Domain.Wallet.Entities.WalletLedgerEntry> query,
        WalletLedgerFilter? filter)
    {
        if (filter is null) return query;

        if (filter.FromDate.HasValue)
            query = query.Where(e => e.OccurredAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(e => e.OccurredAt <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.TransactionType)
            && Enum.TryParse<WalletTransactionType>(filter.TransactionType, true, out var tt))
            query = query.Where(e => e.TransactionType == tt);
        if (filter.MinAmount.HasValue)
            query = query.Where(e => e.Amount.Amount >= filter.MinAmount.Value);
        if (filter.MaxAmount.HasValue)
            query = query.Where(e => e.Amount.Amount <= filter.MaxAmount.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(e => e.Description != null && e.Description.Contains(term));
        }
        return query;
    }

    private static IQueryable<WalletOverviewRow> ApplyOverviewFilter(
        IQueryable<WalletOverviewRow> query,
        WalletOverviewFilter? filter)
    {
        if (filter is null) return query;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(x =>
                (x.User != null && x.User.FullName != null && x.User.FullName.FirstName.Contains(s))
                || (x.User != null && x.User.FullName != null && x.User.FullName.LastName.Contains(s))
                || (x.User != null && x.User.Email != null && x.User.Email.Value.Contains(s)));
        }
        if (filter.IsFrozen.HasValue)
            query = query.Where(x => x.Wallet.IsActive == !filter.IsFrozen.Value);
        if (filter.MinBalance.HasValue)
            query = query.Where(x => x.Wallet.Balance.Amount >= filter.MinBalance.Value);
        if (filter.MaxBalance.HasValue)
            query = query.Where(x => x.Wallet.Balance.Amount <= filter.MaxBalance.Value);
        if (filter.CreatedFrom.HasValue)
            query = query.Where(x => x.Wallet.CreatedAt >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(x => x.Wallet.CreatedAt <= filter.CreatedTo.Value);

        return query;
    }

    private static IQueryable<WalletOverviewRow> ApplyOverviewSort(IQueryable<WalletOverviewRow> query, string? sortBy)
    {
        var normalized = string.IsNullOrWhiteSpace(sortBy) ? "created_desc" : sortBy.Trim().ToLowerInvariant();
        return normalized switch
        {
            "balance_desc" => query.OrderByDescending(x => x.Wallet.Balance.Amount),
            "balance_asc" => query.OrderBy(x => x.Wallet.Balance.Amount),
            "created_asc" => query.OrderBy(x => x.Wallet.CreatedAt),
            "lastactivity_desc" => query.OrderByDescending(x => x.Wallet.UpdatedAt),
            "lastactivity_asc" => query.OrderBy(x => x.Wallet.UpdatedAt),
            _ => query.OrderByDescending(x => x.Wallet.CreatedAt)
        };
    }

    private sealed class WalletOverviewRow
    {
        public Domain.Wallet.Aggregates.Wallet Wallet { get; init; } = default!;
        public Domain.User.Aggregates.User? User { get; init; }
    }
}
