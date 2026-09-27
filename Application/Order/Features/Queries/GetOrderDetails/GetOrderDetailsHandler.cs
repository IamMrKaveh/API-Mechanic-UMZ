using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetOrderDetails;

public class GetOrderDetailsHandler(
    IOrderQueryService orderQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetOrderDetailsQuery, OrderDto>
{
    public async Task<ServiceResult<OrderDto>> Handle(
        GetOrderDetailsQuery request,
        CancellationToken ct)
    {
        if (!currentUserService.UserId.HasValue)
            return ServiceResult<OrderDto>.Unauthorized("کاربر احراز هویت نشده است.");

        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(currentUserService.UserId.Value);

        var orderResult = await (orderQueryService.GetOrderDetailsAsync(orderId, userId, ct)).OrNotFoundAsync("سفارش یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        return ServiceResult<OrderDto>.Success(order);
    }
}