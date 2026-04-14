using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Wallet.QueryServices;

public sealed class WalletQueryService(DBContext context) : IWalletQueryService
{
    public async Task<PaginatedResult<WalletLedgerEntryDto>> GetLedgerPageAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var entries = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new WalletLedgerEntryDto
            {
                Id = e.Id.Value,
                WalletId = e.WalletId.Value,
                UserId = e.OwnerId.Value,
                AmountDelta = e.Amount.Amount,
                BalanceAfter = e.BalanceAfter.Amount,
                TransactionType = e.TransactionType.ToString(),
                Description = e.Description,
                ReferenceId = e.ReferenceId,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<WalletLedgerEntryDto>.Create(entries, entries.Count, 1, entries.Count);
    }

    public async Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId
                        && e.ReferenceId == orderId.Value.ToString()
                        && e.TransactionType == Domain.Wallet.Enums.WalletTransactionType.OrderPayment)
            .Select(e => new WalletLedgerEntryDto
            {
                Id = e.Id.Value,
                WalletId = e.WalletId.Value,
                UserId = e.OwnerId.Value,
                AmountDelta = e.Amount.Amount,
                BalanceAfter = e.BalanceAfter.Amount,
                TransactionType = e.TransactionType.ToString(),
                Description = e.Description,
                ReferenceId = e.ReferenceId,
                CreatedAt = e.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}