namespace Application.Order.Features.Queries.GetOrderStatistics;

public record GetOrderStatisticsQuery(DateTime? FromDate, DateTime? ToDate) : IRequest<ServiceResult<OrderStatisticsDto>>;