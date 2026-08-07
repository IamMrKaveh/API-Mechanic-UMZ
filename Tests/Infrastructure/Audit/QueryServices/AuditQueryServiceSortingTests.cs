using Application.Audit.Features.Shared;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Audit.QueryServices;

[Collection(nameof(DatabaseCollection))]
public class AuditQueryServiceSortingTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private AuditQueryService _sut = null!;

    public async Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();

        await SeedAsync();

        _sut = new AuditQueryService(_context);
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [SkippableTheory]
    [InlineData("CreatedAt", true)]
    [InlineData("CreatedAt", false)]
    [InlineData("EventType", true)]
    [InlineData("EventType", false)]
    [InlineData("Action", true)]
    [InlineData("Action", false)]
    [InlineData(null, true)]
    [InlineData("UnknownColumn", false)]
    public async Task SearchAsync_AppliesRequestedSort(string? sortBy, bool sortDesc)
    {
        var request = new AuditSearchRequest
        {
            SortBy = sortBy,
            SortDesc = sortDesc,
            Page = 1,
            PageSize = 50
        };

        var (logs, total) = await _sut.SearchAsync(request, CancellationToken.None);

        total.ShouldBe(4);
        logs.Count.ShouldBe(4);

        switch (sortBy?.ToLowerInvariant())
        {
            case "eventtype":
                AssertOrdered(logs.Select(l => l.EventType).ToList(), sortDesc);
                break;

            case "action":
                AssertOrdered(logs.Select(l => l.Action).ToList(), sortDesc);
                break;

            default:
                AssertOrdered(logs.Select(l => l.CreatedAt.Ticks).ToList(), sortDesc);
                break;
        }
    }

    [SkippableFact]
    public async Task SearchAsync_WithEntityTypeFilter_ReturnsOnlyMatchingRows()
    {
        var request = new AuditSearchRequest
        {
            EntityType = "Order",
            Page = 1,
            PageSize = 50
        };

        var (logs, total) = await _sut.SearchAsync(request, CancellationToken.None);

        total.ShouldBe(2);
        logs.ShouldAllBe(l => l.EntityType == "Order");
    }

    private async Task SeedAsync()
    {
        var logs = new[]
        {
            new AuditLogBuilder().WithEventType("Alpha").WithAction("Create").WithEntityType("Order").Build(),
            new AuditLogBuilder().WithEventType("Bravo").WithAction("Update").WithEntityType("Order").Build(),
            new AuditLogBuilder().WithEventType("Charlie").WithAction("Delete").WithEntityType("Payment").Build(),
            new AuditLogBuilder().WithEventType("Delta").WithAction("Read").WithEntityType("Product").Build(),
        };

        foreach (var log in logs)
        {
            await Task.Delay(2);
            _context.AuditLogs.Add(log);
        }

        await _context.SaveChangesAsync();
    }

    private static void AssertOrdered<T>(IReadOnlyList<T> values, bool desc) where T : IComparable<T>
    {
        var expected = desc
            ? values.OrderByDescending(v => v).ToList()
            : values.OrderBy(v => v).ToList();

        values.SequenceEqual(expected).ShouldBeTrue(
            $"Expected order: [{string.Join(",", expected)}] but got [{string.Join(",", values)}]");
    }
}
