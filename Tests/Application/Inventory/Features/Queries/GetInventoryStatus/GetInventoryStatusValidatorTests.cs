using Application.Inventory.Features.Queries.GetInventoryStatus;

namespace Tests.Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusValidatorTests
{
    private readonly GetInventoryStatusValidator _sut = new();

    [Fact]
    public void Validate_WithNonEmptyVariantId_ReturnsIsValidTrue()
    {
        var query = new GetInventoryStatusQuery(Guid.NewGuid());

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyVariantId_ReturnsError()
    {
        var query = new GetInventoryStatusQuery(Guid.Empty);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetInventoryStatusQuery.VariantId));
    }
}
