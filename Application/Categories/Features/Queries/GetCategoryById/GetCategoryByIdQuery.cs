namespace Application.Categories.Features.Queries.GetCategoryById;

public record GetCategoryByIdQuery(int Id, int Page, int PageSize)
    : IRequest<ServiceResult<CategoryWithGroupsDto?>>;