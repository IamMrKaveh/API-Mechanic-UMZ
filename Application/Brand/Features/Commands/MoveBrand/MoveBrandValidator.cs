namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandValidator : AbstractValidator<MoveBrandCommand>
{
    public MoveBrandValidator()
    {
        RuleFor(x => x.BrandId).NotEmpty().WithMessage("Brand ID is required.");
        RuleFor(x => x.TargetCategoryId).NotEmpty().WithMessage("Target category ID is required.");
    }
}