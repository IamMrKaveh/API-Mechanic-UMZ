using Application.Attribute.Constants;
using Application.Attribute.Features.Commands.UpdateAttributeType;
using Application.Cache.Contracts;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly UpdateAttributeTypeHandler _sut;

    public UpdateAttributeTypeHandlerTests()
    {
        _repository
            .AttributeTypeExistsAsync(Arg.Any<string>(), Arg.Any<AttributeTypeId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _sut = new UpdateAttributeTypeHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenTypeNotFound_ReturnsNotFoundAndDoesNotPersistOrInvalidateCache()
    {
        _repository
            .GetAttributeTypeByIdAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        var result = await _sut.Handle(
            new UpdateAttributeTypeCommand(Guid.NewGuid(), "n", "d", 1, true),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);

        await _repository.DidNotReceiveWithAnyArgs().UpdateAttributeTypeAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WithAllFieldsProvided_AppliesChangesPersistsAndInvalidatesCache()
    {
        var existing = await new AttributeTypeBuilder().WithName("color").WithSortOrder(0).WithIsActive(true).BuildAsync();

        _repository
            .GetAttributeTypeByIdAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.Handle(
            new UpdateAttributeTypeCommand(existing.Id.Value, "size", "Size", 9, false),
            CancellationToken.None);

        result.ShouldBeSuccess();
        existing.Name.ShouldBe("size");
        existing.DisplayName.ShouldBe("Size");
        existing.SortOrder.ShouldBe(9);
        existing.IsActive.ShouldBeFalse();

        await _repository.Received(1).UpdateAttributeTypeAsync(existing, Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("newname", null, null, null)]
    [InlineData(null, "newdisplay", null, null)]
    [InlineData(null, null, 5, null)]
    [InlineData(null, null, null, false)]
    public async Task Handle_WithNullFields_FallsBackToExistingAggregateValues(
        string? name, string? displayName, int? sortOrder, bool? isActive)
    {
        var existing = await new AttributeTypeBuilder()
            .WithName("color")
            .WithDisplayName("Color")
            .WithSortOrder(3)
            .WithIsActive(true)
            .BuildAsync();

        var originalName = existing.Name;
        var originalDisplayName = existing.DisplayName;
        var originalSortOrder = existing.SortOrder;
        var originalIsActive = existing.IsActive;

        _repository
            .GetAttributeTypeByIdAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.Handle(
            new UpdateAttributeTypeCommand(existing.Id.Value, name, displayName, sortOrder, isActive),
            CancellationToken.None);

        result.ShouldBeSuccess();
        existing.Name.ShouldBe(name ?? originalName);
        existing.DisplayName.ShouldBe(displayName ?? originalDisplayName);
        existing.SortOrder.ShouldBe(sortOrder ?? originalSortOrder);
        existing.IsActive.ShouldBe(isActive ?? originalIsActive);
    }

    [Fact]
    public async Task Handle_WhenNameChanged_QueriesUniquenessCheckWithCurrentIdAsExcludeId()
    {
        var existing = await new AttributeTypeBuilder().WithName("color").BuildAsync();

        _repository
            .GetAttributeTypeByIdAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        string? capturedName = null;
        AttributeTypeId? capturedExcludeId = null;

        _repository
            .AttributeTypeExistsAsync(
                Arg.Do<string>(n => capturedName = n),
                Arg.Do<AttributeTypeId?>(x => capturedExcludeId = x),
                Arg.Any<CancellationToken>())
            .Returns(false);

        _ = await _sut.Handle(
            new UpdateAttributeTypeCommand(existing.Id.Value, "size", null, null, null),
            CancellationToken.None);

        capturedName.ShouldBe("size");
        capturedExcludeId.ShouldBe(existing.Id);
    }
}
