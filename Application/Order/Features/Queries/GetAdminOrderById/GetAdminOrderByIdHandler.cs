using Application.Common.Results;
using Application.Order.Contracts;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetAdminOrderById;

public class GetAdminOrderByIdHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetAdminOrderByIdQuery, ServiceResult<AdminOrderDto>>
{
    private readonly IOrderQueryService _orderQueryService = orderQueryService;

    public async Task<ServiceResult<AdminOrderDto>> Handle(
        GetAdminOrderByIdQuery request,
        CancellationToken ct)
    {
        var order = await _orderQueryService.GetAdminOrderDetailsAsync(
            request.OrderId,
            ct);

        if (order.HasValue)
            return ServiceResult<AdminOrderDto>.NotFound("سفارش یافت نشد.");

        return ServiceResult<AdminOrderDto>.Success(order);
    }
}