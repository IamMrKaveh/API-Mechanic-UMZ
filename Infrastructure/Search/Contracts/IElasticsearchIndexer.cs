namespace Infrastructure.Search.Contracts;

public interface IElasticsearchIndexer
{
    Task<bool> IndexDocumentAsync(
        string entityType,
        Guid entityId,
        string document,
        string changeType,
        CancellationToken ct = default);
}