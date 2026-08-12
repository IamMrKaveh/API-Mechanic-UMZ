using Application.Attribute.Constants;
using Application.Attribute.Features.Commands.UpdateAttributeValue;
using Application.Cache.Contracts;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Commands.UpdateAttributeValue;

public class UpdateAttributeValueHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly UpdateAttributeValueHandler _sut;

    public UpdateAttributeValueHandlerTests()
    {
        _sut = new UpdateAttributeValueHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenValueNotFound_ReturnsNotFoundAndDoesNothingElse()
    {
        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeValue?)null);

        var result = await _sut.Handle(
            new UpdateAttributeValueCommand(Guid.NewGuid(), "red", "Red", "#FF0000", 1, true),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);

        await _repository.DidNotReceiveWithAnyArgs().AttributeValueExistsAsync(default!, default!, default, default);
        await _repository.DidNotReceiveWithAnyArgs().GetAttributeTypeWithValuesAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAttributeTypeAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenNewValueIsDuplicate_ReturnsConflictAndDoesNotLoadParentOrPersist()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();
        var existingValue = type.AddValue("red", "Red");

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(existingValue);

        _repository
            .AttributeValueExistsAsync(
                Arg.Any<AttributeTypeId>(),
                Arg.Any<string>(),
                Arg.Any<AttributeValueId?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new UpdateAttributeValueCommand(existingValue.Id.Value, "blue", "Blue", null, null, null),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);

        await _repository.DidNotReceiveWithAnyArgs().GetAttributeTypeWithValuesAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAttributeTypeAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenValueProvided_ChecksUniquenessWithParentTypeIdAndCurrentValueIdAsExcludeId()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();
        var existingValue = type.AddValue("red", "Red");

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(existingValue);

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

        _ = await _sut.Handle(
            new UpdateAttributeValueCommand(existingValue.Id.Value, "crimson", null, null, null, null),
            CancellationToken.None);

        capturedTypeId.ShouldBe(type.Id);
        capturedValue.ShouldBe("crimson");
        capturedExcludeId.ShouldBe(existingValue.Id);
    }

    [Fact]
    public async Task Handle_WhenValueOmitted_SkipsUniquenessCheck()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();
        var existingValue = type.AddValue("red", "Red");

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(existingValue);

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        var result = await _sut.Handle(
            new UpdateAttributeValueCommand(existingValue.Id.Value, null, "Rojo", null, null, null),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _repository.DidNotReceiveWithAnyArgs().AttributeValueExistsAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenParentTypeNotFound_ReturnsNotFoundAndDoesNotPersist()
    {
        var isolatedType = await new AttributeTypeBuilder().BuildAsync();
        var existingValue = isolatedType.AddValue("red", "Red");

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(existingValue);

        _repository
            .AttributeValueExistsAsync(
                Arg.Any<AttributeTypeId>(),
                Arg.Any<string>(),
                Arg.Any<AttributeValueId?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        var result = await _sut.Handle(
            new UpdateAttributeValueCommand(existingValue.Id.Value, "crimson", "Crimson", null, null, null),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);

        await _repository.DidNotReceiveWithAnyArgs().UpdateAttributeTypeAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenValid_AppliesUpdateOnAggregatePersistsAndInvalidatesCache()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();
        var existingValue = type.AddValue("red", "Red", "#FF0000", 1);

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(existingValue);

        _repository
            .AttributeValueExistsAsync(
                Arg.Any<AttributeTypeId>(),
                Arg.Any<string>(),
                Arg.Any<AttributeValueId?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        var result = await _sut.Handle(
            new UpdateAttributeValueCommand(existingValue.Id.Value, "crimson", "Crimson", "#DC143C", 2, false),
            CancellationToken.None);

        result.ShouldBeSuccess();

        existingValue.Value.ShouldBe("crimson");
        existingValue.DisplayValue.ShouldBe("Crimson");
        existingValue.HexCode.ShouldBe("#DC143C");
        existingValue.SortOrder.ShouldBe(2);
        existingValue.IsActive.ShouldBeFalse();

        await _repository.Received(1).UpdateAttributeTypeAsync(type, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, null, null, null, null)]
    [InlineData("crimson", null, null, null, null)]
    [InlineData(null, "Crimson", null, null, null)]
    [InlineData(null, null, "#DC143C", null, null)]
    [InlineData(null, null, null, 9, null)]
    [InlineData(null, null, null, null, false)]
    public async Task Handle_WithNullFields_FallsBackToExistingValues(
        string? value, string? displayValue, string? hexCode, int? sortOrder, bool? isActive)
    {
        var type = await new AttributeTypeBuilder().BuildAsync();
        var existingValue = type.AddValue("red", "Red", "#FF0000", 1);
        existingValue.Update("red", "Red", "#FF0000", 1, true);

        var originalValue = existingValue.Value;
        var originalDisplay = existingValue.DisplayValue;
        var originalHex = existingValue.HexCode;
        var originalSort = existingValue.SortOrder;
        var originalActive = existingValue.IsActive;

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(existingValue);

        _repository
            .AttributeValueExistsAsync(
                Arg.Any<AttributeTypeId>(),
                Arg.Any<string>(),
                Arg.Any<AttributeValueId?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        var result = await _sut.Handle(
            new UpdateAttributeValueCommand(existingValue.Id.Value, value, displayValue, hexCode, sortOrder, isActive),
            CancellationToken.None);

        result.ShouldBeSuccess();
        existingValue.Value.ShouldBe(value ?? originalValue);
        existingValue.DisplayValue.ShouldBe(displayValue ?? originalDisplay);
        existingValue.HexCode.ShouldBe(hexCode ?? originalHex);
        existingValue.SortOrder.ShouldBe(sortOrder ?? originalSort);
        existingValue.IsActive.ShouldBe(isActive ?? originalActive);
    }
}
