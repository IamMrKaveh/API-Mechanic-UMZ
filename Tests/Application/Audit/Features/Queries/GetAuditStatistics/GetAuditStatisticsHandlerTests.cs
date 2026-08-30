using Application.Audit.Contracts;
using Application.Audit.Features.Queries.GetAuditStatistics;
using Application.Audit.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Audit.Features.Queries.GetAuditStatistics;

public class GetAuditStatisticsHandlerTests
{
    private readonly IAuditQueryService _queryService = Substitute.For<IAuditQueryService>();
    private readonly GetAuditStatisticsHandler _sut;

    public GetAuditStatisticsHandlerTests()
    {
        _sut = new GetAuditStatisticsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithStatisticsFromService()
    {
        var expected = new AuditStatisticsDto
        {
            TotalLogs = 42,
            ByEventType = new Dictionary<string, long>
            {
                ["Security"] = 20,
                ["Order"] = 22
            },
            ByHour = new Dictionary<string, long>
            {
                ["2026-08-29T10:00"] = 15,
                ["2026-08-29T11:00"] = 27
            }
        };

        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetAuditStatisticsQuery(null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        result.Value.TotalLogs.ShouldBe(42);
        result.Value.ByEventType.Count.ShouldBe(2);
        result.Value.ByHour.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WithNullDates_PassesNullBoundsToService()
    {
        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditStatisticsDto());

        var query = new GetAuditStatisticsQuery(null, null);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetStatisticsAsync(
            (DateTime?)null,
            (DateTime?)null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesBoundsToService()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 29, 23, 59, 59, DateTimeKind.Utc);

        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditStatisticsDto());

        var query = new GetAuditStatisticsQuery(from, to);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetStatisticsAsync(
            from,
            to,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOnlyFrom_PassesFromAndNullTo()
    {
        var from = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditStatisticsDto());

        var query = new GetAuditStatisticsQuery(from, null);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetStatisticsAsync(from, (DateTime?)null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOnlyTo_PassesNullFromAndTo()
    {
        var to = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditStatisticsDto());

        var query = new GetAuditStatisticsQuery(null, to);

        await _sut.Handle(query, CancellationToken.None);

        await _queryService.Received(1).GetStatisticsAsync((DateTime?)null, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToService()
    {
        using var cts = new CancellationTokenSource();
        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditStatisticsDto());

        var query = new GetAuditStatisticsQuery(null, null);

        await _sut.Handle(query, cts.Token);

        await _queryService.Received(1).GetStatisticsAsync(
            (DateTime?)null,
            (DateTime?)null,
            cts.Token);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmptyStatistics_StillReturnsSuccess()
    {
        var empty = new AuditStatisticsDto
        {
            TotalLogs = 0,
            ByEventType = new Dictionary<string, long>(),
            ByHour = new Dictionary<string, long>()
        };

        _queryService
            .GetStatisticsAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var query = new GetAuditStatisticsQuery(null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.TotalLogs.ShouldBe(0);
        result.Value.ByEventType.ShouldBeEmpty();
        result.Value.ByHour.ShouldBeEmpty();
    }
}
