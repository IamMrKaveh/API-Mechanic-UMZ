using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetAdminPayments;

public record GetAdminPaymentsQuery(
    Guid? OrderId,
    Guid? UserId,
    string? Status,
    string? Gateway,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<PaymentTransactionDto>>>;