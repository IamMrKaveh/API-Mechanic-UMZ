using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsHandler(ICategoryQueryService queryService)
    : IRequestHandler<GetCategoryProductsQuery, ServiceResult<PaginatedResult<CategoryProductItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<CategoryProductItemDto>>> Handle(
        GetCategoryProductsQuery request,
        CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);

        var result = await queryService.GetCategoryProductsAsync(
            categoryId,
            request.ActiveOnly,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<CategoryProductItemDto>>.Success(result);
    }
}