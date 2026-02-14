namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentValidator : AbstractValidator<VerifyPaymentCommand>
{
    public VerifyPaymentValidator()
    {
        RuleFor(x => x.Authority)
            .NotEmpty().WithMessage("شناسه پرداخت الزامی است.")
            .MaximumLength(100).WithMessage("شناسه پرداخت نامعتبر است.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("وضعیت درگاه الزامی است.");
    }
}