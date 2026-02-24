namespace Application.Category.Features.Queries.GetCategoryProducts;

public class GetCategoryProductsHandler
    : IRequestHandler<GetCategoryProductsQuery, ServiceResult<PaginatedResult<CategoryProductItemDto>>>
{
    private readonly ICategoryQueryService _queryService;

    public GetCategoryProductsHandler(
        ICategoryQueryService queryService
        )
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<PaginatedResult<CategoryProductItemDto>>> Handle(
        GetCategoryProductsQuery request,
        CancellationToken ct
        )
    {
        var result = await _queryService.GetCategoryProductsAsync(
            request.CategoryId,
            request.ActiveOnly,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<CategoryProductItemDto>>.Success(result);
    }
}