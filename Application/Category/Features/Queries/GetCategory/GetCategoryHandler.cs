using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategory;

public class GetCategoryHandler(
    ICategoryQueryService categoryQueryService) : IRequestHandler<GetCategoryQuery, ServiceResult<CategoryDetailDto>>
{
    private readonly ICategoryQueryService _categoryQueryService = categoryQueryService;

    public async Task<ServiceResult<CategoryDetailDto>> Handle(
        GetCategoryQuery request,
        CancellationToken ct)
    {
        var category = await _categoryQueryService.GetCategoryDetailAsync(request.Id, ct);
        return category is null
            ? ServiceResult<CategoryDetailDto>.NotFound("دسته‌بندی یافت نشد.")
            : ServiceResult<CategoryDetailDto>.Success(category);
    }
}