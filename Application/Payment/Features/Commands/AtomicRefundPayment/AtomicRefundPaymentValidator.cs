namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentValidator : AbstractValidator<AtomicRefundPaymentCommand>
{
    public AtomicRefundPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}