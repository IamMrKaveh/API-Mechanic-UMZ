namespace Application.Order.Features.Queries.GetOrderStatuses;

public class GetOrderStatusesHandler(IOrderStatusQueryService orderStatusQueryService)
        : IRequestHandler<GetOrderStatusesQuery, ServiceResult<IEnumerable<OrderStatusDto>>>
{
    public async Task<ServiceResult<IEnumerable<OrderStatusDto>>> Handle(
        GetOrderStatusesQuery request,
        CancellationToken ct)
    {
        var statuses = await orderStatusQueryService.GetAllAsync(ct);
        return ServiceResult<IEnumerable<OrderStatusDto>>.Success(statuses);
    }
}