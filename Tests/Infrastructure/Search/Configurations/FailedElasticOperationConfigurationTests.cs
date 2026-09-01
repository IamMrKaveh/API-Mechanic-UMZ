using Application.Search.Features.Shared;
using Infrastructure.Persistence.Context;

namespace Tests.Infrastructure.Search.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class FailedElasticOperationConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static FailedElasticOperation BuildOperation(
        string status = "Pending",
        string entityType = "Product",
        string? entityId = null,
        string document = "{}",
        string error = "test error",
        int retryCount = 0,
        DateTime? createdAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId ?? Guid.NewGuid().ToString(),
            Document = document,
            Error = error,
            Status = status,
            RetryCount = retryCount,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesAllScalarProperties()
    {
        var operation = BuildOperation(
            status: "Pending",
            entityType: "Product",
            entityId: "prod-123",
            document: "{\"data\":true}",
            error: "connection refused",
            retryCount: 2,
            createdAt: DateTime.UtcNow);

        await _context.FailedElasticOperations.AddAsync(operation);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.FailedElasticOperations
            .FirstAsync(o => o.Id == operation.Id);

        loaded.Id.ShouldBe(operation.Id);
        loaded.EntityType.ShouldBe("Product");
        loaded.EntityId.ShouldBe("prod-123");
        loaded.Document.ShouldBe("{\"data\":true}");
        loaded.Error.ShouldBe("connection refused");
        loaded.Status.ShouldBe("Pending");
        loaded.RetryCount.ShouldBe(2);
        loaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_WithLargeDocumentPayload_PersistsWithoutTruncation()
    {
        var largeDocument = new string('x', 20_000);
        var operation = BuildOperation(document: largeDocument);

        await _context.FailedElasticOperations.AddAsync(operation);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.FailedElasticOperations
            .FirstAsync(o => o.Id == operation.Id);

        loaded.Document.Length.ShouldBe(20_000);
        loaded.Document.ShouldBe(largeDocument);
    }

    [Fact]
    public async Task SaveChanges_WithLargeErrorPayload_PersistsWithoutTruncation()
    {
        var largeError = new string('e', 15_000);
        var operation = BuildOperation(error: largeError);

        await _context.FailedElasticOperations.AddAsync(operation);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.FailedElasticOperations
            .FirstAsync(o => o.Id == operation.Id);

        loaded.Error.Length.ShouldBe(15_000);
        loaded.Error.ShouldBe(largeError);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Processed")]
    [InlineData("Failed")]
    public async Task SaveChanges_WithKnownStatusValues_PersistsAndQueryableByStatus(string status)
    {
        var operation = BuildOperation(status: status);

        await _context.FailedElasticOperations.AddAsync(operation);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var count = await freshContext.FailedElasticOperations
            .CountAsync(o => o.Status == status && o.Id == operation.Id);

        count.ShouldBe(1);
    }

    [Fact]
    public async Task Query_ByEntityTypeAndEntityIdComposite_ReturnsMatchingRows()
    {
        var entityId = Guid.NewGuid().ToString();
        var target = BuildOperation(entityType: "Category", entityId: entityId);
        var other = BuildOperation(entityType: "Product", entityId: entityId);
        var different = BuildOperation(entityType: "Category", entityId: Guid.NewGuid().ToString());

        _context.FailedElasticOperations.AddRange(target, other, different);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var result = await freshContext.FailedElasticOperations
            .Where(o => o.EntityType == "Category" && o.EntityId == entityId)
            .ToListAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(target.Id);
    }

    [Fact]
    public async Task Query_FilteringByStatus_ExercisesStatusIndexAndReturnsExpectedRows()
    {
        var pending = BuildOperation(status: "Pending", entityId: "p1");
        var processed = BuildOperation(status: "Processed", entityId: "p2");
        var failed = BuildOperation(status: "Failed", entityId: "p3");

        _context.FailedElasticOperations.AddRange(pending, processed, failed);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var pendingResults = await freshContext.FailedElasticOperations
            .Where(o => o.Status == "Pending")
            .Select(o => o.Id)
            .ToListAsync();

        pendingResults.ShouldContain(pending.Id);
        pendingResults.ShouldNotContain(processed.Id);
        pendingResults.ShouldNotContain(failed.Id);
    }

    [Fact]
    public async Task SaveChanges_MultipleRowsWithSameEntityTypeAndEntityId_AllPersistWithoutUniqueConstraint()
    {
        var entityId = Guid.NewGuid().ToString();
        var first = BuildOperation(entityType: "Product", entityId: entityId, error: "first");
        var second = BuildOperation(entityType: "Product", entityId: entityId, error: "second");

        _context.FailedElasticOperations.AddRange(first, second);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var count = await freshContext.FailedElasticOperations
            .CountAsync(o => o.EntityType == "Product" && o.EntityId == entityId);

        count.ShouldBe(2);
    }
}
