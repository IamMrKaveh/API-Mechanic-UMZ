using Application.Category.Features.Shared;

namespace Application.Category.Features.Queries.GetCategoryTree;

public record GetCategoryTreeQuery : IQuery<IReadOnlyList<CategoryTreeDto>>, ICacheableQuery
{
    public string CacheKey => "categories:tree";

    public TimeSpan? Expiry => TimeSpan.FromHours(1);
}