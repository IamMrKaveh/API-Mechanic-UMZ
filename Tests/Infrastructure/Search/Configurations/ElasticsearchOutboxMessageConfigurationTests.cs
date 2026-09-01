using Application.Search.Features.Shared;
using Infrastructure.Persistence.Context;
using Infrastructure.Search;

namespace Tests.Infrastructure.Search.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ElasticsearchOutboxMessageConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<ElasticsearchOutboxMessage> PersistAsync(ElasticsearchOutboxMessage message)
    {
        await _context.ElasticsearchOutboxMessages.AddAsync(message);
        await _context.SaveChangesAsync();
        return message;
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesAllScalarProperties()
    {
        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            Guid.NewGuid(),
            "{\"id\":1}",
            "Upsert");

        await PersistAsync(message);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.ElasticsearchOutboxMessages
            .FirstAsync(m => m.Id == message.Id);

        loaded.Id.ShouldBe(message.Id);
        loaded.EntityType.ShouldBe("Product");
        loaded.EntityId.ShouldBe(message.EntityId);
        loaded.Document.ShouldBe("{\"id\":1}");
        loaded.ChangeType.ShouldBe("Upsert");
        loaded.IdempotencyKey.ShouldBe(message.IdempotencyKey);
        loaded.RetryCount.ShouldBe(0);
        loaded.ProcessedAt.ShouldBeNull();
        loaded.Error.ShouldBeNull();
        loaded.IsPoisoned.ShouldBeFalse();
        loaded.CreatedAt.ShouldNotBe(default);
        loaded.NextAttemptAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChanges_AfterMarkFailed_PersistsErrorAndNextAttemptAndRetryCount()
    {
        var message = ElasticsearchOutboxMessage.Create(
            "Category",
            Guid.NewGuid(),
            "{}",
            "Upsert");

        message.MarkFailed("index refused", TimeSpan.FromMinutes(5));
        await PersistAsync(message);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.ElasticsearchOutboxMessages
            .FirstAsync(m => m.Id == message.Id);

        loaded.RetryCount.ShouldBe(1);
        loaded.Error.ShouldBe("index refused");
        loaded.NextAttemptAt.ShouldNotBeNull();
        loaded.ProcessedAt.ShouldBeNull();
        loaded.IsPoisoned.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_AfterMarkPoisoned_PersistsPoisonedFlagAndClearsNextAttempt()
    {
        var message = ElasticsearchOutboxMessage.Create(
            "Brand",
            Guid.NewGuid(),
            "{}",
            "Upsert");

        message.MarkPoisoned("permanent failure");
        await PersistAsync(message);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.ElasticsearchOutboxMessages
            .FirstAsync(m => m.Id == message.Id);

        loaded.IsPoisoned.ShouldBeTrue();
        loaded.Error.ShouldBe("permanent failure");
        loaded.NextAttemptAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicateIdempotencyKey_ThrowsDbUpdateException()
    {
        var entityId = Guid.NewGuid();

        var first = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");
        await PersistAsync(first);

        var second = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.ElasticsearchOutboxMessages.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task SaveChanges_DifferentChangeTypeForSameEntity_ProducesDistinctIdempotencyKeysAndPersistsBoth()
    {
        var entityId = Guid.NewGuid();

        var upsert = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Upsert");
        var delete = ElasticsearchOutboxMessage.Create("Product", entityId, "{}", "Delete");

        await PersistAsync(upsert);
        await PersistAsync(delete);

        await using var freshContext = _fixture.CreateContext();
        var count = await freshContext.ElasticsearchOutboxMessages
            .CountAsync(m => m.EntityId == entityId);

        count.ShouldBe(2);
    }

    [Fact]
    public async Task IsPoisoned_ForBrandNewMessage_DefaultsToFalseAtDatabaseLevel()
    {
        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            Guid.NewGuid(),
            "{}",
            "Upsert");

        await PersistAsync(message);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.ElasticsearchOutboxMessages
            .FirstAsync(m => m.Id == message.Id);

        loaded.IsPoisoned.ShouldBeFalse();
    }

    [Fact]
    public async Task Persisted_MessageMappedToConfiguredTable_IsQueryableViaContextDbSet()
    {
        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            Guid.NewGuid(),
            "{}",
            "Upsert");

        await PersistAsync(message);

        await using var freshContext = _fixture.CreateContext();
        var exists = await freshContext.ElasticsearchOutboxMessages
            .AnyAsync(m => m.IdempotencyKey == message.IdempotencyKey);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_AfterMarkProcessed_PersistsProcessedAtAndClearsNextAttempt()
    {
        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            Guid.NewGuid(),
            "{}",
            "Upsert");

        message.MarkProcessed();
        await PersistAsync(message);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.ElasticsearchOutboxMessages
            .FirstAsync(m => m.Id == message.Id);

        loaded.ProcessedAt.ShouldNotBeNull();
        loaded.NextAttemptAt.ShouldBeNull();
        loaded.Error.ShouldBeNull();
    }
}
