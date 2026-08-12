using Application.Attribute.Constants;
using Application.Attribute.Features.Commands.CreateAttributeType;
using Application.Attribute.Features.Shared;
using Application.Cache.Contracts;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Attribute.Features.Commands.CreateAttributeType;

public class CreateAttributeTypeHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreateAttributeTypeHandler _sut;

    public CreateAttributeTypeHandlerTests()
    {
        _repository
            .AttributeTypeExistsAsync(Arg.Any<string>(), Arg.Any<AttributeTypeId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sut = new CreateAttributeTypeHandler(_repository, _mapper, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenValid_AddsAggregateInvalidatesCacheAndReturnsMappedDto()
    {
        var command = new CreateAttributeTypeCommand("color", "Color", 3);
        var expectedDto = new AttributeTypeDto { Name = "color", DisplayName = "Color", SortOrder = 3 };
        _mapper.Map<AttributeTypeDto>(Arg.Any<object>()).Returns(expectedDto);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);

        await _repository.Received(1).AddAttributeTypeAsync(Arg.Any<AttributeType>(), Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_QueriesRepositoryUniquenessCheckWithTrimmedNameAndNoExcludeId()
    {
        var command = new CreateAttributeTypeCommand("  color  ", "  Color  ", 1);
        string? capturedName = null;
        AttributeTypeId? capturedExcludeId = null;

        _repository
            .AttributeTypeExistsAsync(
                Arg.Do<string>(n => capturedName = n),
                Arg.Do<AttributeTypeId?>(x => capturedExcludeId = x),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _ = await _sut.Handle(command, CancellationToken.None);

        capturedName.ShouldBe("color");
        capturedExcludeId.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_PersistsAggregateBuiltFromCommandInputs()
    {
        var command = new CreateAttributeTypeCommand("color", "Color", 7);
        AttributeType? captured = null;

        await _repository
            .AddAttributeTypeAsync(
                Arg.Do<AttributeType>(x => captured = x),
                Arg.Any<CancellationToken>());

        _mapper.Map<AttributeTypeDto>(Arg.Any<object>()).Returns(new AttributeTypeDto());

        _ = await _sut.Handle(command, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Name.ShouldBe("color");
        captured.DisplayName.ShouldBe("Color");
        captured.SortOrder.ShouldBe(7);
        captured.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_MapsPersistedAggregateThroughInjectedMapper()
    {
        var command = new CreateAttributeTypeCommand("color", "Color", 0);
        object? capturedForMap = null;

        _mapper
            .Map<AttributeTypeDto>(Arg.Do<object>(x => capturedForMap = x))
            .Returns(new AttributeTypeDto());

        AttributeType? addedAggregate = null;
        await _repository
            .AddAttributeTypeAsync(Arg.Do<AttributeType>(x => addedAggregate = x), Arg.Any<CancellationToken>());

        _ = await _sut.Handle(command, CancellationToken.None);

        addedAggregate.ShouldNotBeNull();
        capturedForMap.ShouldBeSameAs(addedAggregate);
    }
}
