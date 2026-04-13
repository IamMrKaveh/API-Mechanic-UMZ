using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetOrderDetails;

public class GetOrderDetailsHandler(IOrderQueryService orderQueryService)
    : IRequestHandler<GetOrderDetailsQuery, ServiceResult<OrderDto>>
{
    public async Task<ServiceResult<OrderDto>> Handle(
        GetOrderDetailsQuery request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(request.UserId);

        var order = await orderQueryService.GetOrderDetailsAsync(orderId, userId, ct);

        if (order is null)
            return ServiceResult<OrderDto>.NotFound("سفارش یافت نشد.");

        return ServiceResult<OrderDto>.Success(order);
    }
}