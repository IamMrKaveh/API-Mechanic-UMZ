using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatistics;

public class GetOrderStatisticsHandler(IOrderQueryService orderQueryService)
    : IRequestHandler<GetOrderStatisticsQuery, ServiceResult<OrderStatisticsDto>>
{
    public async Task<ServiceResult<OrderStatisticsDto>> Handle(
        GetOrderStatisticsQuery request,
        CancellationToken ct)
    {
        var statistics = await orderQueryService.GetOrderStatisticsAsync(ct);
        return ServiceResult<OrderStatisticsDto>.Success(statistics);
    }
}