using Application.Media.Features.Commands.ReorderMedia;

namespace Tests.Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaValidatorTests
{
    private readonly ReorderMediaValidator _sut = new();

    [Fact]
    public void Validate_WhenAllFieldsProvided_IsValid()
    {
        var command = new ReorderMediaCommand(
            "Product",
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenEntityTypeIsEmpty_IsInvalidOnEntityType()
    {
        var command = new ReorderMediaCommand(
            string.Empty,
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid() });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReorderMediaCommand.EntityType));
    }

    [Fact]
    public void Validate_WhenEntityIdIsEmpty_IsInvalidOnEntityId()
    {
        var command = new ReorderMediaCommand(
            "Product",
            Guid.Empty,
            new List<Guid> { Guid.NewGuid() });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReorderMediaCommand.EntityId));
    }

    [Fact]
    public void Validate_WhenOrderedIdsIsEmpty_IsInvalidOnOrderedIds()
    {
        var command = new ReorderMediaCommand(
            "Product",
            Guid.NewGuid(),
            new List<Guid>());

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(ReorderMediaCommand.OrderedIds));
    }
}
