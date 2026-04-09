namespace Application.Search.Features.Queries.SearchProducts;

public class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
{
    public SearchProductsValidator()
    {
        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("حداقل قیمت نمی‌تواند منفی باشد.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("حداکثر قیمت نمی‌تواند منفی باشد.");

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("حداقل قیمت نمی‌تواند بیشتر از حداکثر قیمت باشد.");
    }
}