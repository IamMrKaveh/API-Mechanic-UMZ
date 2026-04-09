using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategory;

public record GetCategoryQuery(Guid Id) : IRequest<ServiceResult<CategoryDetailDto>>;