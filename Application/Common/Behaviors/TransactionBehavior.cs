namespace Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is IQuery)
            return await next(ct);

        try
        {
            return await unitOfWork.ExecuteStrategyAsync(async () =>
            {
                var transaction = await unitOfWork.BeginTransactionAsync(ct);
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
            await auditService.LogSystemEventAsync("TransactionFailed", $"Transaction failed for {typeof(TRequest).Name}: {ex.Message}", ct);
            throw;
        }
    }
}

public interface IQuery
{ }