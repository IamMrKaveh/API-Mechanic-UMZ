using Application.Common.Models;

namespace Application.Payment.Contracts;

public interface IPaymentQueryService
{
    Task<PaginatedResult<PaymentTransactionDto>> GetPagedAsync(
        PaymentSearchParams searchParams,
        CancellationToken ct = default);

    Task<PaymentTransactionDto?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default);

    Task<IEnumerable<PaymentTransactionDto>> GetByOrderIdAsync(
        int orderId,
        CancellationToken ct = default);

    Task<PaymentStatusDto?> GetStatusByAuthorityAsync(
        string authority,
        CancellationToken ct = default);

    Task<PaymentStatisticsDto> GetStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default);
}