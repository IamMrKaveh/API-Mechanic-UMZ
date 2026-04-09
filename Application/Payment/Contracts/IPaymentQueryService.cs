using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Contracts;

public interface IPaymentQueryService
{
    Task<PaymentTransactionDto?> GetTransactionByIdAsync(
        PaymentTransactionId paymentTransactionId,
        CancellationToken ct = default);

    Task<PaymentTransactionDto?> GetByAuthorityAsync(
        string authority,
        CancellationToken ct = default);

    Task<PaymentTransactionDto?> GetLatestByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<PaginatedResult<PaymentTransactionDto>> GetAllAsync(
        UserId? userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<PaymentTransactionDto>> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);
}