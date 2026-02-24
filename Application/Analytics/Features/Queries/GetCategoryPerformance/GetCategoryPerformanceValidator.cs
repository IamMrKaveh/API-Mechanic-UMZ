namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed class GetCategoryPerformanceValidator : AbstractValidator<GetCategoryPerformanceQuery>
{
    public GetCategoryPerformanceValidator()
    {
        When(q => q.FromDate.HasValue && q.ToDate.HasValue, () =>
        {
            RuleFor(q => q.FromDate)
                .LessThan(q => q.ToDate)
                .WithMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
        });
    }
}