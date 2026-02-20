using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryById;

public record GetCategoryByIdQuery(int Id, int Page, int PageSize)
    : IRequest<ServiceResult<CategoryWithGroupsDto?>>;