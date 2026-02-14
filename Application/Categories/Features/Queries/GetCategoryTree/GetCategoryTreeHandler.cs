namespace Application.Categories.Features.Queries.GetCategoryTree;

public class GetCategoryTreeHandler
    : IRequestHandler<GetCategoryTreeQuery, ServiceResult<IReadOnlyList<CategoryTreeDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryTreeHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryTreeDto>>> Handle(
        GetCategoryTreeQuery request, CancellationToken cancellationToken)
    {
        var tree = await _queryService.GetCategoryTreeAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<CategoryTreeDto>>.Success(tree);
    }
}