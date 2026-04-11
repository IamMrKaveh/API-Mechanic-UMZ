namespace Application.Cart.Features.Commands.UpdateCartItem;

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemCommand>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.GuestToken))
            .WithMessage("UserId یا GuestToken الزامی است.");

        RuleFor(x => x.GuestToken)
            .MinimumLength(8)
            .When(x => !string.IsNullOrWhiteSpace(x.GuestToken))
            .WithMessage("توکن مهمان باید حداقل ۸ کاراکتر باشد.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("شناسه واریانت معتبر نیست.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("تعداد نمی‌تواند منفی باشد.")
            .LessThanOrEqualTo(1000).WithMessage("حداکثر تعداد مجاز ۱۰۰۰ عدد است.");
    }
}