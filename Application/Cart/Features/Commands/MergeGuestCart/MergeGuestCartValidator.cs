namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartValidator : AbstractValidator<MergeGuestCartCommand>
{
    public MergeGuestCartValidator()
    {
        RuleFor(x => x.GuestToken)
            .NotEmpty().WithMessage("توکن مهمان الزامی است.")
            .MaximumLength(256).WithMessage("توکن مهمان نامعتبر است.");
    }
}