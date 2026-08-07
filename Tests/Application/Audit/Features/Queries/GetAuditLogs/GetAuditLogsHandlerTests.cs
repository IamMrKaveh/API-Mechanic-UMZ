using Application.Audit.Contracts;
using Application.Audit.Features.Queries.GetAuditLogs;
using Application.Audit.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Audit.Features.Queries.GetAuditLogs;

public class GetAuditLogsHandlerTests
{
    private readonly IAuditQueryService _queryService = Substitute.For<IAuditQueryService>();
    private readonly GetAuditLogsHandler _sut;

    public GetAuditLogsHandlerTests()
    {
        _sut = new GetAuditLogsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsFlatPaginatedResultOfAuditLogDto()
    {
        var logs = new List<AuditLogDto>
        {
            new() { Id = Guid.NewGuid(), EventType = "Security", Action = "Login" },
            new() { Id = Guid.NewGuid(), EventType = "Order", Action = "Created" }
        };

        _queryService
            .SearchAsync(Arg.Any<AuditSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((logs.AsReadOnly() as IReadOnlyList<AuditLogDto>, 2));

        var query = new GetAuditLogsQuery(null, null, null, null, null, null, null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeOfType<PaginatedResult<AuditLogDto>>();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items[0].EventType.ShouldBe("Security");
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_PropagatesEntityTypeToSearchRequest()
    {
        AuditSearchRequest? captured = null;

        _queryService
            .SearchAsync(Arg.Do<AuditSearchRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AuditLogDto>() as IReadOnlyList<AuditLogDto>, 0));

        var query = new GetAuditLogsQuery(
            UserId: null,
            EventType: "Order",
            EntityType: "Order",
            Action: null,
            Keyword: null,
            IpAddress: null,
            From: null,
            To: null);

        await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.EntityType.ShouldBe("Order");
        captured.EventType.ShouldBe("Order");
    }

    [Fact]
    public async Task Handle_PropagatesSortByAndSortDesc()
    {
        AuditSearchRequest? captured = null;

        _queryService
            .SearchAsync(Arg.Do<AuditSearchRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AuditLogDto>() as IReadOnlyList<AuditLogDto>, 0));

        var query = new GetAuditLogsQuery(
            null, null, null, null, null, null, null, null,
            Page: 2, PageSize: 25, SortBy: "EventType", SortDesc: false);

        await _sut.Handle(query, CancellationToken.None);

        captured!.SortBy.ShouldBe("EventType");
        captured.SortDesc.ShouldBeFalse();
        captured.Page.ShouldBe(2);
        captured.PageSize.ShouldBe(25);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmpty_ReturnsSuccessWithZeroTotal()
    {
        _queryService
            .SearchAsync(Arg.Any<AuditSearchRequest>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AuditLogDto>() as IReadOnlyList<AuditLogDto>, 0));

        var query = new GetAuditLogsQuery(null, null, null, null, null, null, null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }
}
