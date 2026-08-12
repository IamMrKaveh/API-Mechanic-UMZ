using Application.Attribute.Constants;
using Application.Attribute.Features.Commands.DeleteAttributeType;
using Application.Cache.Contracts;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Commands.DeleteAttributeType;

public class DeleteAttributeTypeHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly DeleteAttributeTypeHandler _sut;

    public DeleteAttributeTypeHandlerTests()
    {
        _sut = new DeleteAttributeTypeHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenTypeNotFound_ReturnsNotFoundAndDoesNotDeleteOrInvalidateCache()
    {
        _repository
            .GetAttributeTypeByIdAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        var result = await _sut.Handle(new DeleteAttributeTypeCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);

        await _repository.DidNotReceiveWithAnyArgs().DeleteAttributeTypeAsync(default!, default, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenTypeExists_DeletesByAggregateIdWithNullDeletedByAndInvalidatesAllTypesCache()
    {
        var existing = await new AttributeTypeBuilder().WithName("color").BuildAsync();

        _repository
            .GetAttributeTypeByIdAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.Handle(new DeleteAttributeTypeCommand(existing.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();

        await _repository.Received(1).DeleteAttributeTypeAsync(
            existing.Id,
            null,
            Arg.Any<CancellationToken>());

        await _cacheService.Received(1).RemoveAsync(AttributeCacheKeys.AllTypes, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesAttributeTypeIdBuiltFromRequestIdToRepositoryLookup()
    {
        var id = Guid.NewGuid();
        AttributeTypeId? captured = null;

        _repository
            .GetAttributeTypeByIdAsync(
                Arg.Do<AttributeTypeId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        _ = await _sut.Handle(new DeleteAttributeTypeCommand(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
