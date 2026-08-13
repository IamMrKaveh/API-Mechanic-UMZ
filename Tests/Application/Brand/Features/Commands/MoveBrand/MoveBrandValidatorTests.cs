using Application.Brand.Features.Commands.MoveBrand;

namespace Tests.Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandValidatorTests
{
    private readonly MoveBrandValidator _sut = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(new MoveBrandCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyBrandId_IsInvalid()
    {
        var result = _sut.Validate(new MoveBrandCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MoveBrandCommand.BrandId));
    }

    [Fact]
    public void Validate_WithEmptyTargetCategoryId_IsInvalid()
    {
        var result = _sut.Validate(new MoveBrandCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MoveBrandCommand.TargetCategoryId));
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ReturnsErrorsForBothProperties()
    {
        var result = _sut.Validate(new MoveBrandCommand(Guid.Empty, Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MoveBrandCommand.BrandId));
        result.Errors.ShouldContain(e => e.PropertyName == nameof(MoveBrandCommand.TargetCategoryId));
    }
}
