namespace Application.Category.Features.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler
    : IRequestHandler<GetCategoryByIdQuery, ServiceResult<CategoryWithBrandsDto?>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryByIdQueryHandler(ICategoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<CategoryWithBrandsDto?>> Handle(
        GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await _queryService.GetCategoryWithBrandsAsync(request.Id, cancellationToken);

        if (result == null)
            return ServiceResult<CategoryWithBrandsDto?>.Failure("دسته‌بندی یافت نشد.", 404);

        return ServiceResult<CategoryWithBrandsDto?>.Success(result);
    }
}