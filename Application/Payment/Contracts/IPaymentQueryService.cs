using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentQueryService
{
    Task<PaymentTransactionDto?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default);

    Task<PaginatedResult<PaymentTransactionDto>> GetPagedAsync(
        Guid? orderId,
        Guid? userId,
        string? status,
        string? gateway,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<PaymentTransactionDto>> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<PaymentStatusDto?> GetStatusByAuthorityAsync(
        string authority,
        CancellationToken ct = default);
}