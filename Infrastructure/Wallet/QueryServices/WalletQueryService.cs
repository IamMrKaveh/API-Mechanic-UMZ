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
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var query = context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId);

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

    public async Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        var orderIdString = orderId.Value.ToString();

        return await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId
                        && e.ReferenceId == orderIdString
                        && (e.TransactionType == WalletTransactionType.Debit
                            || e.TransactionType == WalletTransactionType.ReservationConfirmed))
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
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<WalletLedgerEntryDto>> ExportLedgerAsync(
        UserId userId,
        WalletLedgerFilter filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var maxRows = filter.MaxRows > 0 ? filter.MaxRows : 10_000;

        var query = context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId);

        query = ApplyFilter(query, filter);

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .Take(maxRows)
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
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var baseQuery = from w in context.Wallets.AsNoTracking()
                        join u in context.Users.AsNoTracking().IgnoreQueryFilters()
                            on w.OwnerId equals u.Id
                        select new WalletOverviewRow(w, u);

        baseQuery = ApplyOverviewFilter(baseQuery, filter);

        var totalCount = await baseQuery.CountAsync(ct);

        baseQuery = ApplyOverviewSort(baseQuery, filter?.SortBy);

        var rows = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                WalletId = x.Wallet.Id.Value,
                UserId = x.Wallet.OwnerId.Value,
                FirstName = x.User.FullName.FirstName,
                LastName = x.User.FullName.LastName,
                Email = x.User.Email.Value,
                Balance = x.Wallet.Balance.Amount,
                IsActive = x.Wallet.IsActive,
                FreezeReason = x.Wallet.FreezeReason,
                CreatedAt = x.Wallet.CreatedAt,
                UpdatedAt = (DateTime?)x.Wallet.UpdatedAt
            })
            .ToListAsync(ct);

        var walletIds = rows.Select(r => r.WalletId).ToList();

        var reservationTotals = await context.WalletReservations
            .AsNoTracking()
            .Where(r => walletIds.Contains(r.WalletId.Value)
                        && r.Status == WalletReservationStatus.Active)
            .GroupBy(r => r.WalletId.Value)
            .Select(g => new { WalletId = g.Key, Total = g.Sum(x => x.Amount.Amount) })
            .ToDictionaryAsync(x => x.WalletId, x => x.Total, ct);

        var lastActivityDates = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => walletIds.Contains(e.WalletId.Value))
            .GroupBy(e => e.WalletId.Value)
            .Select(g => new { WalletId = g.Key, LastAt = g.Max(x => x.OccurredAt) })
            .ToDictionaryAsync(x => x.WalletId, x => (DateTime?)x.LastAt, ct);

        var items = rows.Select(r =>
        {
            var reserved = reservationTotals.TryGetValue(r.WalletId, out var res) ? res : 0m;
            var available = r.Balance - reserved;
            var fullName = $"{r.FirstName} {r.LastName}".Trim();
            var lastActivity = lastActivityDates.TryGetValue(r.WalletId, out var la) ? la : r.UpdatedAt;

            return new WalletOverviewDto(
                r.WalletId,
                r.UserId,
                string.IsNullOrWhiteSpace(fullName) ? "-" : fullName,
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

    public async Task<WalletStatisticsDto> GetStatisticsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);
        var sevenDaysAgo = todayStart.AddDays(-7);

        var walletAggregates = await context.Wallets
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalBalance = g.Sum(w => w.Balance.Amount),
                TotalCount = g.Count(),
                ActiveCount = g.Count(w => w.IsActive),
                FrozenCount = g.Count(w => !w.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        var totalReserved = await context.WalletReservations
            .AsNoTracking()
            .Where(r => r.Status == WalletReservationStatus.Active)
            .SumAsync(r => (decimal?)r.Amount.Amount, ct) ?? 0m;

        var todayCredit = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OccurredAt >= todayStart
                        && e.OccurredAt < todayEnd
                        && (e.TransactionType == WalletTransactionType.Credit
                            || e.TransactionType == WalletTransactionType.Refund
                            || e.TransactionType == WalletTransactionType.TransferIn))
            .SumAsync(e => (decimal?)e.Amount.Amount, ct) ?? 0m;

        var todayDebit = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OccurredAt >= todayStart
                        && e.OccurredAt < todayEnd
                        && (e.TransactionType == WalletTransactionType.Debit
                            || e.TransactionType == WalletTransactionType.TransferOut
                            || e.TransactionType == WalletTransactionType.ReservationConfirmed))
            .SumAsync(e => (decimal?)e.Amount.Amount, ct) ?? 0m;

        var last7DaysCount = await context.WalletLedgerEntries
            .AsNoTracking()
            .CountAsync(e => e.OccurredAt >= sevenDaysAgo, ct);

        var pendingWithdrawals = await context.WalletWithdrawalRequests
            .AsNoTracking()
            .CountAsync(w => w.Status == WithdrawalStatus.Pending, ct);

        var openFraudAlerts = await context.WalletFraudAlerts
            .AsNoTracking()
            .CountAsync(a => a.Status == FraudAlertStatus.Open, ct);

        var totalBalance = walletAggregates?.TotalBalance ?? 0m;
        var totalCount = walletAggregates?.TotalCount ?? 0;
        var activeCount = walletAggregates?.ActiveCount ?? 0;
        var frozenCount = walletAggregates?.FrozenCount ?? 0;

        return new WalletStatisticsDto(
            totalBalance,
            totalReserved,
            totalBalance - totalReserved,
            activeCount,
            frozenCount,
            totalCount,
            todayCredit,
            todayDebit,
            last7DaysCount,
            pendingWithdrawals,
            openFraudAlerts,
            now);
    }

    private static IQueryable<Domain.Wallet.Entities.WalletLedgerEntry> ApplyFilter(
        IQueryable<Domain.Wallet.Entities.WalletLedgerEntry> query,
        WalletLedgerFilter? filter)
    {
        if (filter is null) return query;

        if (filter.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(e => e.OccurredAt >= from);
        }

        if (filter.ToDate.HasValue)
        {
            var to = DateTime.SpecifyKind(filter.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(e => e.OccurredAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType)
            && Enum.TryParse<WalletTransactionType>(filter.TransactionType, ignoreCase: true, out var parsedType))
        {
            query = query.Where(e => e.TransactionType == parsedType);
        }

        if (filter.MinAmount.HasValue)
        {
            var min = filter.MinAmount.Value;
            query = query.Where(e => e.Amount.Amount >= min);
        }

        if (filter.MaxAmount.HasValue)
        {
            var max = filter.MaxAmount.Value;
            query = query.Where(e => e.Amount.Amount <= max);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(e =>
                (e.Description != null && EF.Functions.ILike(e.Description, $"%{term}%"))
                || (e.ReferenceId != null && EF.Functions.ILike(e.ReferenceId, $"%{term}%")));
        }

        return query;
    }

    private static IQueryable<WalletOverviewRow> ApplyOverviewFilter(IQueryable<WalletOverviewRow> query, WalletOverviewFilter? filter)
    {
        if (filter is null) return query;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.User.FullName.FirstName, $"%{term}%")
                || EF.Functions.ILike(x.User.FullName.LastName, $"%{term}%")
                || EF.Functions.ILike(x.User.Email.Value, $"%{term}%"));
        }

        if (filter.IsFrozen.HasValue)
        {
            var frozen = filter.IsFrozen.Value;
            query = query.Where(x => x.Wallet.IsActive == !frozen);
        }

        if (filter.MinBalance.HasValue)
        {
            var min = filter.MinBalance.Value;
            query = query.Where(x => x.Wallet.Balance.Amount >= min);
        }

        if (filter.MaxBalance.HasValue)
        {
            var max = filter.MaxBalance.Value;
            query = query.Where(x => x.Wallet.Balance.Amount <= max);
        }

        if (filter.CreatedFrom.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.CreatedFrom.Value, DateTimeKind.Utc);
            query = query.Where(x => x.Wallet.CreatedAt >= from);
        }

        if (filter.CreatedTo.HasValue)
        {
            var to = DateTime.SpecifyKind(filter.CreatedTo.Value, DateTimeKind.Utc);
            query = query.Where(x => x.Wallet.CreatedAt <= to);
        }

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

    private sealed record WalletOverviewRow(Domain.Wallet.Aggregates.Wallet Wallet, Domain.User.Aggregates.User User);
}