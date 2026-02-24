namespace Application.Order.Features.Queries.GetUserOrders;

public class GetUserOrdersHandler : IRequestHandler<GetUserOrdersQuery, ServiceResult<PaginatedResult<OrderDto>>>
{
    private readonly IOrderQueryService _orderQueryService;

    public GetUserOrdersHandler(IOrderQueryService orderQueryService)
    {
        _orderQueryService = orderQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<OrderDto>>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _orderQueryService.GetUserOrdersAsync(
            request.UserId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<OrderDto>>.Success(result);
    }
}