namespace Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsValidator : AbstractValidator<GetCategoryProductsQuery>
{
    public GetCategoryProductsValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("شناسه دسته‌بندی الزامی است.");
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("شماره صفحه باید بزرگ‌تر از صفر باشد.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("اندازه صفحه باید بین ۱ تا ۱۰۰ باشد.");
    }
}