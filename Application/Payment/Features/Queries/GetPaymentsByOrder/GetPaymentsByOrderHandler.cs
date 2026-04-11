using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public class GetPaymentsByOrderHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetPaymentsByOrderQuery, ServiceResult<IEnumerable<PaymentTransactionDto>>>
{
    public async Task<ServiceResult<IEnumerable<PaymentTransactionDto>>> Handle(
        GetPaymentsByOrderQuery request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var dtos = await paymentQueryService.GetByOrderIdAsync(orderId, ct);
        return ServiceResult<IEnumerable<PaymentTransactionDto>>.Success(dtos);
    }
}