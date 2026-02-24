namespace Application.Category.Features.Queries.GetCategoryWithGroups;

public class GetCategoryWithGroupsHandler
    : IRequestHandler<GetCategoryWithGroupsQuery, ServiceResult<CategoryWithBrandsDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryWithGroupsHandler(
        ICategoryQueryService queryService
        )
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<CategoryWithBrandsDto?>> Handle(
        GetCategoryWithGroupsQuery request,
        CancellationToken ct
        )
    {
        var result = await _queryService.GetCategoryWithBrandsAsync(
            request.CategoryId, ct);

        if (result == null)
            return ServiceResult<CategoryWithBrandsDto?>.Failure("دسته‌بندی یافت نشد.", 404);

        return ServiceResult<CategoryWithBrandsDto?>.Success(result);
    }
}