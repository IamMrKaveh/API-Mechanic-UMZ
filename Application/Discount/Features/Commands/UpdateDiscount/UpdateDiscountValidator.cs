using Domain.Discount.Enums;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountValidator : AbstractValidator<UpdateDiscountCommand>
{
    public UpdateDiscountValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("شناسه کد تخفیف الزامی است.");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .When(x => x.DiscountType != DiscountType.FreeShipping)
            .WithMessage("مقدار تخفیف باید بزرگتر از صفر باشد.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(x => x.StartsAt ?? dateTimeProvider.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("تاریخ انقضا باید بعد از تاریخ شروع باشد.");
    }
}