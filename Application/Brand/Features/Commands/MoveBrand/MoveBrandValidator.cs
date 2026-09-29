using Application.Common.Validation;

namespace Application.Brand.Features.Commands.MoveBrand;

public class MoveBrandValidator : AbstractValidator<MoveBrandCommand>
{
    public MoveBrandValidator()
    {
        this.RuleForRequiredId(x => x.BrandId, "Brand ID is required.");
        this.RuleForRequiredId(x => x.TargetCategoryId, "Target category ID is required.");
    }
}