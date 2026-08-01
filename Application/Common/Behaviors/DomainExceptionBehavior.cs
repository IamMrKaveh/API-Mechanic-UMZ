using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public sealed class DomainExceptionBehavior<TRequest, TResponse>(
    ILogger<DomainExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next(ct);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(
                "Domain rule violated in {RequestName}: {Message}",
                typeof(TRequest).Name,
                ex.Message,
                ct);

            var error = Error.BusinessRule("Domain.Rule", ex.Message);
            var responseType = typeof(TResponse);

            if (responseType == typeof(ServiceResult))
                return (TResponse)(object)ServiceResult.Failure(error);

            if (responseType.IsGenericType &&
                responseType.GetGenericTypeDefinition() == typeof(ServiceResult<>))
            {
                var failureMethod = responseType.GetMethod(
                    nameof(ServiceResult.Failure),
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: [typeof(Error)],
                    modifiers: null);

                if (failureMethod is not null)
                    return (TResponse)failureMethod.Invoke(null, [error])!;
            }

            throw;
        }
    }
}
