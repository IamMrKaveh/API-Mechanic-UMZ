using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Mapster;
using Presentation.Analytic.Requests;

namespace Presentation.Analytic.Mapping;

public sealed class AnalyticMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetDashboardStatisticsRequest, GetDashboardStatisticsQuery>();
        config.NewConfig<GetSalesChartDataRequest, GetSalesChartDataQuery>();
        config.NewConfig<GetTopSellingProductsRequest, GetTopSellingProductsQuery>();
        config.NewConfig<GetCategoryPerformanceRequest, GetCategoryPerformanceQuery>();
        config.NewConfig<GetRevenueReportRequest, GetRevenueReportQuery>();
    }
}