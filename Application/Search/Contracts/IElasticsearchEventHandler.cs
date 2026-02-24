namespace Application.Search.Contracts;

/// <summary>
/// Event handler for Elasticsearch synchronization
/// </summary>
public interface IElasticsearchEventHandler
{
    void HandleProductChangedAsync(ProductChangedEvent @event, CancellationToken ct = default);

    Task HandleCategoryChangedAsync(CategoryChangedEvent @event, CancellationToken ct = default);

    Task HandleBrandChangedAsync(BrandChangedEvent @event, CancellationToken ct = default);
}