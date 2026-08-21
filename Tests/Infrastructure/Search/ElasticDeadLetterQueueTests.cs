using Application.Search.Features.Shared;
using Infrastructure.Persistence.Context;
using Infrastructure.Search;

namespace Tests.Infrastructure.Search;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ElasticDeadLetterQueueTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private ElasticDeadLetterQueue _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new ElasticDeadLetterQueue(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [RequiresDockerFact]
    public async Task DequeueAsync_WhenNoRowsExist_ReturnsEmpty()
    {
        var result = (await _sut.DequeueAsync(10, CancellationToken.None)).ToList();

        result.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task DequeueAsync_WhenNoPendingRowsExist_ReturnsEmpty()
    {
        var now = DateTime.UtcNow;
        _context.FailedElasticOperations.Add(BuildOperation(status: "Processed", createdAt: now.AddMinutes(-5)));
        _context.FailedElasticOperations.Add(BuildOperation(status: "Failed", createdAt: now.AddMinutes(-10)));
        await _context.SaveChangesAsync();

        var result = (await _sut.DequeueAsync(10, CancellationToken.None)).ToList();

        result.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task DequeueAsync_WithMixedStatuses_ReturnsOnlyPendingRows()
    {
        var now = DateTime.UtcNow;
        var pending = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-5));
        var processed = BuildOperation(status: "Processed", createdAt: now.AddMinutes(-10));
        var failed = BuildOperation(status: "Failed", createdAt: now.AddMinutes(-15));
        _context.FailedElasticOperations.AddRange(pending, processed, failed);
        await _context.SaveChangesAsync();

        var result = (await _sut.DequeueAsync(10, CancellationToken.None)).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(pending.Id);
        result[0].Status.ShouldBe("Pending");
    }

    [RequiresDockerFact]
    public async Task DequeueAsync_WithMultiplePendingRows_ReturnsUpToCountOrderedByCreatedAtAscending()
    {
        var now = DateTime.UtcNow;
        var newest = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-1), entityId: "newest");
        var middle = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-10), entityId: "middle");
        var oldest = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-30), entityId: "oldest");
        var extra = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-45), entityId: "extra");
        _context.FailedElasticOperations.AddRange(newest, middle, oldest, extra);
        await _context.SaveChangesAsync();

        var result = (await _sut.DequeueAsync(3, CancellationToken.None)).ToList();

        result.Count.ShouldBe(3);
        result[0].EntityId.ShouldBe("extra");
        result[1].EntityId.ShouldBe("oldest");
        result[2].EntityId.ShouldBe("middle");
    }

    [RequiresDockerFact]
    public async Task DequeueAsync_WhenPendingCountLessThanRequested_ReturnsAllPending()
    {
        var now = DateTime.UtcNow;
        var first = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-5), entityId: "a");
        var second = BuildOperation(status: "Pending", createdAt: now.AddMinutes(-3), entityId: "b");
        _context.FailedElasticOperations.AddRange(first, second);
        await _context.SaveChangesAsync();

        var result = (await _sut.DequeueAsync(10, CancellationToken.None)).ToList();

        result.Count.ShouldBe(2);
        result[0].EntityId.ShouldBe("a");
        result[1].EntityId.ShouldBe("b");
    }

    [RequiresDockerTheory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public async Task DequeueAsync_RespectsRequestedCount(int count)
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 7; i++)
        {
            _context.FailedElasticOperations.Add(BuildOperation(
                status: "Pending",
                createdAt: now.AddMinutes(-i),
                entityId: i.ToString(CultureInfo.InvariantCulture)));
        }
        await _context.SaveChangesAsync();

        var result = (await _sut.DequeueAsync(count, CancellationToken.None)).ToList();

        result.Count.ShouldBe(count);
    }

    private static FailedElasticOperation BuildOperation(
        string status,
        DateTime createdAt,
        string? entityId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityType = "Product",
            EntityId = entityId ?? Guid.NewGuid().ToString(),
            Document = "{}",
            Error = "test error",
            Status = status,
            RetryCount = 0,
            CreatedAt = createdAt
        };
}
