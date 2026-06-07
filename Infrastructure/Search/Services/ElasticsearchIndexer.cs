using Infrastructure.Search.Contracts;

namespace Infrastructure.Search.Services;

public sealed class ElasticsearchIndexer(
    ElasticsearchClient client,
    IAuditService auditService) : IElasticsearchIndexer
{
    public async Task<bool> IndexDocumentAsync(
        string entityType,
        Guid entityId,
        string document,
        string changeType,
        CancellationToken ct = default)
    {
        var indexName = ResolveIndexName(entityType);
        if (indexName is null)
        {
            await auditService.LogErrorAsync(
                $"Unknown entity type '{entityType}' for Elasticsearch indexing.", ct);
            return false;
        }

        try
        {
            if (string.Equals(changeType, "Delete", StringComparison.OrdinalIgnoreCase))
            {
                var delete = await client.DeleteAsync(indexName, entityId, ct);
                return delete.IsValidResponse
                    || delete.Result == Elastic.Clients.Elasticsearch.Result.NotFound;
            }

            var response = await client.IndexAsync<object>(
                JsonSerializer.Deserialize<object>(document)!,
                i => i.Index(indexName).Id(entityId),
                ct);

            if (!response.IsValidResponse)
            {
                await auditService.LogErrorAsync(
                    $"Elasticsearch indexing failed for {entityType}:{entityId} - {response.DebugInformation}", ct);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Exception while indexing {entityType}:{entityId} - {ex.Message}", ct);
            return false;
        }
    }

    private static string? ResolveIndexName(string entityType) => entityType switch
    {
        "Product" => "products_v1",
        "Category" => "categories_v1",
        "Brand" => "brands_v1",
        _ => null
    };
}