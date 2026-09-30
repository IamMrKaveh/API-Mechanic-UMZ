using Application.Attribute.Features.Commands.DeleteAttributeValue;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Commands.DeleteAttributeValue;

public class DeleteAttributeValueHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly DeleteAttributeValueHandler _sut;

    public DeleteAttributeValueHandlerTests()
    {
        _sut = new DeleteAttributeValueHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenValueNotFound_ReturnsNotFoundAndDoesNotDelete()
    {
        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeValue?)null);

        var result = await _sut.Handle(new DeleteAttributeValueCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);

        await _repository.DidNotReceiveWithAnyArgs().DeleteAttributeValueAsync(default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenValueExists_DeletesByEntityIdWithNullDeletedBy()
    {
        var type = await new AttributeTypeBuilder().BuildAsync();
        var value = type.AddValue("red", "Red", DateTime.UtcNow);

        _repository
            .GetAttributeValueByIdAsync(Arg.Any<AttributeValueId>(), Arg.Any<CancellationToken>())
            .Returns(value);

        var result = await _sut.Handle(new DeleteAttributeValueCommand(value.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();

        await _repository.Received(1).DeleteAttributeValueAsync(
            value.Id,
            null,
            Arg.Any<CancellationToken>());

    }

    [Fact]
    public async Task Handle_PassesAttributeValueIdBuiltFromRequestIdToRepositoryLookup()
    {
        var id = Guid.NewGuid();
        AttributeValueId? captured = null;

        _repository
            .GetAttributeValueByIdAsync(
                Arg.Do<AttributeValueId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((AttributeValue?)null);

        _ = await _sut.Handle(new DeleteAttributeValueCommand(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
