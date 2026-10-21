using Application.Common.Validation;

namespace Application.Discount.Features.Commands.DeleteDiscount;

public class DeleteDiscountValidator : AbstractValidator<DeleteDiscountCommand>
{

    public DeleteDiscountValidator()
    {
        this.RuleForRequiredId(x => x.Id);
    }
}
