using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Queries.GetCategoryWithGroups;

public class GetCategoryWithGroupsHandler(ICategoryQueryService queryService)
    : IRequestHandler<GetCategoryWithBrandsQuery, ServiceResult<CategoryWithBrandsDto?>>
{
    private readonly ICategoryQueryService _queryService = queryService;

    public async Task<ServiceResult<CategoryWithBrandsDto?>> Handle(
        GetCategoryWithBrandsQuery request,
        CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId);
        var result = await _queryService.GetCategoryWithBrandsAsync(categoryId, ct);

        if (result is null)
            return ServiceResult<CategoryWithBrandsDto?>.NotFound("دسته‌بندی یافت نشد.");

        return ServiceResult<CategoryWithBrandsDto?>.Success(result);
    }
}