using FluentValidation;
using System.Text.RegularExpressions;

namespace Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed class RequestWithdrawalValidator : AbstractValidator<RequestWithdrawalCommand>
{
    private static readonly Regex IbanPattern = new(@"^IR\d{24}$", RegexOptions.Compiled);

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
            .Must(BeValidIbanFormat).WithMessage("فرمت شماره شبا نامعتبر است. باید با IR شروع شود و ۲۶ کاراکتر باشد.");

        RuleFor(x => x.AccountHolder)
            .NotEmpty().WithMessage("نام صاحب حساب الزامی است.")
            .MinimumLength(3).WithMessage("نام صاحب حساب باید حداقل ۳ کاراکتر باشد.")
            .MaximumLength(150).WithMessage("نام صاحب حساب نباید بیش از ۱۵۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نباید بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }

    private static bool BeValidIbanFormat(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return false;
        var normalized = iban.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        if (!normalized.StartsWith("IR")) normalized = "IR" + normalized;
        return IbanPattern.IsMatch(normalized);
    }
}