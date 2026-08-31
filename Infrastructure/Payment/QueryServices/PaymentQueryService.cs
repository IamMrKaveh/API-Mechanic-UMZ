using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Payment.QueryServices;

public sealed class PaymentQueryService(DBContext context) : IPaymentQueryService
{
    public async Task<PaymentTransactionDto?> GetByAuthorityAsync(
        string authority, CancellationToken ct = default)
    {
        var authorityVo = PaymentAuthority.Create(authority);
        return await context.PaymentTransactions
            .AsNoTracking()
            .Where(t => t.Authority == authorityVo)
            .Select(MapToDto())
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaginatedResult<PaymentTransactionDto>> GetPagedAsync(
        Guid? orderId, Guid? userId, string? status, string? gateway,
        DateTime? fromDate, DateTime? toDate,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.PaymentTransactions.AsNoTracking().AsQueryable();

        if (orderId.HasValue)
        {
            var orderIdVo = OrderId.From(orderId.Value);
            query = query.Where(t => t.OrderId == orderIdVo);
        }

        if (userId.HasValue)
        {
            var userIdVo = Domain.User.ValueObjects.UserId.From(userId.Value);
            query = query.Where(t => t.UserId == userIdVo);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusVo = PaymentStatus.FromString(status);
            query = query.Where(t => t.Status == statusVo);
        }

        if (!string.IsNullOrWhiteSpace(gateway))
        {
            var gatewayVo = PaymentGateway.FromString(gateway);
            query = query.Where(t => t.Gateway == gatewayVo);
        }

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
        var authorityVo = PaymentAuthority.Create(authority);
        var tx = await context.PaymentTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Authority == authorityVo, ct);

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

    private static System.Linq.Expressions.Expression<Func<PaymentTransaction, PaymentTransactionDto>>
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
