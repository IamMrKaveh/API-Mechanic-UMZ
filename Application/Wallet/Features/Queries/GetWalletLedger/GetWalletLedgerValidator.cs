using Application.Common.Validation;

namespace Application.Wallet.Features.Queries.GetWalletLedger;

public sealed class GetWalletLedgerValidator : AbstractValidator<GetWalletLedgerQuery>
{
    public GetWalletLedgerValidator()
    {
        this.RuleForPagination(x => x.Page, x => x.PageSize, maxPageSize: 200);

        this.RuleForOptionalDateRangeValues(
            x => x.FromDate,
            x => x.ToDate,
            x => x.FromDate!.Value,
            x => x.ToDate!.Value,
            message: "FromDate must be less than or equal to ToDate.");

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
