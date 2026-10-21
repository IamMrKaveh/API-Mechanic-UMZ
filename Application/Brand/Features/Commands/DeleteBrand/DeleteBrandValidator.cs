using Application.Common.Validation;

namespace Application.Brand.Features.Commands.DeleteBrand;

public class DeleteBrandValidator : AbstractValidator<DeleteBrandCommand>
{

    public DeleteBrandValidator()
    {
        this.RuleForRequiredId(x => x.BrandId);
    }
}
