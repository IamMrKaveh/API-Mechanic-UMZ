using Npgsql;

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
        if (request is IQuery || request is IBypassTransactionBehavior || request is IManualTransactionRequest)
            return await next(ct);

        try
        {
            return await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
            {
                var response = await next(cancellationToken);

                if (response is ServiceResult serviceResult && serviceResult.IsFailure)
                    return response;

                await unitOfWork.SaveChangesAsync(cancellationToken);
                return response;
            }, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            var constraintName = GetConstraintName(ex);

            await auditService.LogSystemEventAsync(
                "UniqueConstraintViolation",
                $"Unique constraint '{constraintName}' violated for {typeof(TRequest).Name}: {ex.Message}",
                ct);

            var mapped = (request as IHasUniqueConstraintMapping)?.MapUniqueConstraintViolation(constraintName);
            var message = mapped ?? "این رکورد از قبل وجود دارد.";

            return BuildFailure(Error.Conflict(ErrorCode.UniqueViolation, message));
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            await auditService.LogSystemEventAsync(
                "ForeignKeyViolation",
                $"FK violation for {typeof(TRequest).Name}: {ex.Message}",
                ct);

            return BuildFailure(Error.Conflict(
                ErrorCode.ForeignKeyViolation,
                "این عملیات به دلیل وابستگی به منابع دیگر امکان‌پذیر نیست."));
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

    private static TResponse BuildFailure(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(ServiceResult))
            return (TResponse)(object)ServiceResult.Failure(error);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(ServiceResult<>))
        {
            var failureMethod = responseType.GetMethod(
                nameof(ServiceResult<object>.Failure),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Error)],
                modifiers: null);

            if (failureMethod is not null)
                return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        throw new InvalidOperationException(
            $"TransactionBehavior cannot build a failure response for type {responseType.FullName}.");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23505";

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg && pg.SqlState == "23503";

    private static string? GetConstraintName(DbUpdateException ex)
        => (ex.InnerException as PostgresException)?.ConstraintName;
}
