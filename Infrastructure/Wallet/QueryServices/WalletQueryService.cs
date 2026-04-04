using Application.Wallet.Contracts;
using Application.Wallet.Features.Shared;
using Infrastructure.Persistence.Context;
using SharedKernel.Models;

namespace Infrastructure.Wallet.QueryServices;

public class WalletQueryService(DBContext context) : IWalletQueryService
{
    private readonly DBContext _context = context;

    public async Task<PaginatedResult<WalletLedgerEntryDto>> GetLedgerPageAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new WalletLedgerEntryDto
            {
                Id = e.Id,
                WalletId = e.WalletId,
                UserId = e.UserId,
                AmountDelta = e.Amount,
                BalanceAfter = e.BalanceAfter,
                TransactionType = e.TransactionType.ToString(),
                ReferenceType = e.ReferenceType.ToString(),
                ReferenceId = e.ReferenceId,
                IdempotencyKey = e.IdempotencyKey,
                CorrelationId = e.CorrelationId,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<WalletLedgerEntryDto>.Create(items, total, page, pageSize);
    }

    public async Task<WalletLedgerEntryDto?> GetOrderPaymentLedgerEntryAsync(
        int userId,
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.WalletLedgerEntries
            .AsNoTracking()
            .Where(e =>
                e.UserId == userId &&
                e.ReferenceId == orderId &&
                e.ReferenceType == WalletReferenceType.Order &&
                e.TransactionType == WalletTransactionType.OrderPayment)
            .Select(e => new WalletLedgerEntryDto
            {
                Id = e.Id,
                WalletId = e.WalletId,
                UserId = e.UserId,
                AmountDelta = e.AmountDelta,
                BalanceAfter = e.BalanceAfter,
                TransactionType = e.TransactionType.ToString(),
                ReferenceType = e.ReferenceType.ToString(),
                ReferenceId = e.ReferenceId,
                IdempotencyKey = e.IdempotencyKey,
                CorrelationId = e.CorrelationId,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }
}