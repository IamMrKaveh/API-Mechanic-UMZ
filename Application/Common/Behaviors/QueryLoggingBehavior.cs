using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Common.Behaviors;

public sealed class QueryLoggingBehavior<TRequest, TResponse>(
    ILogger<QueryLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IQuery)
            return await next(cancellationToken);

        var queryName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "Handling query {QueryName}",
            queryName);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();
            logger.LogInformation(
                "Handled query {QueryName} in {ElapsedMilliseconds} ms",
                queryName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Query {QueryName} failed after {ElapsedMilliseconds} ms",
                queryName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}