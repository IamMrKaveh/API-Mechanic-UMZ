using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tests.TestInfrastructure.Database;
using Xunit;

namespace Tests.Infrastructure.Persistence.Outbox;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OutboxArchiveMessageConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

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

    private static OutboxMessage NewProcessedSource(DateTime createdAt, DateTime processedAt, string type = "ArchivedEvent")
    {
        var message = OutboxMessage.Create(
            type,
            "{\"payload\":\"archived\"}",
            createdAt,
            traceParent: "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            traceState: "vendor=example");
        message.MarkProcessed(processedAt);
        return message;
    }

    [Fact]
    public async Task Persist_FromProcessed_RoundTripsAllMappedProperties()
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-5);
        var processedAt = DateTime.UtcNow.AddMinutes(-4);
        var archivedAt = DateTime.UtcNow;

        var source = NewProcessedSource(createdAt, processedAt, "OrderShipped");
        var archive = OutboxArchiveMessage.FromProcessed(source, archivedAt);

        await _context.OutboxArchiveMessages.AddAsync(archive);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxArchiveMessages
            .SingleAsync(m => m.Id == source.Id);

        reloaded.Id.ShouldBe(source.Id);
        reloaded.Type.ShouldBe("OrderShipped");
        reloaded.Payload.ShouldBe("{\"payload\":\"archived\"}");
        reloaded.CreatedAt.ShouldBe(createdAt, TimeSpan.FromMilliseconds(1));
        reloaded.ProcessedAt.ShouldBe(processedAt, TimeSpan.FromMilliseconds(1));
        reloaded.ArchivedAt.ShouldBe(archivedAt, TimeSpan.FromMilliseconds(1));
        reloaded.RetryCount.ShouldBe(0);
        reloaded.IsPoisoned.ShouldBeFalse();
        reloaded.TraceParent.ShouldBe("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        reloaded.TraceState.ShouldBe("vendor=example");
        reloaded.Error.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_FromProcessed_UsesOutboxMessagesArchiveTable()
    {
        var source = NewProcessedSource(DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(-1));
        var archive = OutboxArchiveMessage.FromProcessed(source, DateTime.UtcNow);

        await _context.OutboxArchiveMessages.AddAsync(archive);
        await _context.SaveChangesAsync();

        var count = await _context.Database.SqlQueryRaw<long>(
            "SELECT COUNT(*)::bigint AS \"Value\" FROM \"OutboxMessagesArchive\" WHERE id = {0}",
            source.Id.Value).SingleAsync();

        count.ShouldBe(1);
    }

    [Fact]
    public async Task FromProcessed_UnprocessedOutboxMessage_ThrowsInvalidOperationException()
    {
        var unprocessed = OutboxMessage.Create("Unprocessed", "{}", DateTime.UtcNow);

        var action = () => OutboxArchiveMessage.FromProcessed(unprocessed, DateTime.UtcNow);

        action.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    public void FromProcessed_NullSource_ThrowsArgumentNullException()
    {
        var action = () => OutboxArchiveMessage.FromProcessed(null!, DateTime.UtcNow);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public async Task Persist_ArchivedMessagesOrderedByArchivedAt_ReturnsExpectedOrder()
    {
        var now = DateTime.UtcNow;

        var older = OutboxArchiveMessage.FromProcessed(
            NewProcessedSource(now.AddMinutes(-10), now.AddMinutes(-9), "Older"),
            now.AddMinutes(-5));
        var newer = OutboxArchiveMessage.FromProcessed(
            NewProcessedSource(now.AddMinutes(-4), now.AddMinutes(-3), "Newer"),
            now.AddMinutes(-1));

        await _context.OutboxArchiveMessages.AddRangeAsync(older, newer);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var ordered = await freshContext.OutboxArchiveMessages
            .OrderBy(m => m.ArchivedAt)
            .Select(m => m.Type)
            .ToListAsync();

        ordered.ShouldBe(new[] { "Older", "Newer" });
    }

    [Fact]
    public async Task Persist_ArchivedFailedMessage_PreservesRetryCountAndErrorMessage()
    {
        var source = OutboxMessage.Create("FailedThenProcessed", "{}", DateTime.UtcNow.AddMinutes(-3));
        source.MarkFailed("transient failure");
        source.MarkFailed("still failing");
        source.MarkProcessed(DateTime.UtcNow.AddMinutes(-1));

        var archive = OutboxArchiveMessage.FromProcessed(source, DateTime.UtcNow);

        await _context.OutboxArchiveMessages.AddAsync(archive);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxArchiveMessages.SingleAsync(m => m.Id == source.Id);

        reloaded.RetryCount.ShouldBe(2);
        reloaded.Error.ShouldBeNull();
    }
}
