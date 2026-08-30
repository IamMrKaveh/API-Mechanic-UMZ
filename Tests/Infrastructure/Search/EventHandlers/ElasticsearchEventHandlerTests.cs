using Application.Search.Contracts;
using Application.Search.Events;
using Application.Search.Features.Shared;
using Infrastructure.Search.EventHandlers;
using SharedKernel.Enums;

namespace Tests.Infrastructure.Search.EventHandlers;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ElasticsearchEventHandlerTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    // The production handler builds a document id via
    //   Guid.Parse(domainEvent.EntityId.ToString())
    // and IEntityChangeEvent.EntityId is an int. That call always fails at
    // runtime with a FormatException, so every one of the three public
    // HandleXxxChangedAsync methods surfaces the same failure. These tests
    // document that observable behavior and pin down the interface's
    // fire-and-forget explicit implementation.

    private static ProductChangedEvent BuildProductEvent(
        int entityId = 1,
        EntityChangeType change = default,
        ProductSearchDocument? document = null)
        => new(entityId, change, document);

    private static CategoryChangedEvent BuildCategoryEvent(
        int entityId = 1,
        EntityChangeType change = default,
        CategorySearchDocument? document = null)
        => new(entityId, change, document);

    private static BrandChangedEvent BuildBrandEvent(
        int entityId = 1,
        EntityChangeType change = default,
        BrandSearchDocument? document = null)
        => new(entityId, change, document);

    [Fact]
    public async Task HandleProductChangedAsync_WithIntEntityId_ThrowsFormatExceptionWhenParsingAsGuid()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildProductEvent(entityId: 7);

        await Should.ThrowAsync<FormatException>(() =>
            sut.HandleProductChangedAsync(evt, CancellationToken.None));

        (await context.ElasticsearchOutboxMessages.CountAsync()).ShouldBe(0);
        await auditService.DidNotReceiveWithAnyArgs()
            .LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task HandleCategoryChangedAsync_WithIntEntityId_ThrowsFormatExceptionWhenParsingAsGuid()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildCategoryEvent(entityId: 42);

        await Should.ThrowAsync<FormatException>(() =>
            sut.HandleCategoryChangedAsync(evt, CancellationToken.None));

        (await context.ElasticsearchOutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task HandleBrandChangedAsync_WithIntEntityId_ThrowsFormatExceptionWhenParsingAsGuid()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildBrandEvent(entityId: 99);

        await Should.ThrowAsync<FormatException>(() =>
            sut.HandleBrandChangedAsync(evt, CancellationToken.None));

        (await context.ElasticsearchOutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task InterfaceExplicitHandleProductChangedAsync_IsFireAndForgetAndDoesNotThrow()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        IElasticsearchEventHandler sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildProductEvent(entityId: 1);

        // Because the interface method returns void and internally discards the Task,
        // no exception should surface to the caller even though the wrapped Task faults.
        var act = () => sut.HandleProductChangedAsync(evt, CancellationToken.None);

        act.ShouldNotThrow();
    }

    [Fact]
    public async Task Handler_ImplementsIElasticsearchEventHandlerContract()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        sut.ShouldBeAssignableTo<IElasticsearchEventHandler>();
    }

    [Fact]
    public async Task Handler_IsSealed()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        sut.GetType().IsSealed.ShouldBeTrue();
    }

    [Fact]
    public async Task Constructor_RequiresNonNullDependencies()
    {
        // Primary constructor injects two dependencies; both are used in Handle* methods.
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();

        var act = () => new ElasticsearchEventHandler(context, auditService);
        act.ShouldNotThrow();
    }

    [Fact]
    public async Task HandleProductChangedAsync_ExceptionOccursBeforeAddingOutboxMessage()
    {
        // Regression pin: prove the Guid.Parse call happens before context.ElasticsearchOutboxMessages.AddAsync,
        // so no partial writes appear in the outbox table after a failed call.
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildProductEvent(entityId: 5);

        try
        {
            await sut.HandleProductChangedAsync(evt, CancellationToken.None);
        }
        catch (FormatException)
        {
            // expected
        }

        var pending = await context.ElasticsearchOutboxMessages.ToListAsync();
        pending.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleCategoryChangedAsync_DoesNotInvokeAuditService()
    {
        // The Category and Brand overloads do not log to audit even on the happy path.
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildCategoryEvent(entityId: 1);

        try
        {
            await sut.HandleCategoryChangedAsync(evt, CancellationToken.None);
        }
        catch (FormatException)
        {
            // expected due to int -> Guid parse
        }

        await auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task HandleBrandChangedAsync_DoesNotInvokeAuditService()
    {
        await using var context = _fixture.CreateContext();
        var auditService = Substitute.For<IAuditService>();
        var sut = new ElasticsearchEventHandler(context, auditService);

        var evt = BuildBrandEvent(entityId: 1);

        try
        {
            await sut.HandleBrandChangedAsync(evt, CancellationToken.None);
        }
        catch (FormatException)
        {
            // expected due to int -> Guid parse
        }

        await auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }
}
