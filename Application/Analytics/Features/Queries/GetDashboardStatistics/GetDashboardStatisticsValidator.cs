namespace Application.Analytics.Features.Queries.GetDashboardStatistics;

public sealed class GetDashboardStatisticsValidator : AbstractValidator<GetDashboardStatisticsQuery>
{
    public GetDashboardStatisticsValidator()
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
                .LessThanOrEqualTo(DateTime.UtcNow)
                .WithMessage("تاریخ شروع نمی‌تواند در آینده باشد.");
        });
    }
}