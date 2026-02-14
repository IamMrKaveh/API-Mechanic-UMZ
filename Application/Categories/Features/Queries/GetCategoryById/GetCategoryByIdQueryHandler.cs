namespace Application.Categories.Features.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler
    : IRequestHandler<GetCategoryByIdQuery, ServiceResult<CategoryWithGroupsDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryByIdQueryHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<CategoryWithGroupsDto?>> Handle(
        GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryWithGroupsAsync(request.Id, cancellationToken);

        if (result == null)
            return ServiceResult<CategoryWithGroupsDto?>.Failure("دسته‌بندی یافت نشد.", 404);

        return ServiceResult<CategoryWithGroupsDto?>.Success(result);
    }
}