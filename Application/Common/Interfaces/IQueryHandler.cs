namespace Application.Common.Interfaces;

public interface IQueryHandler<in TQuery, TResult>
    : IRequestHandler<TQuery, ServiceResult<TResult>>
    where TQuery : IQuery<TResult>
{
}