using Application.Category.Features.Shared;
using Application.Common.Results;

namespace Application.Category.Features.Queries.GetCategory;

public record GetCategoryQuery(int Id) : IRequest<ServiceResult<CategoryDetailDto>>;