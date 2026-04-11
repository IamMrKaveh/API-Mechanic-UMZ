using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetAdminOrderById;

public class GetAdminOrderByIdHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetAdminOrderByIdQuery, ServiceResult<AdminOrderDto>>
{
    public async Task<ServiceResult<AdminOrderDto>> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken ct)
    {
        var order = await orderQueryService.GetAdminOrderDetailsAsync(
            request.OrderId,
            ct);

        if (order.HasValue)
            return ServiceResult<AdminOrderDto>.NotFound("سفارش یافت نشد.");

        return ServiceResult<AdminOrderDto>.Success(order);
    }
}