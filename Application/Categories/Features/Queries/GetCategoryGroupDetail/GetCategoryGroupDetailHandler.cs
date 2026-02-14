using Application.Categories.Features.Shared;
using Application.Categories.Contracts;

namespace Application.Categories.Features.Queries.GetCategoryGroupDetail;

public class GetCategoryGroupDetailHandler
    : IRequestHandler<GetCategoryGroupDetailQuery, ServiceResult<CategoryGroupDetailDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryGroupDetailHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<CategoryGroupDetailDto?>> Handle(
        GetCategoryGroupDetailQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryGroupDetailAsync(
            request.GroupId, cancellationToken);

        if (result == null)
            return ServiceResult<CategoryGroupDetailDto?>.Failure("گروه یافت نشد.", 404);

        return ServiceResult<CategoryGroupDetailDto?>.Success(result);
    }
}