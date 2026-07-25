using Domain.Discount.Enums;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountValidator : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithName("کد تخفیف").WithMessage("کد تخفیف الزامی است.")
            .MaximumLength(50).WithName("کد تخفیف").WithMessage("طول کد تخفیف نمی‌تواند بیش از ۵۰ کاراکتر باشد.");

        RuleFor(x => x.DiscountType)
            .IsInEnum().WithName("نوع تخفیف").WithMessage("نوع تخفیف نامعتبر است.");

        RuleFor(x => x.Value)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .When(x => x.DiscountType != DiscountType.FreeShipping)
            .WithName("مقدار تخفیف")
            .WithMessage("مقدار تخفیف باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithName("مقدار تخفیف")
            .WithMessage("درصد تخفیف نمی‌تواند بیش از ۱۰۰ باشد.");

        RuleFor(x => x.Value)
            .Equal(0)
            .When(x => x.DiscountType == DiscountType.FreeShipping)
            .WithName("مقدار تخفیف")
            .WithMessage("در حالت ارسال رایگان، مقدار تخفیف باید صفر باشد.");

        RuleFor(x => x.MaximumDiscountAmount)
            .GreaterThan(0)
            .When(x => x.MaximumDiscountAmount.HasValue)
            .WithName("سقف مبلغ تخفیف")
            .WithMessage("سقف مبلغ تخفیف باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0)
            .When(x => x.UsageLimit.HasValue)
            .WithName("سقف تعداد استفاده")
            .WithMessage("سقف تعداد استفاده باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(x => x.StartsAt ?? dateTimeProvider.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithName("تاریخ انقضا")
            .WithMessage("تاریخ انقضا باید بعد از تاریخ شروع باشد.");
    }
}
