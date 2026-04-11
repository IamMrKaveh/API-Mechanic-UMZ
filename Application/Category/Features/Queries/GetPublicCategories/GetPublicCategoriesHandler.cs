using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetPublicCategories;

public class GetPublicCategoriesHandler(ICategoryQueryService categoryQueryService)
    : IRequestHandler<GetPublicCategoriesQuery, ServiceResult<PaginatedResult<CategoryDto>>>
{
    public async Task<ServiceResult<PaginatedResult<CategoryDto>>> Handle(
        GetPublicCategoriesQuery request,
        CancellationToken ct)
    {
        var result = await categoryQueryService.GetPublicCategoriesAsync(
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<CategoryDto>>.Success(result);
    }
}