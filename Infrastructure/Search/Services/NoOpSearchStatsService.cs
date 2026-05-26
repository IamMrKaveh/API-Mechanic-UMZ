namespace Infrastructure.Search.Services;

public class NoOpSearchStatsService : ISearchStatsService
{
    public Task<SearchStatsResult> GetStatsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new SearchStatsResult(
            IsAvailable: false,
            UnavailableReason: "سرویس جستجو غیرفعال است."));
    }
}