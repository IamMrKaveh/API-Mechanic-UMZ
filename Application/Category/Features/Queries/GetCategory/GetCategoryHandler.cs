using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Category.Features.Queries.GetCategory;

public class GetCategoryHandler(
    ICategoryQueryService categoryQueryService)
    : IQueryHandler<GetCategoryQuery, CategoryDetailDto>
{
    public async Task<ServiceResult<CategoryDetailDto>> Handle(GetCategoryQuery request, CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.Id);
        var category = await categoryQueryService.GetCategoryDetailAsync(categoryId, ct);
        return category is null
            ? ServiceResult<CategoryDetailDto>.NotFound("دسته‌بندی یافت نشد.")
            : ServiceResult<CategoryDetailDto>.Success(category);
    }
}