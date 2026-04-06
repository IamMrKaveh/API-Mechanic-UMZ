namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageValidator : AbstractValidator<RecordDamageCommand>
{
    public RecordDamageValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("مقدار باید بیشتر از صفر باشد.");
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}