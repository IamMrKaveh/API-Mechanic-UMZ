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
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly GetAllAttributeTypesHandler _sut;

    public GetAllAttributeTypesHandlerTests()
    {
        _sut = new GetAllAttributeTypesHandler(_repository, _mapper);
    }

    [Fact]
    public void Query_ImplementsCacheableQueryWithExpectedKeyAndExpiry()
    {
        var query = new GetAllAttributeTypesQuery();

        ((ICacheableQuery)query).CacheKey.ShouldBe(AttributeCacheKeys.AllTypes);
        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task Handle_MapsRepositoryTypesToPagedResult()
    {
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
        await _repository.Received(1).GetAllAttributeTypesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsNoItems_ReturnsSuccessWithZeroTotalAndPageSizeOne()
    {
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
