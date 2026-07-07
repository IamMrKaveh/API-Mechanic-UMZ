using FluentValidation;

namespace Application.Wallet.Features.Commands.RejectWithdrawal;

public sealed class RejectWithdrawalValidator : AbstractValidator<RejectWithdrawalCommand>
{
    public RejectWithdrawalValidator()
    {
        RuleFor(x => x.WithdrawalId).NotEmpty();
        RuleFor(x => x.AdminId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("دلیل رد درخواست الزامی است.")
            .MaximumLength(500).WithMessage("دلیل رد نباید بیش از ۵۰۰ کاراکتر باشد.");
    }
}