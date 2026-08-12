using Application.Media.Features.Queries.GetEntityMedia;

namespace Tests.Application.Media.Features.Queries.GetEntityMedia;

public class GetEntityMediaValidatorTests
{
    private readonly GetEntityMediaValidator _sut = new();

    [Fact]
    public void Validate_WhenAllFieldsProvided_IsValid()
    {
        var query = new GetEntityMediaQuery("Product", Guid.NewGuid());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WhenEntityTypeIsEmpty_IsInvalidOnEntityType()
    {
        var query = new GetEntityMediaQuery(string.Empty, Guid.NewGuid());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetEntityMediaQuery.EntityType));
    }

    [Fact]
    public void Validate_WhenEntityIdIsEmpty_IsInvalidOnEntityId()
    {
        var query = new GetEntityMediaQuery("Product", Guid.Empty);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetEntityMediaQuery.EntityId));
    }
}
