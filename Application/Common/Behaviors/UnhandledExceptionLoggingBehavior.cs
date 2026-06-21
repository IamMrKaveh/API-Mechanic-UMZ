using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviors;

public sealed class UnhandledExceptionLoggingBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (FluentValidation.ValidationException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unhandled exception for request {RequestName}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw;
        }
    }
}