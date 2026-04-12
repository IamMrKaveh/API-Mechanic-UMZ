using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using Mapster;
using Presentation.Order.Requests;

namespace Presentation.Order.Mapping;

public sealed class OrderMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAdminOrdersRequest, GetAdminOrdersQuery>();
        config.NewConfig<GetOrderStatisticsRequest, GetOrderStatisticsQuery>();
    }
}