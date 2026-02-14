namespace Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountValidator : AbstractValidator<CreateDiscountCommand>
{
    public CreateDiscountValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(20)
            .Matches(@"^[A-Za-z0-9\-_]+$")
            .WithMessage("کد تخفیف فقط می‌تواند شامل حروف، اعداد، خط تیره و زیرخط باشد.");

        RuleFor(x => x.Percentage)
            .InclusiveBetween(0.01m, 100m);

        RuleFor(x => x.MaxDiscountAmount)
            .GreaterThan(0)
            .When(x => x.MaxDiscountAmount.HasValue);

        RuleFor(x => x.MinOrderAmount)
            .GreaterThan(0)
            .When(x => x.MinOrderAmount.HasValue);

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0)
            .When(x => x.UsageLimit.HasValue);

        RuleFor(x => x.MaxUsagePerUser)
            .GreaterThan(0)
            .When(x => x.MaxUsagePerUser.HasValue);

        RuleFor(x => x.ExpiresAt)
            .GreaterThan(x => x.StartsAt)
            .When(x => x.ExpiresAt.HasValue && x.StartsAt.HasValue)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }
}