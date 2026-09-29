using Application.Common.Validation;
using FluentValidation;

namespace Application.Wallet.Features.Commands.ConfirmWalletTransfer;

public sealed class ConfirmWalletTransferValidator : AbstractValidator<ConfirmWalletTransferCommand>
{
    public ConfirmWalletTransferValidator()
    {
        this.RuleForRequiredId(x => x.TransferId, "شناسه انتقال الزامی است.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تأیید الزامی است.")
            .Matches("^[0-9]+$").WithMessage("کد تأیید باید فقط شامل ارقام باشد.")
            .Length(4, 8).WithMessage("طول کد تأیید نامعتبر است.");
    }
}