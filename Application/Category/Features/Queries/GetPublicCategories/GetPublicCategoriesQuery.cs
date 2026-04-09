using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetPublicCategories;

public record GetPublicCategoriesQuery() : IRequest<ServiceResult<PaginatedResult<CategoryDto>>>;