using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryTree;

public record GetCategoryTreeQuery : IQuery<IReadOnlyList<CategoryTreeDto>>;