using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryProducts;

public record GetCategoryProductsQuery(
    Guid CategoryId,
    bool ActiveOnly,
    int Page,
    int PageSize) : IRequest<ServiceResult<PaginatedResult<CategoryProductItemDto>>>;