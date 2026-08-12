using Application.Attribute.Constants;
using Application.Attribute.Features.Queries.GetAllAttributeTypes;
using Application.Attribute.Features.Shared;
using Application.Cache.Contracts;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Queries.GetAllAttributeTypes;

public class GetAllAttributeTypesHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly GetAllAttributeTypesHandler _sut;

    public GetAllAttributeTypesHandlerTests()
    {
        _sut = new GetAllAttributeTypesHandler(_repository, _mapper, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenCachedResultHasItems_ReturnsCachedWithoutHittingRepository()
    {
        var cached = new PaginatedResult<AttributeTypeDto>(
            new List<AttributeTypeDto> { new() { Name = "color" } },
            totalCount: 1,
            page: 1,
            pageSize: 1);

        _cacheService
            .GetAsync<PaginatedResult<AttributeTypeDto>>(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _sut.Handle(new GetAllAttributeTypesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _repository.DidNotReceiveWithAnyArgs().GetAllAttributeTypesAsync(default);
        await _cacheService.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceiveWithAnyArgs().SetAsync<PaginatedResult<AttributeTypeDto>>(
            default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCachedResultIsEmpty_RemovesCacheKeyAndReloadsFromRepository()
    {
        var emptyCached = new PaginatedResult<AttributeTypeDto>(
            new List<AttributeTypeDto>(),
            totalCount: 0,
            page: 1,
            pageSize: 1);

        _cacheService
            .GetAsync<PaginatedResult<AttributeTypeDto>>(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>())
            .Returns(emptyCached);

        var typeA = await new AttributeTypeBuilder().WithName("color").BuildAsync();
        var typeB = await new AttributeTypeBuilder().WithName("size").BuildAsync();
        var typesFromRepo = new List<AttributeType> { typeA, typeB };

        _repository
            .GetAllAttributeTypesAsync(Arg.Any<CancellationToken>())
            .Returns(typesFromRepo);

        var mapped = new List<AttributeTypeDto>
    {
        new() { Name = "color" },
        new() { Name = "size" }
    };

        _mapper.Map<List<AttributeTypeDto>>(Arg.Any<object>()).Returns(mapped);

        var result = await _sut.Handle(new GetAllAttributeTypesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(2);

        await _cacheService.Received(1).RemoveAsync(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMisses_ReadsFromRepositoryAndStoresResultForOneHour()
    {
        _cacheService
            .GetAsync<PaginatedResult<AttributeTypeDto>>(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>())
            .Returns((PaginatedResult<AttributeTypeDto>?)null);

        var typeA = await new AttributeTypeBuilder().WithName("color").BuildAsync();
        _repository
            .GetAllAttributeTypesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AttributeType> { typeA });

        var mapped = new List<AttributeTypeDto> { new() { Name = "color" } };
        _mapper.Map<List<AttributeTypeDto>>(Arg.Any<object>()).Returns(mapped);

        TimeSpan? capturedExpiry = null;
        PaginatedResult<AttributeTypeDto>? capturedValue = null;
        string? capturedKey = null;

        await _cacheService.SetAsync(
            Arg.Do<string>(k => capturedKey = k),
            Arg.Do<PaginatedResult<AttributeTypeDto>>(v => capturedValue = v),
            Arg.Do<TimeSpan?>(t => capturedExpiry = t),
            Arg.Any<CancellationToken>());

        var result = await _sut.Handle(new GetAllAttributeTypesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.TotalCount.ShouldBe(1);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);

        capturedKey.ShouldBe(AttributeCacheKeys.AllTypes);
        capturedValue.ShouldNotBeNull();
        capturedValue!.TotalCount.ShouldBe(1);
        capturedExpiry.ShouldBe(TimeSpan.FromHours(1));

        await _cacheService.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsNoItems_ReturnsSuccessWithZeroTotalAndPageSizeOne()
    {
        _cacheService
            .GetAsync<PaginatedResult<AttributeTypeDto>>(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>())
            .Returns((PaginatedResult<AttributeTypeDto>?)null);

        _repository
            .GetAllAttributeTypesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AttributeType>());

        _mapper.Map<List<AttributeTypeDto>>(Arg.Any<object>()).Returns(new List<AttributeTypeDto>());

        var result = await _sut.Handle(new GetAllAttributeTypesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
        result.Value.Page.ShouldBe(1);
        result.Value.PageSize.ShouldBe(1);
    }
}
