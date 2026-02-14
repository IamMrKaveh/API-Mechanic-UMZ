using Application.Categories.Features.Shared;

namespace Application.Categories.Features.Queries.GetCategoryWithGroups;

/// <summary>
/// جزئیات یک Category به همراه گروه‌ها (Admin)
/// </summary>
public record GetCategoryWithGroupsQuery(int CategoryId)
    : IRequest<ServiceResult<CategoryWithGroupsDto?>>;