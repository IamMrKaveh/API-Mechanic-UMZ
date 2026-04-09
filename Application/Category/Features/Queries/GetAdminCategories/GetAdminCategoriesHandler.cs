using Application.Category.Contracts;
using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesHandler(
    ICategoryQueryService categoryQueryService)
    : IRequestHandler<GetAdminCategoriesQuery, ServiceResult<PaginatedResult<CategoryListItemDto>>>
{
    private readonly ICategoryQueryService _categoryQueryService = categoryQueryService;

    public async Task<ServiceResult<PaginatedResult<CategoryListItemDto>>> Handle(
        GetAdminCategoriesQuery request,
        CancellationToken ct)
    {
        var result = await _categoryQueryService.GetCategoriesPagedAsync(
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<CategoryListItemDto>>.Success(result);
    }
}