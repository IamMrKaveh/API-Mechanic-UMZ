namespace Application.Category.Features.Queries.GetCategoryWithGroups;

public record GetCategoryWithGroupsQuery(Guid CategoryId) : IRequest<ServiceResult<CategoryWithBrandsDto?>>;