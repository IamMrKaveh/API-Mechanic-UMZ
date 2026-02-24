namespace Application.Category.Features.Queries.GetCategoryWithGroups;

/// <summary>
/// جزئیات یک Category به همراه گروه‌ها (Admin)
/// </summary>
public record GetCategoryWithGroupsQuery(int CategoryId)
    : IRequest<ServiceResult<CategoryWithBrandsDto?>>;