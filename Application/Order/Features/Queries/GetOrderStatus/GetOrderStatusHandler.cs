using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetOrderStatus;

public class GetOrderStatusHandler(IOrderQueryService orderQueryService)
    : IRequestHandler<GetOrderStatusQuery, ServiceResult<OrderStatusDto>>
{
    public async Task<ServiceResult<OrderStatusDto>> Handle(
        GetOrderStatusQuery request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(request.UserId);

        var status = await orderQueryService.GetOrderDetailsAsync(orderId, userId, ct);

        if (status is null)
            return ServiceResult<OrderStatusDto>.NotFound("وضعیت سفارش یافت نشد.");

        return ServiceResult<OrderStatusDto>.Success(status.Adapt<OrderStatusDto>());
    }
}