namespace Application.Categories.Features.Queries.GetCategoryGroupById;

public class GetCategoryGroupByIdHandler
    : IRequestHandler<GetCategoryGroupByIdQuery, ServiceResult<CategoryGroupDetailDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryGroupByIdHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<CategoryGroupDetailDto?>> Handle(
        GetCategoryGroupByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryGroupDetailAsync(request.Id, cancellationToken);

        if (result == null)
            return ServiceResult<CategoryGroupDetailDto?>.Failure("گروه یافت نشد.", 404);

        return ServiceResult<CategoryGroupDetailDto?>.Success(result);
    }
}