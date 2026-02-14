using Application.Categories.Features.Shared;

namespace Application.Categories.Features.Queries.GetCategoryGroupDetail;

public record GetCategoryGroupDetailQuery(int GroupId)
    : IRequest<ServiceResult<CategoryGroupDetailDto?>>;