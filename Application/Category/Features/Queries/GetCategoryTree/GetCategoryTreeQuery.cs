using Application.Category.Features.Shared;
using Application.Common.Results;

namespace Application.Category.Features.Queries.GetCategoryTree;

public record GetCategoryTreeQuery : IRequest<ServiceResult<IReadOnlyList<CategoryTreeDto>>>;