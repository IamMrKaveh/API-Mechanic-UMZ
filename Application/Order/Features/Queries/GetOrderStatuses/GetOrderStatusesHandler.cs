using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatuses;

public class GetOrderStatusesHandler(IOrderStatusQueryService orderStatusQueryService)
    : IRequestHandler<GetOrderStatusesQuery, ServiceResult<IReadOnlyList<OrderStatusDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<OrderStatusDto>>> Handle(
        GetOrderStatusesQuery request,
        CancellationToken ct)
    {
        var statuses = await orderStatusQueryService.GetAllAsync(ct);
        return ServiceResult<IReadOnlyList<OrderStatusDto>>.Success(statuses);
    }
}