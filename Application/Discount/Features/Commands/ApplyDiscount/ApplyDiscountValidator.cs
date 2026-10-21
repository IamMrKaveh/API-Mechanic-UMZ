using Application.Common.Validation;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public class ApplyDiscountValidator : AbstractValidator<ApplyDiscountCommand>
{

    public ApplyDiscountValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OrderAmount).GreaterThan(0);
        this.RuleForRequiredId(x => x.OrderId);
    }
}
