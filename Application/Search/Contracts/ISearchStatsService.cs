namespace Application.Search.Contracts;

public interface ISearchStatsService
{
    Task<SearchStatsResult> GetStatsAsync(CancellationToken ct = default);
}

public record SearchStatsResult(
    bool IsAvailable,
    string? Status = null,
    long TotalDocuments = 0,
    string? ClusterName = null,
    int NumberOfNodes = 0,
    int ActivePrimaryShards = 0,
    string? UnavailableReason = null);