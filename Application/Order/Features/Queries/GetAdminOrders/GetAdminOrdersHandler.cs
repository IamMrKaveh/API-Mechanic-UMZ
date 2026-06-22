using Application.Order.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetAdminOrders;

public class GetAdminOrdersHandler(
    IOrderQueryService orderQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetAdminOrdersQuery, PaginatedResult<AdminOrderDto>>
{
    public async Task<ServiceResult<PaginatedResult<AdminOrderDto>>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);
        var result = await orderQueryService.GetAdminOrdersAsync(
            userId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.IsPaid,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<AdminOrderDto>>.Success(result);
    }
}