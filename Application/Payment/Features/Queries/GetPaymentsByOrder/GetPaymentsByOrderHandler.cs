using Application.Common.Results;

namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public class GetPaymentsByOrderHandler
    : IRequestHandler<GetPaymentsByOrderQuery, ServiceResult<IEnumerable<PaymentTransactionDto>>>
{
    private readonly IPaymentQueryService _paymentQueryService;

    public GetPaymentsByOrderHandler(IPaymentQueryService paymentQueryService)
    {
        _paymentQueryService = paymentQueryService;
    }

    public async Task<ServiceResult<IEnumerable<PaymentTransactionDto>>> Handle(
        GetPaymentsByOrderQuery request,
        CancellationToken cancellationToken)
    {
        var dtos = await _paymentQueryService.GetByOrderIdAsync(request.OrderId, cancellationToken);
        return ServiceResult<IEnumerable<PaymentTransactionDto>>.Success(dtos);
    }
}