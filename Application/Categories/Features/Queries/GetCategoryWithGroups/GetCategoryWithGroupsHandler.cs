using Application.Categories.Features.Shared;
using Application.Categories.Contracts;

namespace Application.Categories.Features.Queries.GetCategoryWithGroups;

public class GetCategoryWithGroupsHandler
    : IRequestHandler<GetCategoryWithGroupsQuery, ServiceResult<CategoryWithGroupsDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryWithGroupsHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<CategoryWithGroupsDto?>> Handle(
        GetCategoryWithGroupsQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryWithGroupsAsync(
            request.CategoryId, cancellationToken);

        if (result == null)
            return ServiceResult<CategoryWithGroupsDto?>.Failure("دسته‌بندی یافت نشد.", 404);

        return ServiceResult<CategoryWithGroupsDto?>.Success(result);
    }
}