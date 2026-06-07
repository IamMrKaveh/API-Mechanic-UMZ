using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletQueryService(DBContext context) : IWalletQueryService
{
    public async Task<PaginatedResult<WalletLedgerEntryDto>> GetLedgerPageAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 200) pageSize = 200;

        var query = context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId);

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
                e.OccurredAt
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
                e.OccurredAt
            ))
            .FirstOrDefaultAsync(ct);
    }
}