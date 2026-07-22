using Application.Payment.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Features.Queries.GetPaymentsByOrder;

public class GetPaymentsByOrderHandler(
    IPaymentQueryService paymentQueryService,
    IOrderRepository orderRepository,
    ICurrentUserService currentUser)
    : IQueryHandler<GetPaymentsByOrderQuery, IEnumerable<PaymentTransactionDto>>
{
    public async Task<ServiceResult<IEnumerable<PaymentTransactionDto>>> Handle(
        GetPaymentsByOrderQuery request,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return ServiceResult<IEnumerable<PaymentTransactionDto>>.Unauthorized("کاربر احراز هویت نشده است.");

        var orderId = OrderId.From(request.OrderId);
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<IEnumerable<PaymentTransactionDto>>.NotFound("سفارش یافت نشد.");

        var userId = UserId.From(currentUser.UserId.Value);
        if (!currentUser.IsAdmin && order.UserId != userId)
            return ServiceResult<IEnumerable<PaymentTransactionDto>>.Forbidden("دسترسی ممنوع.");

        var dtos = await paymentQueryService.GetByOrderIdAsync(orderId, ct);
        return ServiceResult<IEnumerable<PaymentTransactionDto>>.Success(dtos);
    }
}
