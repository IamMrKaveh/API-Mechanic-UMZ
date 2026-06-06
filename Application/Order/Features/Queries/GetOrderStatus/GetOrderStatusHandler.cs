using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetOrderStatus;

public class GetOrderStatusHandler(
    IOrderQueryService orderQueryService,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetOrderStatusQuery, ServiceResult<OrderDto>>
{
    public async Task<ServiceResult<OrderDto>> Handle(
        GetOrderStatusQuery request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(currentUserService.UserId.Value);

        var order = await orderQueryService.GetOrderDetailsAsync(orderId, userId, ct);

        if (order is null)
            return ServiceResult<OrderDto>.NotFound("سفارش یافت نشد.");

        return ServiceResult<OrderDto>.Success(order);
    }
}