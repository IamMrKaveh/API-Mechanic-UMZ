namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentValidator : AbstractValidator<AtomicRefundPaymentCommand>
{
    public AtomicRefundPaymentValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("شناسه سفارش الزامی است.");

        RuleFor(x => x.RequestedByUserId)
            .GreaterThan(0).WithMessage("شناسه مدیر الزامی است.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("دلیل استرداد الزامی است.")
            .MaximumLength(500).WithMessage("دلیل استرداد نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.PartialAmount)
            .GreaterThan(0).When(x => x.PartialAmount.HasValue).WithMessage("مبلغ استرداد نامعتبر است.");
    }
}