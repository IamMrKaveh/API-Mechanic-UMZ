using Application.Order.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetUserOrders;

public class GetUserOrdersHandler(
    IOrderQueryService orderQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUserOrdersQuery, PaginatedResult<OrderListItemDto>>
{
    public async Task<ServiceResult<PaginatedResult<OrderListItemDto>>> Handle(
        GetUserOrdersQuery request,
        CancellationToken ct)
    {
        if (!currentUserService.UserId.HasValue)
            return ServiceResult<PaginatedResult<OrderListItemDto>>.Unauthorized("کاربر احراز هویت نشده است.");

        var userId = UserId.From(currentUserService.UserId.Value);

        var result = await orderQueryService.GetUserOrdersAsync(
            userId,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<OrderListItemDto>>.Success(result);
    }
}