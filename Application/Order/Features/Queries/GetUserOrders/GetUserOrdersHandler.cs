using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetUserOrders;

public class GetUserOrdersHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetUserOrdersQuery, ServiceResult<PaginatedResult<OrderDto>>>
{
    public async Task<ServiceResult<PaginatedResult<OrderDto>>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await orderQueryService.GetUserOrdersAsync(
            request.UserId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<OrderDto>>.Success(result);
    }
}