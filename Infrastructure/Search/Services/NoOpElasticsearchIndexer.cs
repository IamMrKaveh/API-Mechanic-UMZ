using Infrastructure.Search.Contracts;

namespace Infrastructure.Search.Services;

public sealed class NoOpElasticsearchIndexer : IElasticsearchIndexer
{
    public Task<bool> IndexDocumentAsync(
        string entityType,
        Guid entityId,
        string document,
        string changeType,
        CancellationToken ct = default) => Task.FromResult(true);
}