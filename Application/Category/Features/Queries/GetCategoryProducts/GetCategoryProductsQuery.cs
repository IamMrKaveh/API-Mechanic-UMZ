namespace Application.Category.Features.Queries.GetCategoryProducts;

/// <summary>
/// محصولات یک دسته‌بندی با صفحه‌بندی
/// </summary>
public record GetCategoryProductsQuery(
    int CategoryId,
    bool ActiveOnly,
    int Page,
    int PageSize
    ) : IRequest<ServiceResult<PaginatedResult<CategoryProductItemDto>>>;