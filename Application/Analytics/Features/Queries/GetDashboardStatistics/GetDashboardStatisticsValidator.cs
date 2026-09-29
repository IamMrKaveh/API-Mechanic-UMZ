using Application.Common.Validation;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed class GetDashboardStatisticsValidator : AbstractValidator<GetDashboardStatisticsQuery>
{
    public GetDashboardStatisticsValidator(IDateTimeProvider dateTimeProvider)
    {
        this.RuleForOptionalDateRange(q => q.FromDate, q => q.ToDate, allowEqual: false);

        When(q => q.FromDate.HasValue, () =>
        {
            RuleFor(q => q.FromDate)
                .LessThanOrEqualTo(_ => dateTimeProvider.UtcNow)
                .WithMessage("تاریخ شروع نمی‌تواند در آینده باشد.");
        });
    }
}