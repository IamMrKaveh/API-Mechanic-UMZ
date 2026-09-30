using Application.Cache.Contracts;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.EntityFramework.Configurations;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Cache.OutputCache;

/// <summary>
/// Proves that saving an EF entity evicts the cached queries tagged with its type name,
/// which is what replaced the hand-written cache invalidation in command handlers.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class EfAutoEvictIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    [RequestOutputCache(tags: [CacheTags.Category], expirationInSeconds: 300)]
    public sealed record CategoryProbeQuery : IRequest<ServiceResult<int>>;

    [RequestOutputCache(tags: [CacheTags.Warehouse], expirationInSeconds: 300)]
    public sealed record WarehouseProbeQuery : IRequest<ServiceResult<int>>;

    public sealed class ProbeCounter
    {
        public int Calls;
    }

    public sealed class ProbeHandlers(ProbeCounter counter) :
        IRequestHandler<CategoryProbeQuery, ServiceResult<int>>,
        IRequestHandler<WarehouseProbeQuery, ServiceResult<int>>
    {
        public Task<ServiceResult<int>> Handle(CategoryProbeQuery request, CancellationToken ct)
            => Task.FromResult(ServiceResult<int>.Success(++counter.Calls));

        public Task<ServiceResult<int>> Handle(WarehouseProbeQuery request, CancellationToken ct)
            => Task.FromResult(ServiceResult<int>.Success(++counter.Calls));
    }

    private ServiceProvider _provider = null!;
    private DBContext _context = null!;
    private ProbeCounter _counter = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(fixture.IsDockerAvailable, fixture.UnavailabilityReason ?? "Docker engine not available.");

        _counter = new ProbeCounter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_counter);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EfAutoEvictIntegrationTests>());
        services.AddMediatROutputCache(o => o.UseMemoryCache());
        _provider = services.BuildServiceProvider();

        var builder = new DbContextOptionsBuilder<DBContext>().UseNpgsql(fixture.ConnectionString);
        builder.UseMediatROutputCacheAutoEvict(_provider);

        _context = new DBContext(
            builder.Options,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _provider.DisposeAsync();
        await fixture.ResetAsync();
    }

    private async Task SaveNewCategoryAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = await new CategoryBuilder()
            .WithName($"Category-{suffix}")
            .WithSlug($"category-{suffix}")
            .BuildAsync();
        category.ClearDomainEvents();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task SavingTaggedEntity_EvictsCachedQuery()
    {
        var mediator = _provider.GetRequiredService<IMediator>();

        await mediator.Send(new CategoryProbeQuery());
        await mediator.Send(new CategoryProbeQuery());
        _counter.Calls.ShouldBe(1);

        await SaveNewCategoryAsync();

        var afterSave = await mediator.Send(new CategoryProbeQuery());
        afterSave.Value.ShouldBe(2);
        _counter.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task SavingUnrelatedEntity_KeepsCachedQuery()
    {
        var mediator = _provider.GetRequiredService<IMediator>();

        await mediator.Send(new WarehouseProbeQuery());
        await SaveNewCategoryAsync();
        await mediator.Send(new WarehouseProbeQuery());

        _counter.Calls.ShouldBe(1);
    }
}
