using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageValidator : AbstractValidator<RecordDamageCommand>
{
    public RecordDamageValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("مقدار باید بیشتر از صفر باشد.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("دلیل ثبت ضایعات الزامی است.");
    }
}