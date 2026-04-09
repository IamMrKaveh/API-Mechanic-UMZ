using Application.Category.Contracts;
using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetPublicCategories;

public class GetPublicCategoriesHandler(ICategoryQueryService categoryQueryService)
    : IRequestHandler<GetPublicCategoriesQuery, ServiceResult<PaginatedResult<CategoryDto>>>
{
    private readonly ICategoryQueryService _categoryQueryService = categoryQueryService;

    public async Task<ServiceResult<PaginatedResult<CategoryDto>>> Handle(
        GetPublicCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryQueryService.GetPublicCategoriesAsync(
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<CategoryDto>>.Success(result);
    }
}