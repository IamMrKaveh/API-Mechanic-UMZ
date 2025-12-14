namespace Infrastructure.Search;

public class ElasticsearchCircuitBreaker
{
    private readonly AsyncCircuitBreakerPolicy _policy;
    private readonly ILogger<ElasticsearchCircuitBreaker> _logger;

    public ElasticsearchCircuitBreaker(ILogger<ElasticsearchCircuitBreaker> logger)
    {
        _logger = logger;

        _policy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(
                        exception,
                        "Circuit breaker opened for Elasticsearch. Duration: {Duration}",
                        duration);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset for Elasticsearch");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open for Elasticsearch");
                });
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await _policy.ExecuteAsync(operation);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Request blocked by circuit breaker - Elasticsearch is unavailable");
            throw new InvalidOperationException("Search service temporarily unavailable");
        }
    }
}