namespace Application.Common.Interfaces;

public interface ICommand : IRequest<ServiceResult>
{
}

public interface ICommand<TResult> : IRequest<ServiceResult<TResult>>
{
}