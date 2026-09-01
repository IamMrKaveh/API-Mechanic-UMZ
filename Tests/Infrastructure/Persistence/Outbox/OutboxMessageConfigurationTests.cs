using Infrastructure.Persistence.Outbox;

namespace Tests.Infrastructure.Persistence.Outbox;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OutboxMessageConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private static OutboxMessage NewPendingMessage(DateTime createdAt, string type = "TestEvent", string? payload = null)
    {
        return OutboxMessage.Create(
            type,
            payload ?? "{\"value\":\"test\"}",
            createdAt,
            traceParent: "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            traceState: "vendor=example");
    }

    [Fact]
    public async Task Persist_NewOutboxMessage_RoundTripsAllMappedProperties()
    {
        var createdAt = DateTime.UtcNow;
        var message = NewPendingMessage(createdAt, "OrderPlaced", "{\"order\":123}");

        await _context.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxMessages
            .SingleAsync(m => m.Id == message.Id);

        reloaded.Id.ShouldBe(message.Id);
        reloaded.Type.ShouldBe("OrderPlaced");
        reloaded.Payload.ShouldBe("{\"order\":123}");
        reloaded.CreatedAt.ShouldBe(createdAt, TimeSpan.FromMilliseconds(1));
        reloaded.ProcessedAt.ShouldBeNull();
        reloaded.Error.ShouldBeNull();
        reloaded.RetryCount.ShouldBe(0);
        reloaded.IsPoisoned.ShouldBeFalse();
        reloaded.TraceParent.ShouldBe("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        reloaded.TraceState.ShouldBe("vendor=example");
    }

    [Fact]
    public async Task Persist_MarkProcessed_PersistsProcessedAtAndClearsError()
    {
        var message = NewPendingMessage(DateTime.UtcNow);
        message.MarkFailed("transient error");

        await _context.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        var processedAt = DateTime.UtcNow.AddSeconds(1);
        message.MarkProcessed(processedAt);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxMessages.SingleAsync(m => m.Id == message.Id);

        reloaded.ProcessedAt.ShouldNotBeNull();
        reloaded.ProcessedAt!.Value.ShouldBe(processedAt, TimeSpan.FromMilliseconds(1));
        reloaded.Error.ShouldBeNull();
        reloaded.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task Persist_MarkFailed_IncrementsRetryCountAndStoresError()
    {
        var message = NewPendingMessage(DateTime.UtcNow);
        message.MarkFailed("connection timeout");
        message.MarkFailed("connection refused");

        await _context.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxMessages.SingleAsync(m => m.Id == message.Id);

        reloaded.RetryCount.ShouldBe(2);
        reloaded.Error.ShouldBe("connection refused");
        reloaded.IsPoisoned.ShouldBeFalse();
        reloaded.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_MarkPoisoned_PersistsPoisonedFlagAndErrorMessage()
    {
        var message = NewPendingMessage(DateTime.UtcNow);
        message.MarkPoisoned("payload validation failed");

        await _context.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxMessages.SingleAsync(m => m.Id == message.Id);

        reloaded.IsPoisoned.ShouldBeTrue();
        reloaded.Error.ShouldBe("payload validation failed");
    }

    [Fact]
    public async Task Query_PendingFilteredIndex_YieldsOnlyEligibleMessages()
    {
        var now = DateTime.UtcNow;

        var eligible = NewPendingMessage(now.AddMinutes(-1), "Eligible");
        var processed = NewPendingMessage(now.AddMinutes(-2), "Processed");
        processed.MarkProcessed(now);
        var poisoned = NewPendingMessage(now.AddMinutes(-3), "Poisoned");
        poisoned.MarkPoisoned("boom");
        var overRetries = NewPendingMessage(now.AddMinutes(-4), "OverRetries");
        for (int i = 0; i < 5; i++)
            overRetries.MarkFailed("retry");

        await _context.OutboxMessages.AddRangeAsync(eligible, processed, poisoned, overRetries);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var pending = await freshContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.IsPoisoned == false && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        pending.Select(m => m.Id).ShouldContain(eligible.Id);
        pending.Select(m => m.Id).ShouldNotContain(processed.Id);
        pending.Select(m => m.Id).ShouldNotContain(poisoned.Id);
        pending.Select(m => m.Id).ShouldNotContain(overRetries.Id);
    }

    [Fact]
    public async Task Persist_OutboxMessageWithoutTracingHeaders_PersistsNullValues()
    {
        var message = OutboxMessage.Create("EventNoTrace", "{}", DateTime.UtcNow);

        await _context.OutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.OutboxMessages.SingleAsync(m => m.Id == message.Id);

        reloaded.TraceParent.ShouldBeNull();
        reloaded.TraceState.ShouldBeNull();
        reloaded.Error.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_DuplicatePrimaryKey_ThrowsDbUpdateException()
    {
        var first = NewPendingMessage(DateTime.UtcNow, "First");

        await _context.OutboxMessages.AddAsync(first);
        await _context.SaveChangesAsync();

        await using var secondContext = _fixture.CreateContext();
        var duplicateSql =
            "INSERT INTO \"OutboxMessages\" (id, type, payload, created_at, retry_count, is_poisoned) " +
            "VALUES ({0}, {1}, {2}, {3}, {4}, {5})";

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await secondContext.Database.ExecuteSqlRawAsync(
                duplicateSql,
                first.Id.Value,
                "Duplicate",
                "{}",
                DateTime.UtcNow,
                0,
                false);
        });
    }
}
