using Application.Common.Results;

namespace Application.Payment.Features.Queries.GetAdminPayments;

public class GetAdminPaymentsHandler
    : IRequestHandler<GetAdminPaymentsQuery, ServiceResult<PaginatedResult<PaymentTransactionDto>>>
{
    private readonly IPaymentQueryService _paymentQueryService;

    public GetAdminPaymentsHandler(IPaymentQueryService paymentQueryService)
    {
        _paymentQueryService = paymentQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<PaymentTransactionDto>>> Handle(
        GetAdminPaymentsQuery request,
        CancellationToken ct)
    {
        var result = await _paymentQueryService.GetPagedAsync(request.Params, ct);
        return ServiceResult<PaginatedResult<PaymentTransactionDto>>.Success(result);
    }
}