using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryTree;

public class GetCategoryTreeHandler(
    ICategoryQueryService categoryQueryService) : IRequestHandler<GetCategoryTreeQuery, ServiceResult<IReadOnlyList<CategoryTreeDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<CategoryTreeDto>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken ct)
    {
        var tree = await categoryQueryService.GetCategoryTreeAsync(ct);
        return ServiceResult<IReadOnlyList<CategoryTreeDto>>.Success(tree);
    }
}