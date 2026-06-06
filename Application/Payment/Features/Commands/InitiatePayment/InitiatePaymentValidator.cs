namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentValidator : AbstractValidator<InitiatePaymentCommand>
{
    public InitiatePaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}