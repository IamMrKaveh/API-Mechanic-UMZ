namespace Application.Wallet.Features.Shared;

public sealed record WalletOverviewFilter
{
    public string? Search { get; init; }
    public bool? IsFrozen { get; init; }
    public decimal? MinBalance { get; init; }
    public decimal? MaxBalance { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public string? SortBy { get; init; }
}