using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Order.Features.Queries.GetAdminOrderById;

public class GetAdminOrderByIdHandler(IOrderQueryService orderQueryService)
    : IRequestHandler<GetAdminOrderByIdQuery, ServiceResult<AdminOrderDto>>
{
    public async Task<ServiceResult<AdminOrderDto>> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken ct)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await orderQueryService.GetAdminOrderDetailsAsync(orderId, ct);

        if (order is null)
            return ServiceResult<AdminOrderDto>.NotFound("سفارش یافت نشد.");

        return ServiceResult<AdminOrderDto>.Success(order);
    }
}