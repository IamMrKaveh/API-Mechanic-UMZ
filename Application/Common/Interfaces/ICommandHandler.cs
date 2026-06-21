namespace Application.Common.Interfaces;

public interface ICommandHandler<in TCommand>
    : IRequestHandler<TCommand, ServiceResult>
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResult>
    : IRequestHandler<TCommand, ServiceResult<TResult>>
    where TCommand : ICommand<TResult>
{
}