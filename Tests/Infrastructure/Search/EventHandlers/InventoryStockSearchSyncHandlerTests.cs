using Application.Common.Events;
using Domain.Inventory.Events;
using Domain.Inventory.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;
using Infrastructure.Search.EventHandlers;

namespace Tests.Infrastructure.Search.EventHandlers;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class InventoryStockSearchSyncHandlerTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private static DomainEventNotification<StockIncreasedEvent> IncreaseNotification(VariantId variantId) =>
        new(new StockIncreasedEvent(InventoryId.NewId(), variantId, 5, 15, "restock"));

    private static DomainEventNotification<StockReservedEvent> ReserveNotification(VariantId variantId) =>
        new(new StockReservedEvent(InventoryId.NewId(), variantId, 2, 2));

    private static DomainEventNotification<StockReservationReleasedEvent> ReleaseNotification(VariantId variantId) =>
        new(new StockReservationReleasedEvent(InventoryId.NewId(), variantId, 2, 0));

    private async Task<ProductVariant> SeedProductVariantAsync()
    {
        await using var context = _fixture.CreateContext();

        var category = await new CategoryBuilder().BuildAsync();
        category.ClearDomainEvents();
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var brand = await new BrandBuilder().WithCategoryId(category.Id).BuildAsync();
        brand.ClearDomainEvents();
        context.Brands.Add(brand);
        await context.SaveChangesAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariantBuilder().WithProductId(product.Id).Build();
        variant.ClearDomainEvents();
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        return variant;
    }

    [Fact]
    public async Task Handle_StockIncreased_EnqueuesProductOutboxMessageForKnownVariant()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(IncreaseNotification(variant.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var messages = await verify.ElasticsearchOutboxMessages.ToListAsync();
        messages.Count.ShouldBe(1);
        messages[0].EntityType.ShouldBe("Product");
        messages[0].ChangeType.ShouldBe("StockChanged");
    }

    [Fact]
    public async Task Handle_StockReserved_EnqueuesProductOutboxMessageForKnownVariant()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(ReserveNotification(variant.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var messages = await verify.ElasticsearchOutboxMessages.ToListAsync();
        messages.Count.ShouldBe(1);
        messages[0].EntityType.ShouldBe("Product");
        messages[0].ChangeType.ShouldBe("StockChanged");
    }

    [Fact]
    public async Task Handle_StockReservationReleased_EnqueuesProductOutboxMessageForKnownVariant()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(ReleaseNotification(variant.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var messages = await verify.ElasticsearchOutboxMessages.ToListAsync();
        messages.Count.ShouldBe(1);
        messages[0].EntityType.ShouldBe("Product");
        messages[0].ChangeType.ShouldBe("StockChanged");
    }

    [Fact]
    public async Task Handle_OutboxEntityId_MatchesProductIdOfPersistedVariant()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(IncreaseNotification(variant.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var message = await verify.ElasticsearchOutboxMessages.SingleAsync();
        message.EntityId.ShouldBe(variant.ProductId.Value);
    }

    [Fact]
    public async Task Handle_SerializedDocument_ContainsProductIdOfVariant()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(IncreaseNotification(variant.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var message = await verify.ElasticsearchOutboxMessages.SingleAsync();
        message.Document.ShouldContain(variant.ProductId.Value.ToString());
    }

    [Fact]
    public async Task Handle_StockIncreased_WhenVariantDoesNotExist_DoesNothing()
    {
        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(IncreaseNotification(VariantId.NewId()), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        (await verify.ElasticsearchOutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Handle_StockReserved_WhenVariantDoesNotExist_DoesNothing()
    {
        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(ReserveNotification(VariantId.NewId()), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        (await verify.ElasticsearchOutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Handle_StockReservationReleased_WhenVariantDoesNotExist_DoesNothing()
    {
        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(ReleaseNotification(VariantId.NewId()), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        (await verify.ElasticsearchOutboxMessages.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Handle_MultipleStockEventsForSameVariant_EnqueuesOneMessagePerEvent()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        await sut.Handle(IncreaseNotification(variant.Id), CancellationToken.None);
        await sut.Handle(ReserveNotification(variant.Id), CancellationToken.None);
        await sut.Handle(ReleaseNotification(variant.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        await using var verify = _fixture.CreateContext();
        var count = await verify.ElasticsearchOutboxMessages.CountAsync();
        count.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToDbQuery()
    {
        var variant = await SeedProductVariantAsync();

        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.Handle(IncreaseNotification(variant.Id), cts.Token));
    }

    [Fact]
    public async Task Handler_ImplementsAllThreeStockEventNotificationHandlers()
    {
        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        sut.ShouldBeAssignableTo<INotificationHandler<DomainEventNotification<StockIncreasedEvent>>>();
        sut.ShouldBeAssignableTo<INotificationHandler<DomainEventNotification<StockReservedEvent>>>();
        sut.ShouldBeAssignableTo<INotificationHandler<DomainEventNotification<StockReservationReleasedEvent>>>();
    }

    [Fact]
    public async Task Handler_IsSealed()
    {
        await using var context = _fixture.CreateContext();
        var sut = new InventoryStockSearchSyncHandler(context);

        sut.GetType().IsSealed.ShouldBeTrue();
    }
}
