using FluentValidation;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public sealed class GetWalletLedgerValidator : AbstractValidator<GetWalletLedgerQuery>
{
    public GetWalletLedgerValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate!.Value)
                .LessThanOrEqualTo(x => x.ToDate!.Value)
                .WithMessage("FromDate must be less than or equal to ToDate.");
        });

        When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue, () =>
        {
            RuleFor(x => x.MinAmount!.Value)
                .LessThanOrEqualTo(x => x.MaxAmount!.Value)
                .WithMessage("MinAmount must be less than or equal to MaxAmount.");
        });

        RuleFor(x => x.MinAmount!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAmount.HasValue);

        RuleFor(x => x.MaxAmount!.Value)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxAmount.HasValue);

        RuleFor(x => x.SearchTerm!)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));
    }
}