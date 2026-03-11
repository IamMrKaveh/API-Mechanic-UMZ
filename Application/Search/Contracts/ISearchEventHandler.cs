namespace Application.Search.Contracts;

public interface ISearchEventHandler
{
    void HandleProductChangedAsync(ProductChangedEvent @event, CancellationToken ct = default);

    Task HandleCategoryChangedAsync(CategoryChangedEvent @event, CancellationToken ct = default);

    Task HandleBrandChangedAsync(BrandChangedEvent @event, CancellationToken ct = default);
}