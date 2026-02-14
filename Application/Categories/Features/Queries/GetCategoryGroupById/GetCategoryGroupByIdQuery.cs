namespace Application.Categories.Features.Queries.GetCategoryGroupById;

public record GetCategoryGroupByIdQuery(int Id)
    : IRequest<ServiceResult<CategoryGroupDetailDto?>>;