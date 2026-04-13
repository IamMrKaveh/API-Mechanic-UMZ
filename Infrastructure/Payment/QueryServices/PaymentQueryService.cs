using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;
using Infrastructure.Persistence.Context;
using MapsterMapper;

namespace Infrastructure.Payment.QueryServices;

public sealed class PaymentQueryService(DBContext context, IMapper mapper) : IPaymentQueryService
{
    private readonly DBContext _context = context;
    private readonly IMapper _mapper = mapper;

    public async Task<PaginatedResult<PaymentTransactionDto>> GetPagedAsync(
        PaymentSearchParams searchParams,
        CancellationToken ct = default)
    {
        var query = _context.PaymentTransactions
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (searchParams.OrderId.HasValue)
            query = query.Where(t => t.OrderId == searchParams.OrderId.Value);

        if (searchParams.UserId.HasValue)
            query = query.Where(t => t.UserId == searchParams.UserId.Value);

        if (!string.IsNullOrWhiteSpace(searchParams.Status))
            query = query.Where(t => t.Status.Value == searchParams.Status);

        if (searchParams.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= searchParams.FromDate.Value);

        if (searchParams.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= searchParams.ToDate.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(ct);

        var dtos = _mapper.Map<IEnumerable<PaymentTransactionDto>>(items);

        return PaginatedResult<PaymentTransactionDto>.Create(
            [.. dtos], total, searchParams.Page, searchParams.PageSize);
    }

    public async Task<PaymentTransactionDto?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default)
    {
        var transaction = await _context.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);

        return transaction is null ? null : _mapper.Map<PaymentTransactionDto>(transaction);
    }

    public async Task<IEnumerable<PaymentTransactionDto>> GetByOrderIdAsync(
        int orderId,
        CancellationToken ct = default)
    {
        var transactions = await _context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.OrderId == orderId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return _mapper.Map<IEnumerable<PaymentTransactionDto>>(transactions);
    }

    public async Task<PaymentStatusDto?> GetStatusByAuthorityAsync(
        string authority,
        CancellationToken ct = default)
    {
        var transaction = await _context.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);

        return transaction is null ? null : _mapper.Map<PaymentStatusDto>(transaction);
    }

    public async Task<PaymentStatistics> GetStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default)
    {
        var query = _context.PaymentTransactions
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var stats = await query
            .GroupBy(t => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Successful = g.Count(t => t.Status.Value == "Success"),
                Failed = g.Count(t => t.Status.Value == "Failed"),
                Pending = g.Count(t => t.Status.Value == "Pending"),
                Expired = g.Count(t => t.Status.Value == "Expired"),
                Refunded = g.Count(t => t.Status.Value == "Refunded"),
                TotalSuccessAmount = g
                    .Where(t => t.Status.Value == "Success")
                    .Sum(t => t.Amount.Amount),
                TotalRefundedAmount = g
                    .Where(t => t.Status.Value == "Refunded")
                    .Sum(t => t.Amount.Amount),
                TotalFees = g.Sum(t => t.Fee)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
            return PaymentStatistics.Create(0, 0, 0, 0, 0, 0, 0, 0, 0);

        return PaymentStatistics.Create(
            totalTransactions: stats.Total,
            successfulTransactions: stats.Successful,
            failedTransactions: stats.Failed,
            pendingTransactions: stats.Pending,
            expiredTransactions: stats.Expired,
            refundedTransactions: stats.Refunded,
            totalSuccessfulAmount: stats.TotalSuccessAmount,
            totalRefundedAmount: stats.TotalRefundedAmount,
            totalFees: stats.TotalFees);
    }

    public async Task<PaymentTransaction?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<PaymentTransaction?> GetByAuthorityWithOrderAsync(
        string authority,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Include(t => t.Order)
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);
    }

    public async Task<IEnumerable<PaymentTransaction>> GetPendingExpiredTransactionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.Status.Value == "Pending" && t.ExpiresAt < cutoffTime && !t.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<(IEnumerable<PaymentTransaction> Transactions, int TotalCount)> GetPagedAsync(
        int? orderId = null,
        int? userId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _context.PaymentTransactions
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (orderId.HasValue)
            query = query.Where(t => t.OrderId == orderId.Value);

        if (userId.HasValue)
            query = query.Where(t => t.UserId == userId.Value);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status.Value == status);

        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<PaymentTransaction>> GetSuccessfulByOrderIdAsync(
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && t.Status.Value == "Success" && !t.IsDeleted)
            .ToListAsync(ct);
    }

    public async Task<PaymentTransaction?> GetLatestByOrderIdAsync(
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaymentTransaction?> GetVerifiedByOrderIdAsync(
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId && t.Status.Value == "Success" && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaymentTransaction?> GetActiveByOrderIdAsync(
        int orderId,
        CancellationToken ct = default)
    {
        return await _context.PaymentTransactions
            .Where(t => t.OrderId == orderId
                        && (t.Status.Value == "Pending" || t.Status.Value == "Processing")
                        && !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }
}