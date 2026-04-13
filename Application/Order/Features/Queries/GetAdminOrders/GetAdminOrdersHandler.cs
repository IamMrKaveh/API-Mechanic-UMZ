using Application.Order.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Queries.GetAdminOrders;

public class GetAdminOrdersHandler(IOrderQueryService orderQueryService)
    : IRequestHandler<GetAdminOrdersQuery, ServiceResult<PaginatedResult<AdminOrderDto>>>
{
    public async Task<ServiceResult<PaginatedResult<AdminOrderDto>>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
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