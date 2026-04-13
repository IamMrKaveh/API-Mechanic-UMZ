using Domain.Discount.Enums;

namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountValidator : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("کد تخفیف الزامی است.")
            .MaximumLength(50);

        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("مقدار تخفیف باید بزرگتر از صفر باشد.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(x => x.StartsAt ?? DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("تاریخ انقضا باید بعد از تاریخ شروع باشد.");
    }
}