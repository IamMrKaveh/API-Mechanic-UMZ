namespace Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Only transactional for Commands (requests that do not end with 'Query')
        // Or check if it implements a specific marker interface like ICommand
        var isCommand = request.GetType().Name.EndsWith("Command");

        if (!isCommand)
        {
            return await next();
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();
            var response = await next();
            await _unitOfWork.CommitTransactionAsync();
            return response;
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}