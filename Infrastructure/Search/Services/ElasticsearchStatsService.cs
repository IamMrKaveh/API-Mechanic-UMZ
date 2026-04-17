using Application.Audit.Contracts;
using Application.Search.Contracts;
using Elastic.Clients.Elasticsearch;

namespace Infrastructure.Search.Services;

public sealed class ElasticsearchStatsService(
    ElasticsearchClient client,
    IAuditService auditService) : ISearchStatsService
{
    public async Task<SearchStatsResult> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var healthResponse = await client.Cluster.HealthAsync(cancellationToken: ct);

            if (!healthResponse.IsValidResponse)
            {
                return new SearchStatsResult(
                    IsAvailable: false,
                    UnavailableReason: healthResponse.DebugInformation);
            }

            var statsResponse = await client.Indices.StatsAsync(cancellationToken: ct);
            var totalDocs = statsResponse.IsValidResponse
                ? statsResponse.Indices?.Values.Sum(i => i.Total?.Docs?.Count ?? 0) ?? 0
                : 0;

            return new SearchStatsResult(
                IsAvailable: true,
                Status: healthResponse.Status.ToString(),
                TotalDocuments: totalDocs,
                ClusterName: healthResponse.ClusterName,
                NumberOfNodes: healthResponse.NumberOfNodes,
                ActivePrimaryShards: healthResponse.ActivePrimaryShards);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"GetStatsAsync failed: {ex.Message}", ct);
            return new SearchStatsResult(IsAvailable: false, UnavailableReason: ex.Message);
        }
    }
}