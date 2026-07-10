using SharedKernel.Abstractions.Interfaces;

namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed class GetDashboardStatisticsValidator : AbstractValidator<GetDashboardStatisticsQuery>
{
    public GetDashboardStatisticsValidator(IDateTimeProvider dateTimeProvider)
    {
        When(q => q.FromDate.HasValue && q.ToDate.HasValue, () =>
        {
            RuleFor(q => q.FromDate)
                .LessThan(q => q.ToDate)
                .WithMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
        });

        When(q => q.FromDate.HasValue, () =>
        {
            RuleFor(q => q.FromDate)
                .LessThanOrEqualTo(_ => dateTimeProvider.UtcNow)
                .WithMessage("تاریخ شروع نمی‌تواند در آینده باشد.");
        });
    }
}