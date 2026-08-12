using Application.Attribute.Constants;
using Application.Attribute.Features.Commands.CreateAttributeValue;
using Application.Attribute.Features.Shared;
using Application.Cache.Contracts;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Commands.CreateAttributeValue;

public class CreateAttributeValueHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreateAttributeValueHandler _sut;

    public CreateAttributeValueHandlerTests()
    {
        _sut = new CreateAttributeValueHandler(_repository, _mapper, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenParentTypeNotFound_ReturnsNotFoundAndDoesNotPersistOrInvalidateCache()
    {
        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        var command = new CreateAttributeValueCommand(Guid.NewGuid(), "red", "Red", "#FF0000", 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);

        await _repository.DidNotReceiveWithAnyArgs().AttributeValueExistsAsync(default!, default!, default, default);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAttributeTypeAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenValueAlreadyExists_ReturnsConflictAndDoesNotPersistOrInvalidateCache()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        _repository
            .AttributeValueExistsAsync(
                Arg.Any<AttributeTypeId>(),
                Arg.Any<string>(),
                Arg.Any<AttributeValueId?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateAttributeValueCommand(type.Id.Value, "red", "Red", "#FF0000", 0);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);

        await _repository.DidNotReceiveWithAnyArgs().UpdateAttributeTypeAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenValid_AddsValueToAggregatePersistsAndInvalidatesCacheReturningMappedDto()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        _repository
            .AttributeValueExistsAsync(
                Arg.Any<AttributeTypeId>(),
                Arg.Any<string>(),
                Arg.Any<AttributeValueId?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var expectedDto = new AttributeValueDto { Value = "red", DisplayValue = "Red" };
        _mapper.Map<AttributeValueDto>(Arg.Any<object>()).Returns(expectedDto);

        var command = new CreateAttributeValueCommand(type.Id.Value, "red", "Red", "#FF0000", 4);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);

        type.Values.Count.ShouldBe(1);
        var added = type.Values.Single();
        added.Value.ShouldBe("red");
        added.DisplayValue.ShouldBe("Red");
        added.HexCode.ShouldBe("#FF0000");
        added.SortOrder.ShouldBe(4);

        await _repository.Received(1).UpdateAttributeTypeAsync(type, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChecksValueUniquenessAgainstParentTypeIdWithoutExcludeId()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        AttributeTypeId? capturedTypeId = null;
        string? capturedValue = null;
        AttributeValueId? capturedExcludeId = null;

        _repository
            .AttributeValueExistsAsync(
                Arg.Do<AttributeTypeId>(x => capturedTypeId = x),
                Arg.Do<string>(v => capturedValue = v),
                Arg.Do<AttributeValueId?>(x => capturedExcludeId = x),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateAttributeValueCommand(type.Id.Value, "red", "Red", null, 0);

        _ = await _sut.Handle(command, CancellationToken.None);

        capturedTypeId.ShouldBe(type.Id);
        capturedValue.ShouldBe("red");
        capturedExcludeId.ShouldBeNull();
    }
}
