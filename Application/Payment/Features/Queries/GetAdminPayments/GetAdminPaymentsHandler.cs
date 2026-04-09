using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using SharedKernel.Models;

namespace Application.Payment.Features.Queries.GetAdminPayments;

public class GetAdminPaymentsHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetAdminPaymentsQuery, ServiceResult<PaginatedResult<PaymentTransactionDto>>>
{
    private readonly IPaymentQueryService _paymentQueryService = paymentQueryService;

    public async Task<ServiceResult<PaginatedResult<PaymentTransactionDto>>> Handle(
        GetAdminPaymentsQuery request,
        CancellationToken ct)
    {
        var result = await _paymentQueryService.GetPagedAsync(request.Params, ct);
        return ServiceResult<PaginatedResult<PaymentTransactionDto>>.Success(result);
    }
}