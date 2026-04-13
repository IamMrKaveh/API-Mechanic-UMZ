using Application.Order.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetUserOrders;

public class GetUserOrdersHandler(IOrderQueryService orderQueryService)
    : IRequestHandler<GetUserOrdersQuery, ServiceResult<PaginatedResult<OrderListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<OrderListItemDto>>> Handle(
        GetUserOrdersQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var result = await orderQueryService.GetUserOrdersAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<OrderListItemDto>>.Success(result);
    }
}