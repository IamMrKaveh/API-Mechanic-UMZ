namespace Application.Payment.Features.Commands.RefundPayment;

public class RefundPaymentValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0).WithMessage("شناسه تراکنش الزامی است.");

        RuleFor(x => x.AdminUserId)
            .GreaterThan(0).WithMessage("شناسه مدیر الزامی است.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("دلیل استرداد نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }
}