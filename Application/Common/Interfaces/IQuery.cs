namespace Application.Common.Interfaces;

public interface IQuery
{
}

public interface IQuery<TResult> : IRequest<ServiceResult<TResult>>, IQuery
{
}