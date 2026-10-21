using Application.Common.Validation;

namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageValidator : AbstractValidator<CancelDiscountUsageCommand>
{

    public CancelDiscountUsageValidator()
    {
        this.RuleForRequiredId(x => x.DiscountCodeId);
        this.RuleForRequiredId(x => x.OrderId);
    }
}
