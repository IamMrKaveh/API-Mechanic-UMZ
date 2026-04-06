namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentValidator : AbstractValidator<VerifyPaymentCommand>
{
    public VerifyPaymentValidator()
    {
        RuleFor(x => x.Authority).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}