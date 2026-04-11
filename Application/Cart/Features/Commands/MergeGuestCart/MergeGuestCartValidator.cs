namespace Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartValidator : AbstractValidator<MergeGuestCartCommand>
{
    public MergeGuestCartValidator()
    {
        RuleFor(x => x.GuestToken)
            .NotEmpty().WithMessage("توکن مهمان الزامی است.")
            .MinimumLength(8).WithMessage("توکن مهمان باید حداقل ۸ کاراکتر باشد.");
    }
}