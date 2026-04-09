using Application.Payment.Contracts;
using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public class GetPaymentsByOrderHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetPaymentsByOrderQuery, ServiceResult<IEnumerable<PaymentTransactionDto>>>
{
    private readonly IPaymentQueryService _paymentQueryService = paymentQueryService;

    public async Task<ServiceResult<IEnumerable<PaymentTransactionDto>>> Handle(
        GetPaymentsByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var dtos = await _paymentQueryService.GetByOrderIdAsync(request.OrderId, cancellationToken);
        return ServiceResult<IEnumerable<PaymentTransactionDto>>.Success(dtos);
    }
}