using Application.Category.Contracts;
using Application.Category.Features.Shared;
using Application.Common.Results;

namespace Application.Category.Features.Queries.GetCategoryWithGroups;

public class GetCategoryWithGroupsHandler(ICategoryQueryService queryService) : IRequestHandler<GetCategoryWithGroupsQuery, ServiceResult<CategoryWithBrandsDto?>>
{
    private readonly ICategoryQueryService _queryService = queryService;

    public async Task<ServiceResult<CategoryWithBrandsDto?>> Handle(
        GetCategoryWithGroupsQuery request,
        CancellationToken ct
        )
    {
        var result = await _queryService.GetCategoryWithBrandsAsync(
            request.CategoryId, ct);

        if (result == null)
            return ServiceResult<CategoryWithBrandsDto?>.NotFound("دسته‌بندی یافت نشد.");

        return ServiceResult<CategoryWithBrandsDto?>.Success(result);
    }
}