using Application.Common.Validation;

namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsValidator : AbstractValidator<GetTopSellingProductsQuery>
{
    public GetTopSellingProductsValidator()
    {
        RuleFor(q => q.Count)
            .InclusiveBetween(1, 100)
            .WithMessage("تعداد باید بین ۱ و ۱۰۰ باشد.");

        this.RuleForOptionalDateRange(q => q.FromDate, q => q.ToDate, allowEqual: false);
    }
}