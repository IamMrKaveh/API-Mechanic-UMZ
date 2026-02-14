namespace Application.Analytics.Features.Queries.GetTopSellingProducts;

public sealed class GetTopSellingProductsValidator : AbstractValidator<GetTopSellingProductsQuery>
{
    public GetTopSellingProductsValidator()
    {
        RuleFor(q => q.Count)
            .InclusiveBetween(1, 100)
            .WithMessage("تعداد باید بین ۱ و ۱۰۰ باشد.");

        When(q => q.FromDate.HasValue && q.ToDate.HasValue, () =>
        {
            RuleFor(q => q.FromDate)
                .LessThan(q => q.ToDate)
                .WithMessage("تاریخ شروع باید قبل از تاریخ پایان باشد.");
        });
    }
}