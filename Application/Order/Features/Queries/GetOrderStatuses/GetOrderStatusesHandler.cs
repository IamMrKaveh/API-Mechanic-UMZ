using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatuses;

public class GetOrderStatusesHandler(
    IOrderStatusQueryService orderStatusQueryService)
    : IQueryHandler<GetOrderStatusesQuery, IReadOnlyList<OrderStatusDto>>
{
    public async Task<ServiceResult<IReadOnlyList<OrderStatusDto>>> Handle(
        GetOrderStatusesQuery request,
        CancellationToken ct)
    {
        var statuses = await orderStatusQueryService.GetAllAsync(request.OnlyActive, ct);
        return ServiceResult<IReadOnlyList<OrderStatusDto>>.Success(statuses);
    }
}