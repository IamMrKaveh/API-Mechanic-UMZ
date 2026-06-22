using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatistics;

public record GetOrderStatisticsQuery
    : IQuery<OrderStatisticsDto>;