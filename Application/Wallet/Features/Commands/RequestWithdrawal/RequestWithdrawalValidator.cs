using SharedKernel.Validation;

namespace Application.Wallet.Features.Commands.RequestWithdrawal;

public sealed class RequestWithdrawalValidator : AbstractValidator<RequestWithdrawalCommand>
{
    public RequestWithdrawalValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ باید بزرگتر از صفر باشد.")
            .GreaterThanOrEqualTo(50_000m).WithMessage("حداقل مبلغ برداشت ۵۰,۰۰۰ تومان است.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.Iban)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("شماره شبا الزامی است.")
            .Must(BeValidIbanFormat).WithMessage("فرمت شماره شبا نامعتبر است. باید با IR شروع شود و شامل ۲۴ رقم باشد.")
            .Must(BeValidIbanChecksum).WithMessage("شماره شبا نامعتبر است. لطفاً شماره شبای صحیح وارد کنید.");

        RuleFor(x => x.AccountHolder)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("نام صاحب حساب الزامی است.")
            .Must(v => !string.IsNullOrWhiteSpace(v?.Trim())).WithMessage("نام صاحب حساب الزامی است.")
            .Must(v => v!.Trim().Length >= 3).WithMessage("نام صاحب حساب باید حداقل ۳ کاراکتر باشد.")
            .MaximumLength(150).WithMessage("نام صاحب حساب نباید بیش از ۱۵۰ کاراکتر باشد.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نباید بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }

    private static bool BeValidIbanFormat(string? iban)
        => IranianIban.HasValidFormat(IranianIban.Normalize(iban));

    private static bool BeValidIbanChecksum(string? iban)
        => IranianIban.HasValidChecksum(IranianIban.Normalize(iban));
}