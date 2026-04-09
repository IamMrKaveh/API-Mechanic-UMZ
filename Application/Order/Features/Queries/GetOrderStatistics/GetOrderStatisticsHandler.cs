namespace Application.Order.Features.Queries.GetOrderStatistics;

public class GetOrderStatisticsHandler : IRequestHandler<GetOrderStatisticsQuery, ServiceResult<OrderStatisticsDto>>
{
    private readonly IOrderQueryService _orderQueryService;

    public GetOrderStatisticsHandler(IOrderQueryService orderQueryService)
    {
        _orderQueryService = orderQueryService;
    }

    public async Task<ServiceResult<OrderStatisticsDto>> Handle(
        GetOrderStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var statistics = await _orderQueryService.GetOrderStatisticsAsync(
            request.FromDate,
            request.ToDate,
            cancellationToken);

        return ServiceResult<OrderStatisticsDto>.Success(statistics);
    }
}