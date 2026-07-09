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
}