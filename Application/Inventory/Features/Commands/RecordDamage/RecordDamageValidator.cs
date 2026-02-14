namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageValidator : AbstractValidator<RecordDamageCommand>
{
    public RecordDamageValidator()
    {
        RuleFor(x => x.VariantId)
            .GreaterThan(0)
            .WithMessage("شناسه واریانت باید بزرگتر از صفر باشد.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("تعداد خسارت باید بزرگتر از صفر باشد.");

        RuleFor(x => x.Notes)
            .NotEmpty()
            .WithMessage("توضیحات خسارت الزامی است.")
            .MaximumLength(500)
            .WithMessage("توضیحات نمی‌تواند بیش از 500 کاراکتر باشد.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("شناسه کاربر باید بزرگتر از صفر باشد.");
    }
}