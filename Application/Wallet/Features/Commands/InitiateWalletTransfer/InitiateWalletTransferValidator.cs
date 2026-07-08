using FluentValidation;

namespace Application.Wallet.Features.Commands.InitiateWalletTransfer;

public sealed class InitiateWalletTransferValidator : AbstractValidator<InitiateWalletTransferCommand>
{
    public InitiateWalletTransferValidator()
    {
        RuleFor(x => x.FromUserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.RecipientPhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل گیرنده الزامی است.")
            .MaximumLength(32);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ از سقف مجاز عبور کرده است.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("توضیحات نباید بیش از ۵۰۰ کاراکتر باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}