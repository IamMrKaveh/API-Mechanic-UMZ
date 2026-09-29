using Application.Common.Validation;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Analytics.Features.Queries.GetRevenueReport;

public sealed class GetRevenueReportValidator : AbstractValidator<GetRevenueReportQuery>
{
    public GetRevenueReportValidator(IDateTimeProvider dateTimeProvider)
    {
        this.RuleForRequiredDateRange(q => q.FromDate, q => q.ToDate, () => dateTimeProvider.UtcNow);
    }
}