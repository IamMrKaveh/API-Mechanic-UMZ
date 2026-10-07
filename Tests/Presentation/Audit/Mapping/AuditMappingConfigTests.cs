using Application.Audit.Features.Queries.GetAuditLogs;
using Application.Audit.Features.Queries.GetAuditStatistics;
using Mapster;
using Presentation.Audit.Mapping;
using Presentation.Audit.Requests;

namespace Tests.Presentation.Audit.Mapping;

public class AuditMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly AuditMappingConfig _sut = new();

    public AuditMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetAuditLogsRequest_MapsToQuery()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new GetAuditLogsRequest(userId, "Login", "User", "Login", "admin", "127.0.0.1", from, to, 2, 25, "Action", false);

        // Act
        var query = request.Adapt<GetAuditLogsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.UserId.ShouldBe(request.UserId);
        query.EventType.ShouldBe(request.EventType);
        query.EntityType.ShouldBe(request.EntityType);
        query.Action.ShouldBe(request.Action);
        query.Keyword.ShouldBe(request.Keyword);
        query.IpAddress.ShouldBe(request.IpAddress);
        query.From.ShouldBe(request.From);
        query.To.ShouldBe(request.To);
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
        query.SortBy.ShouldBe(request.SortBy);
        query.SortDesc.ShouldBe(request.SortDesc);
    }

    [Fact]
    public void GetAuditLogsRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new GetAuditLogsRequest();

        // Act
        var query = request.Adapt<GetAuditLogsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.UserId.ShouldBeNull();
        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(50);
        query.SortBy.ShouldBe("CreatedAt");
        query.SortDesc.ShouldBeTrue();
    }

    [Fact]
    public void GetAuditStatisticsRequest_MapsToQuery()
    {
        // Arrange
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 01, 31, 0, 0, 0, DateTimeKind.Utc);
        var request = new GetAuditStatisticsRequest(from, to);

        // Act
        var query = request.Adapt<GetAuditStatisticsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.From.ShouldBe(request.From);
        query.To.ShouldBe(request.To);
    }

    [Fact]
    public void GetAuditStatisticsRequest_WithNullDates_MapsToQuery()
    {
        // Arrange
        var request = new GetAuditStatisticsRequest(null, null);

        // Act
        var query = request.Adapt<GetAuditStatisticsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.From.ShouldBeNull();
        query.To.ShouldBeNull();
    }

    [Fact]
    public void AuditMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
