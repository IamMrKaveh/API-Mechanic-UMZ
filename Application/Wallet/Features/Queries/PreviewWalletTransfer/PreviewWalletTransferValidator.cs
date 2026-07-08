using FluentValidation;

namespace Application.Wallet.Features.Queries.PreviewWalletTransfer;

public sealed class PreviewWalletTransferValidator : AbstractValidator<PreviewWalletTransferQuery>
{
    public PreviewWalletTransferValidator()
    {
        RuleFor(x => x.FromUserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.RecipientPhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل گیرنده الزامی است.")
            .MaximumLength(32);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ باید بزرگتر از صفر باشد.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("مبلغ از سقف مجاز عبور کرده است.");
    }
}