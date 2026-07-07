using FluentValidation;

namespace Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed class RequestWithdrawalValidator : AbstractValidator<RequestWithdrawalCommand>
{
    public RequestWithdrawalValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ باید بزرگتر از صفر باشد.")
            .GreaterThanOrEqualTo(50_000m).WithMessage("حداقل مبلغ برداشت ۵۰,۰۰۰ تومان است.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.Iban)
            .NotEmpty().WithMessage("شماره شبا الزامی است.")
            .MaximumLength(32).WithMessage("طول شماره شبا نامعتبر است.");

        RuleFor(x => x.AccountHolder)
            .NotEmpty().WithMessage("نام صاحب حساب الزامی است.")
            .MaximumLength(150).WithMessage("نام صاحب حساب نباید بیش از ۱۵۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نباید بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}