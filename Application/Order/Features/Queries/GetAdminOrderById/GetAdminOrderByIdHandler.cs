using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Queries.GetAdminOrderById;

public class GetAdminOrderByIdHandler(
    IOrderQueryService orderQueryService)
    : IQueryHandler<GetAdminOrderByIdQuery, AdminOrderDto>
{
    public async Task<ServiceResult<AdminOrderDto>> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var orderResult = await (orderQueryService.GetAdminOrderDetailsAsync(orderId, ct)).OrNotFoundAsync("سفارش یافت نشد.");
        if (orderResult.IsFailure) return orderResult.Error;
        var order = orderResult.Value;

        return ServiceResult<AdminOrderDto>.Success(order);
    }
}