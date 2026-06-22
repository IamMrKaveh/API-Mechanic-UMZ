using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetAdminPayments;

public class GetAdminPaymentsHandler(
    IPaymentQueryService paymentQueryService)
    : IQueryHandler<GetAdminPaymentsQuery, PaginatedResult<PaymentTransactionDto>>
{
    public async Task<ServiceResult<PaginatedResult<PaymentTransactionDto>>> Handle(
        GetAdminPaymentsQuery request,
        CancellationToken ct)
    {
        var result = await paymentQueryService.GetPagedAsync(
            request.OrderId,
            request.UserId,
            request.Status,
            request.Gateway,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<PaymentTransactionDto>>.Success(result);
    }
}