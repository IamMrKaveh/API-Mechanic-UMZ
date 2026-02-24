namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentValidator : AbstractValidator<InitiatePaymentCommand>
{
    public InitiatePaymentValidator()
    {
        RuleFor(x => x.Dto.OrderId)
            .GreaterThan(0).WithMessage("شناسه سفارش الزامی است.");

        RuleFor(x => x.Dto.Amount.Amount)
            .GreaterThan(0).WithMessage("مبلغ پرداخت باید بزرگتر از صفر باشد.");

        RuleFor(x => x.Dto.Description)
            .NotEmpty().WithMessage("توضیحات الزامی است.")
            .MaximumLength(500).WithMessage("توضیحات نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

        RuleFor(x => x.Dto.CallbackUrl)
            .NotEmpty().WithMessage("آدرس بازگشت الزامی است.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("شناسه کاربر الزامی است.");
    }
}