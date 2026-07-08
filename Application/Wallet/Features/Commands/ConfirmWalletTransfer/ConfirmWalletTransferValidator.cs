using FluentValidation;

namespace Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public sealed class ConfirmWalletTransferValidator : AbstractValidator<ConfirmWalletTransferCommand>
{
    public ConfirmWalletTransferValidator()
    {
        RuleFor(x => x.TransferId)
            .NotEmpty().WithMessage("شناسه انتقال الزامی است.");

        RuleFor(x => x.FromUserId)
            .NotEmpty().WithMessage("شناسه کاربر الزامی است.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تأیید الزامی است.")
            .Matches("^[0-9]+$").WithMessage("کد تأیید باید فقط شامل ارقام باشد.")
            .Length(4, 8).WithMessage("طول کد تأیید نامعتبر است.");
    }
}