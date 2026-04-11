using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetAdminPayments;

public class GetAdminPaymentsHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetAdminPaymentsQuery, ServiceResult<PaginatedResult<PaymentTransactionDto>>>
{
    public async Task<ServiceResult<PaginatedResult<PaymentTransactionDto>>> Handle(
        GetAdminPaymentsQuery request,
        CancellationToken ct)
    {
        var result = await paymentQueryService.GetPagedAsync(request.Params, ct);
        return ServiceResult<PaginatedResult<PaymentTransactionDto>>.Success(result);
    }
}