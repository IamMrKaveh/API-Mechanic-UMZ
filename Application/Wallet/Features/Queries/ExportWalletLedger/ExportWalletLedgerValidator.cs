using FluentValidation;

namespace Application.Wallet.Features.Queries.ExportWalletLedger;

public sealed class ExportWalletLedgerValidator : AbstractValidator<ExportWalletLedgerQuery>
{
    private static readonly string[] AllowedFormats = ["csv", "json"];

    public ExportWalletLedgerValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.MaxRows).InclusiveBetween(1, 100_000);

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Format must be either 'csv' or 'json'.");

        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate!.Value)
                .LessThanOrEqualTo(x => x.ToDate!.Value);
        });

        When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue, () =>
        {
            RuleFor(x => x.MinAmount!.Value)
                .LessThanOrEqualTo(x => x.MaxAmount!.Value);
        });
    }
}