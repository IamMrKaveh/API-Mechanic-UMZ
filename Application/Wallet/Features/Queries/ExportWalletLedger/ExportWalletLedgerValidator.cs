using Application.Common.Validation;
using FluentValidation;

namespace Application.Wallet.Features.Queries.ExportWalletLedger;

public sealed class ExportWalletLedgerValidator : AbstractValidator<ExportWalletLedgerQuery>
{
    private static readonly string[] AllowedFormats = ["csv", "json"];

    public ExportWalletLedgerValidator()
    {
        this.RuleForRequiredId(x => x.UserId);

        RuleFor(x => x.MaxRows).InclusiveBetween(1, 100_000);

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Format must be either 'csv' or 'json'.");

        this.RuleForOptionalDateRangeValues(
            x => x.FromDate,
            x => x.ToDate,
            x => x.FromDate!.Value,
            x => x.ToDate!.Value);

        When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue, () =>
        {
            RuleFor(x => x.MinAmount!.Value)
                .LessThanOrEqualTo(x => x.MaxAmount!.Value);
        });
    }
}