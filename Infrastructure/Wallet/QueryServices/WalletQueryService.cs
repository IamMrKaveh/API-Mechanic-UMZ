using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

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
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);

        var dtos = entries.Select(e => new WalletLedgerEntryDto(
            Id: e.Id.Value,
            WalletId: e.WalletId.Value,
            UserId: e.OwnerId.Value,
            AmountDelta: e.Amount.Amount,
            BalanceAfter: e.BalanceAfter.Amount,
            TransactionType: e.TransactionType.ToString(),
            ReferenceType: string.Empty,
            ReferenceId: Guid.TryParse(e.ReferenceId, out var refGuid) ? refGuid : Guid.Empty,
            Description: e.Description,
            CreatedAt: e.OccurredAt
        )).ToList();

        return PaginatedResult<WalletLedgerEntryDto>.Create(dtos, dtos.Count, 1, dtos.Count);
    }

    public async Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        var entry = await context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.OwnerId == userId
                        && e.ReferenceId == orderId.Value.ToString()
                        && (e.TransactionType == WalletTransactionType.Debit
                            || e.TransactionType == WalletTransactionType.ReservationConfirmed))
            .FirstOrDefaultAsync(ct);

        if (entry is null) return null;

        return new WalletLedgerEntryDto(
            Id: entry.Id.Value,
            WalletId: entry.WalletId.Value,
            UserId: entry.OwnerId.Value,
            AmountDelta: entry.Amount.Amount,
            BalanceAfter: entry.BalanceAfter.Amount,
            TransactionType: entry.TransactionType.ToString(),
            ReferenceType: string.Empty,
            ReferenceId: Guid.TryParse(entry.ReferenceId, out var refGuid) ? refGuid : Guid.Empty,
            Description: entry.Description,
            CreatedAt: entry.OccurredAt);
    }
}