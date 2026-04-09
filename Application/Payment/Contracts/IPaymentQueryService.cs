using Application.Payment.Features.Shared;

namespace Application.Payment.Contracts;

public interface IPaymentQueryService
{
    Task<PaymentTransactionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PaymentTransactionDto?> GetByAuthorityAsync(string authority, CancellationToken ct = default);

    Task<PaymentTransactionDto?> GetLatestByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    Task<PaginatedResult<PaymentTransactionDto>> GetAllAsync(
        Guid? userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<PaymentTransactionDto>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}