namespace Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that wraps every command in a database transaction and
/// relies on IUnitOfWork to persist domain events to the Outbox table atomically before committing,
/// guaranteeing that state changes and event publication are never split.
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var isCommand = request.GetType().Name.EndsWith("Command");

        if (!isCommand)
            return await next(ct);

        using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var response = await next(ct);

            await _unitOfWork.CommitTransactionAsync(ct);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}