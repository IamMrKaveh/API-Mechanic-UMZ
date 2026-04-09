namespace Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is IQuery)
            return await next();

        try
        {
            return await unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await using var transaction = await unitOfWork.BeginTransactionAsync(ct);
                try
                {
                    var response = await next();
                    await unitOfWork.CommitTransactionAsync(ct);
                    return response;
                }
                catch
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    throw;
                }
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transaction failed for {RequestType}", typeof(TRequest).Name);
            throw;
        }
    }
}

public interface IQuery
{ }