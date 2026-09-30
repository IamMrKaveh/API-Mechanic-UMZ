using Application.Cache.Contracts;
using Application.Category.Features.Queries.GetPublicCategories;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProductDetails;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;
using Shouldly;
using SharedKernel.Results;

namespace Tests.Infrastructure.Cache.OutputCache;

public class MediatROutputCacheTests
{
    [RequestOutputCache(tags: [CacheTags.Product], expirationInSeconds: 60)]
    public sealed record CachedProbeQuery(int Id) : IRequest<ServiceResult<string>>;

    public sealed record UncachedProbeQuery(int Id) : IRequest<ServiceResult<string>>;

    [RequestOutputCacheEvict(CacheTags.Product)]
    public sealed record EvictingProbeCommand : IRequest<ServiceResult<string>>;

    public sealed class ProbeCounter
    {
        public int Calls;
        public bool FailNext;
    }

    public sealed class ProbeHandlers(ProbeCounter counter) :
        IRequestHandler<CachedProbeQuery, ServiceResult<string>>,
        IRequestHandler<UncachedProbeQuery, ServiceResult<string>>,
        IRequestHandler<EvictingProbeCommand, ServiceResult<string>>
    {
        public Task<ServiceResult<string>> Handle(CachedProbeQuery request, CancellationToken ct)
            => Next($"cached-{request.Id}");

        public Task<ServiceResult<string>> Handle(UncachedProbeQuery request, CancellationToken ct)
            => Next($"uncached-{request.Id}");

        public Task<ServiceResult<string>> Handle(EvictingProbeCommand request, CancellationToken ct)
            => Task.FromResult(counter.FailNext
                ? ServiceResult<string>.Failure("nope")
                : ServiceResult<string>.Success("done"));

        private Task<ServiceResult<string>> Next(string value)
        {
            counter.Calls++;
            return Task.FromResult(counter.FailNext
                ? ServiceResult<string>.Failure("boom")
                : ServiceResult<string>.Success($"{value}#{counter.Calls}"));
        }
    }

    private static (IMediator Mediator, ProbeCounter Counter, IServiceProvider Provider) CreateHost()
    {
        var counter = new ProbeCounter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(counter);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatROutputCacheTests>());
        services.AddMediatROutputCache(o => o.UseMemoryCache());

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IMediator>(), counter, provider);
    }

    [Fact]
    public async Task CachedQuery_SecondIdenticalRequest_SkipsHandler()
    {
        var (mediator, counter, _) = CreateHost();

        var first = await mediator.Send(new CachedProbeQuery(1));
        var second = await mediator.Send(new CachedProbeQuery(1));

        counter.Calls.ShouldBe(1);
        second.Value.ShouldBe(first.Value);
    }

    [Fact]
    public async Task CachedQuery_DifferentPayload_HasSeparateEntries()
    {
        var (mediator, counter, _) = CreateHost();

        await mediator.Send(new CachedProbeQuery(1));
        await mediator.Send(new CachedProbeQuery(2));

        counter.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task UnmarkedQuery_IsNeverCached()
    {
        var (mediator, counter, _) = CreateHost();

        await mediator.Send(new UncachedProbeQuery(1));
        await mediator.Send(new UncachedProbeQuery(1));

        counter.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task FailedResult_IsNotCached()
    {
        var (mediator, counter, _) = CreateHost();
        counter.FailNext = true;

        await mediator.Send(new CachedProbeQuery(1));
        counter.FailNext = false;
        var second = await mediator.Send(new CachedProbeQuery(1));

        counter.Calls.ShouldBe(2);
        second.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EvictCommand_Success_InvalidatesTaggedQueries()
    {
        var (mediator, counter, _) = CreateHost();

        await mediator.Send(new CachedProbeQuery(1));
        await mediator.Send(new EvictingProbeCommand());
        await mediator.Send(new CachedProbeQuery(1));

        counter.Calls.ShouldBe(2);
    }


    [Fact]
    public async Task ManualEviction_ByTag_InvalidatesTaggedQueries()
    {
        var (mediator, counter, provider) = CreateHost();

        await mediator.Send(new CachedProbeQuery(1));
        var invalidator = provider.GetRequiredService<IRequestOutputCacheInvalidator>();
        var evicted = await invalidator.EvictByTagsAsync([CacheTags.Product]);
        await mediator.Send(new CachedProbeQuery(1));

        evicted.IsSuccess.ShouldBeTrue();
        counter.Calls.ShouldBe(2);
    }

    [Theory]
    [InlineData(typeof(GetProductCatalogQuery), new[] { CacheTags.Product, CacheTags.ProductVariant, CacheTags.Category, CacheTags.Brand, CacheTags.Inventory, CacheTags.Media })]
    [InlineData(typeof(GetProductDetailsQuery), new[] { CacheTags.Product, CacheTags.ProductVariant, CacheTags.Category, CacheTags.Brand, CacheTags.Inventory, CacheTags.Media })]
    [InlineData(typeof(GetPublicCategoriesQuery), new[] { CacheTags.Category, CacheTags.Media })]
    public void PublicCatalogQueries_AreOptedIntoOutputCache_WithEntityTags(Type queryType, string[] expectedTags)
    {
        var attribute = queryType
            .GetCustomAttributes(typeof(RequestOutputCacheAttribute), inherit: false)
            .Cast<RequestOutputCacheAttribute>()
            .Single();

        attribute.Tags.ShouldBe(expectedTags, ignoreOrder: true);
    }

    [Fact]
    public void CacheTags_MatchEfEntityTypeNames()
    {
        CacheTags.Product.ShouldBe("Product");
        CacheTags.ProductVariant.ShouldBe("ProductVariant");
        CacheTags.Category.ShouldBe("Category");
        CacheTags.Brand.ShouldBe("Brand");
        CacheTags.Inventory.ShouldBe("Inventory");
    }
}
