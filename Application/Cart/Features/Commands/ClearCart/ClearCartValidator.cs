namespace Application.Cart.Features.Commands.ClearCart;

public class ClearCartValidator : AbstractValidator<ClearCartCommand>
{
    public ClearCartValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.GuestToken))
            .WithMessage("UserId یا GuestToken الزامی است.");

        RuleFor(x => x.GuestToken)
            .MinimumLength(8)
            .When(x => !string.IsNullOrWhiteSpace(x.GuestToken))
            .WithMessage("توکن مهمان باید حداقل ۸ کاراکتر باشد.");
    }
}