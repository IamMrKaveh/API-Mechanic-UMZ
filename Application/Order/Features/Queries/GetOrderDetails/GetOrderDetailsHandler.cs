using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderDetails;

public class GetOrderDetailsHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetOrderDetailsQuery, ServiceResult<OrderDto>>
{
    private readonly IOrderQueryService _orderQueryService = orderQueryService;

    public async Task<ServiceResult<OrderDto>> Handle(
        GetOrderDetailsQuery request,
        CancellationToken ct)
    {
        var order = await _orderQueryService.GetOrderDetailsAsync(
            request.OrderId,
            request.UserId,
            ct);

        if (order is null)
            return ServiceResult<OrderDto>.NotFound("سفارش یافت نشد.");

        return ServiceResult<OrderDto>.Success(order);
    }
}