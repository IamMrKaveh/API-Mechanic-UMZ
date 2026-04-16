using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Payment.QueryServices;

public sealed class PaymentQueryService(DBContext context) : IPaymentQueryService
{
    public async Task<PaymentTransactionDto?> GetTransactionByIdAsync(
        PaymentTransactionId paymentTransactionId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.Id == paymentTransactionId)
            .Select(MapToDto())
            .FirstOrDefaultAsync(ct);

    public async Task<PaymentTransactionDto?> GetByAuthorityAsync(
        string authority, CancellationToken ct = default)
        => await context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.Authority.Value == authority)
            .Select(MapToDto())
            .FirstOrDefaultAsync(ct);

    public async Task<PaymentTransactionDto?> GetLatestByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
        => await context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(MapToDto())
            .FirstOrDefaultAsync(ct);

    public async Task<PaginatedResult<PaymentTransactionDto>> GetAllAsync(
        UserId? userId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.PaymentTransactions.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(t => t.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status.Value == status);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto())
            .ToListAsync(ct);

        return PaginatedResult<PaymentTransactionDto>.Create(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<PaymentTransactionDto>> GetPagedAsync(
        Guid? orderId, Guid? userId, string? status, string? gateway,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.PaymentTransactions.AsNoTracking().AsQueryable();

        if (orderId.HasValue)
            query = query.Where(t => t.OrderId.Value == orderId.Value);
        if (userId.HasValue)
            query = query.Where(t => t.UserId.Value == userId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status.Value == status);
        if (!string.IsNullOrWhiteSpace(gateway))
            query = query.Where(t => t.Gateway.Value == gateway);
        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto())
            .ToListAsync(ct);

        return PaginatedResult<PaymentTransactionDto>.Create(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<PaymentTransactionDto>> GetByOrderIdAsync(
        OrderId orderId, CancellationToken ct = default)
    {
        var items = await context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(MapToDto())
            .ToListAsync(ct);
        return items.AsReadOnly();
    }

    public async Task<PaymentStatusDto?> GetStatusByAuthorityAsync(
        string authority, CancellationToken ct = default)
    {
        var tx = await context.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Authority.Value == authority, ct);

        if (tx is null) return null;

        return new PaymentStatusDto
        {
            Authority = tx.Authority.Value,
            Status = tx.Status.Value,
            IsSuccess = tx.IsSuccessful(),
            RefId = tx.RefId,
            Amount = tx.Amount.Amount
        };
    }

    private static System.Linq.Expressions.Expression<Func<Domain.Payment.Aggregates.PaymentTransaction, PaymentTransactionDto>>
        MapToDto()
        => t => new PaymentTransactionDto
        {
            Id = t.Id.Value,
            OrderId = t.OrderId.Value,
            UserId = t.UserId.Value,
            Authority = t.Authority.Value,
            Gateway = t.Gateway.Value,
            Amount = t.Amount.Amount,
            Status = t.Status.Value,
            RefId = t.RefId,
            VerifiedAt = t.VerifiedAt,
            ExpiresAt = t.ExpiresAt,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
}