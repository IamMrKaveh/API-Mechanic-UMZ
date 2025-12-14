namespace Infrastructure.Search;

public class ResilientElasticSearchService : ISearchService
{
    private readonly ElasticSearchService _innerService;
    private readonly ILogger<ResilientElasticSearchService> _logger;
    private readonly IElasticDeadLetterQueue _deadLetterQueue;
    private readonly ElasticsearchCircuitBreaker _circuitBreaker;

    public ResilientElasticSearchService(
        ElasticSearchService innerService,
        ILogger<ResilientElasticSearchService> logger,
        IElasticDeadLetterQueue deadLetterQueue,
        ElasticsearchCircuitBreaker circuitBreaker)
    {
        _innerService = innerService;
        _logger = logger;
        _deadLetterQueue = deadLetterQueue;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<SearchResultDto<ProductSearchDocument>> SearchProductsAsync(
        SearchProductsQuery query,
        CancellationToken ct)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
            await ExecuteWithRetryAsync(
                async () => await _innerService.SearchProductsAsync(query, ct),
                nameof(SearchProductsAsync)));
    }

    public async Task<GlobalSearchResultDto> SearchGlobalAsync(
        string query,
        CancellationToken ct)
    {
        return await ExecuteWithRetryAsync(
            async () => await _innerService.SearchGlobalAsync(query, ct),
            nameof(SearchGlobalAsync));
    }

    public async Task IndexProductAsync(
        ProductSearchDocument document,
        CancellationToken ct)
    {
        try
        {
            await ExecuteWithRetryAsync(
                async () =>
                {
                    await _innerService.IndexProductAsync(document, ct);
                    return true;
                },
                nameof(IndexProductAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index product {ProductId} after retries. Adding to DLQ.", document.Id);

            try
            {
                await _deadLetterQueue.EnqueueAsync(new FailedIndexOperation
                {
                    EntityType = "Product",
                    EntityId = document.Id.ToString(),
                    Document = JsonSerializer.Serialize(document),
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                }, ct);

                _logger.LogInformation("Product {ProductId} added to DLQ for retry", document.Id);
            }
            catch (Exception dlqEx)
            {
                _logger.LogCritical(dlqEx, "Failed to add operation to DLQ for product {ProductId}", document.Id);
                throw;
            }
        }
    }

    public async Task IndexCategoryAsync(
        CategorySearchDocument document,
        CancellationToken ct)
    {
        try
        {
            await ExecuteWithRetryAsync(
                async () =>
                {
                    await _innerService.IndexCategoryAsync(document, ct);
                    return true;
                },
                nameof(IndexCategoryAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index category {CategoryId} after retries", document.Id);

            await _deadLetterQueue.EnqueueAsync(new FailedIndexOperation
            {
                EntityType = "Category",
                EntityId = document.Id.ToString(),
                Document = JsonSerializer.Serialize(document),
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            }, ct);

            throw;
        }
    }

    public async Task IndexCategoryGroupAsync(
        CategoryGroupSearchDocument document,
        CancellationToken ct)
    {
        try
        {
            await ExecuteWithRetryAsync(
                async () =>
                {
                    await _innerService.IndexCategoryGroupAsync(document, ct);
                    return true;
                },
                nameof(IndexCategoryGroupAsync));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index category group {GroupId} after retries", document.Id);

            await _deadLetterQueue.EnqueueAsync(new FailedIndexOperation
            {
                EntityType = "CategoryGroup",
                EntityId = document.Id.ToString(),
                Document = JsonSerializer.Serialize(document),
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            }, ct);

            throw;
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        const int maxRetries = 3;
        var attempt = 0;

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));

                _logger.LogWarning(
                    ex,
                    "Retry {Attempt}/{MaxRetries} for {Operation} after {Delay}s",
                    attempt,
                    maxRetries,
                    operationName,
                    delay.TotalSeconds);

                await Task.Delay(delay);
            }
        }
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is HttpRequestException
            || ex is TaskCanceledException
            || ex is TimeoutException
            || (ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) ?? false)
            || (ex.Message?.Contains("connection", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}