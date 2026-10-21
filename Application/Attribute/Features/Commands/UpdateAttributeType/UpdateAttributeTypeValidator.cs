using Application.Common.Validation;

namespace Application.Attribute.Features.Commands.UpdateAttributeType;

public class UpdateAttributeTypeValidator : AbstractValidator<UpdateAttributeTypeCommand>
{

    public UpdateAttributeTypeValidator()
    {
        this.RuleForRequiredId(x => x.Id);
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.DisplayName).MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x =>
            x.Name is not null || x.DisplayName is not null || x.SortOrder is not null || x.IsActive is not null)
            .WithMessage("حداقل یک فیلد برای به‌روزرسانی باید ارسال شود.");
    }
}
