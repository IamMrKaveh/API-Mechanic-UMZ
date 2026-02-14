namespace Application.Order.Features.Queries.GetOrderDetails;

public class GetOrderDetailsHandler : IRequestHandler<GetOrderDetailsQuery, ServiceResult<OrderDto>>
{
    private readonly IOrderQueryService _orderQueryService;

    public GetOrderDetailsHandler(IOrderQueryService orderQueryService)
    {
        _orderQueryService = orderQueryService;
    }

    public async Task<ServiceResult<OrderDto>> Handle(
        GetOrderDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orderQueryService.GetOrderDetailsAsync(
            request.OrderId,
            request.UserId,
            cancellationToken);

        if (order == null)
            return ServiceResult<OrderDto>.Failure("سفارش یافت نشد.", 404);

        return ServiceResult<OrderDto>.Success(order);
    }
}