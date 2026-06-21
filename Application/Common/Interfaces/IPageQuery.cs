namespace Application.Common.Interfaces;

public interface IPageQuery<TResult> : IQuery<PaginatedResult<TResult>>
{
    int Page { get; }

    int PageSize { get; }
}