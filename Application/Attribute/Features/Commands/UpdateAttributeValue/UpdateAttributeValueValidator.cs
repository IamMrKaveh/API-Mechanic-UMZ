using Application.Common.Validation;

namespace Application.Attribute.Features.Commands.UpdateAttributeValue;

public class UpdateAttributeValueValidator : AbstractValidator<UpdateAttributeValueCommand>
{

    public UpdateAttributeValueValidator()
    {
        this.RuleForRequiredId(x => x.Id);
        RuleFor(x => x.Value).MaximumLength(100);
        RuleFor(x => x.DisplayValue).MaximumLength(200);
        RuleFor(x => x.HexCode).MaximumLength(7);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x =>
            x.Value is not null || x.DisplayValue is not null || x.HexCode is not null
            || x.SortOrder is not null || x.IsActive is not null)
            .WithMessage("حداقل یک فیلد برای به‌روزرسانی باید ارسال شود.");
    }
}
