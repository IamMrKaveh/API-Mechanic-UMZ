using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Queries.GetCategoryWithBrands;

public class GetCategoryWithBrandsHandler(ICategoryQueryService queryService)
    : IQueryHandler<GetCategoryWithBrandsQuery, CategoryWithBrandsDto?>
{
    public async Task<ServiceResult<CategoryWithBrandsDto?>> Handle(
        GetCategoryWithBrandsQuery request,
        CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        var fetchResult = await (queryService.GetCategoryWithBrandsAsync(categoryId, ct)).OrNotFoundAsync("دسته‌بندی یافت نشد.");
        if (fetchResult.IsFailure) return fetchResult.Error;
        var result = fetchResult.Value;

        return ServiceResult<CategoryWithBrandsDto?>.Success(result);
    }
}