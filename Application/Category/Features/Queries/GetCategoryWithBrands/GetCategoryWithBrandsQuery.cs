using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryWithBrands;

public record GetCategoryWithBrandsQuery(Guid CategoryId) : IRequest<ServiceResult<CategoryWithBrandsDto?>>;