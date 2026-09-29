using Application.Common.Validation;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Analytics.Features.Queries.GetSalesChartData;

public sealed class GetSalesChartDataValidator : AbstractValidator<GetSalesChartDataQuery>
{
    private static readonly string[] AllowedGroupByValues = ["day", "week", "month"];

    public GetSalesChartDataValidator(IDateTimeProvider dateTimeProvider)
    {
        this.RuleForRequiredDateRange(q => q.FromDate, q => q.ToDate, () => dateTimeProvider.UtcNow);

        RuleFor(q => q.GroupBy)
            .Must(v => AllowedGroupByValues.Contains(v.ToLowerInvariant()))
            .WithMessage("مقدار groupBy باید یکی از day, week یا month باشد.");
    }
}