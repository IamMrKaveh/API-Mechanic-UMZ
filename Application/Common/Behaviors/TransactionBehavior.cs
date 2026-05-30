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
        if (request is IQuery || request is IBypassTransactionBehavior)
            return await next(ct);

        try
        {
            return await unitOfWork.ExecuteStrategyAsync(
                async (_, cancellationToken) => await next(cancellationToken), ct);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "TransactionFailed",
                $"Transaction failed for {typeof(TRequest).Name}: {ex.Message}",
                ct);
            throw;
        }
    }
}