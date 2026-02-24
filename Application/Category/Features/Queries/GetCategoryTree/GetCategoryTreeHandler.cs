namespace Application.Category.Features.Queries.GetCategoryTree;

public class GetCategoryTreeHandler
    : IRequestHandler<GetCategoryTreeQuery, ServiceResult<IReadOnlyList<CategoryTreeDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryTreeHandler(
        ICategoryQueryService queryService
        )
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryTreeDto>>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken ct
        )
    {
        var tree = await _queryService.GetCategoryTreeAsync(ct);
        return ServiceResult<IReadOnlyList<CategoryTreeDto>>.Success(tree);
    }
}