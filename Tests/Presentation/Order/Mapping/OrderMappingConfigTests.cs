using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using Mapster;
using Presentation.Order.Mapping;
using Presentation.Order.Requests;

namespace Tests.Presentation.Order.Mapping;

public class OrderMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly OrderMappingConfig _sut = new();

    public OrderMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetAdminOrdersRequest_MapsToQuery()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new GetAdminOrdersRequest(userId, "Pending", true, from, to, 2, 25);

        // Act
        var query = request.Adapt<GetAdminOrdersQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Status.ShouldBe(request.Status);
        query.FromDate.ShouldBe(request.FromDate);
        query.ToDate.ShouldBe(request.ToDate);
        query.IsPaid.ShouldBe(request.IsPaid);
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
    }

    [Fact]
    public void GetAdminOrdersRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new GetAdminOrdersRequest();

        // Act
        var query = request.Adapt<GetAdminOrdersQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Status.ShouldBeNull();
        query.FromDate.ShouldBeNull();
        query.ToDate.ShouldBeNull();
        query.IsPaid.ShouldBeNull();
        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(10);
    }

    [Fact]
    public void GetOrderStatisticsRequest_MapsToQuery()
    {
        // Arrange
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new GetOrderStatisticsRequest(from, to);

        // Act
        var query = request.Adapt<GetOrderStatisticsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
    }

    [Fact]
    public void OrderMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
