using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetAdminOrders;

public class GetAdminOrdersHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetAdminOrdersQuery, ServiceResult<PaginatedResult<AdminOrderDto>>>
{
    private readonly IOrderQueryService _orderQueryService = orderQueryService;

    public async Task<ServiceResult<PaginatedResult<AdminOrderDto>>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken ct)
    {
        var result = await _orderQueryService.GetAdminOrdersAsync(
            request.UserId,
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