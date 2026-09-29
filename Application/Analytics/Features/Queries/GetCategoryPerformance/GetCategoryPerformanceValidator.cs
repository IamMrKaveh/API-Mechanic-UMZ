using Application.Common.Validation;

namespace Application.Analytics.Features.Queries.GetCategoryPerformance;

public sealed class GetCategoryPerformanceValidator : AbstractValidator<GetCategoryPerformanceQuery>
{
    public GetCategoryPerformanceValidator()
    {
        this.RuleForOptionalDateRange(q => q.FromDate, q => q.ToDate, allowEqual: false);
    }
}