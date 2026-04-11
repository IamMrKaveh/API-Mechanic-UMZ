namespace Application.Order.Features.Queries.GetOrderStatistics;

public class GetOrderStatisticsHandler(IOrderQueryService orderQueryService) : IRequestHandler<GetOrderStatisticsQuery, ServiceResult<OrderStatisticsDto>>
{
    public async Task<ServiceResult<OrderStatisticsDto>> Handle(
        GetOrderStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var statistics = await orderQueryService.GetOrderStatisticsAsync(
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return ServiceResult<OrderStatisticsDto>.Success(statistics);
    }
}