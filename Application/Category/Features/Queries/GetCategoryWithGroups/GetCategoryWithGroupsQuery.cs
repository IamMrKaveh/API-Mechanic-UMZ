using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryWithGroups;

public record GetCategoryWithBrandsQuery(Guid CategoryId) : IRequest<ServiceResult<CategoryWithBrandsDto?>>;