using Domain.Attribute.Aggregates;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Persistence.Interceptors;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AuditableEntityInterceptorTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _fixture.ResetAsync();
    }

    private DBContext CreateContextWithProvider(IDateTimeProvider provider)
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(provider),
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));
    }

    [Fact]
    public async Task SavingChanges_WithAddedAuditableEntity_SetsCreatedAtToProviderUtcNowAndLeavesUpdatedAtNull()
    {
        var fixedNow = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(fixedNow);

        await using var writeContext = CreateContextWithProvider(provider);
        var type = await new AttributeTypeBuilder()
            .WithName("color")
            .WithDisplayName("Color")
            .BuildAsync();

        writeContext.AttributeTypes.Add(type);
        await writeContext.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateContext();
        var persisted = await verifyContext.AttributeTypes.FirstOrDefaultAsync(a => a.Id == type.Id);
        persisted.ShouldNotBeNull();
        persisted.CreatedAt.ShouldBe(fixedNow);
        persisted.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SavingChanges_WithModifiedAuditableEntity_UpdatesUpdatedAtToProviderUtcNowAndPreservesCreatedAt()
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var initialProvider = Substitute.For<IDateTimeProvider>();
        initialProvider.UtcNow.Returns(createdAt);

        await using (var writeContext = CreateContextWithProvider(initialProvider))
        {
            var type = await new AttributeTypeBuilder()
                .WithName("size")
                .WithDisplayName("Size")
                .BuildAsync();
            writeContext.AttributeTypes.Add(type);
            await writeContext.SaveChangesAsync();
        }

        AttributeType stored;
        var updateProvider = Substitute.For<IDateTimeProvider>();
        updateProvider.UtcNow.Returns(updatedAt);

        await using (var updateContext = CreateContextWithProvider(updateProvider))
        {
            stored = (await updateContext.AttributeTypes.FirstAsync())!;
            stored.MarkAsDeleted(deletedBy: null);
            updateContext.AttributeTypes.Update(stored);
            await updateContext.SaveChangesAsync();
        }

        await using var verifyContext = _fixture.CreateContext();
        var persisted = await verifyContext.AttributeTypes.IgnoreQueryFilters().FirstAsync(a => a.Id == stored.Id);
        persisted.CreatedAt.ShouldBe(createdAt);
        persisted.UpdatedAt.ShouldBe(updatedAt);
    }

    [Fact]
    public async Task SavingChanges_WithNonAuditableEntity_DoesNotThrow()
    {
        var provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        await using var writeContext = CreateContextWithProvider(provider);
        var message = OutboxMessage.Create("SampleEvent", "{}", DateTime.UtcNow);
        writeContext.OutboxMessages.Add(message);

        await writeContext.SaveChangesAsync();

        await using var verifyContext = _fixture.CreateContext();
        var persisted = await verifyContext.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
        persisted.ShouldNotBeNull();
    }
}
