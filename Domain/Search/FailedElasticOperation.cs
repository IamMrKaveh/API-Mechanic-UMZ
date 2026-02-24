namespace Domain.Search;

public class FailedIndexOperation
{
    public string EntityType { get; init; } = default!;
    public string EntityId { get; init; } = default!;
    public string Document { get; init; } = default!;
    public string Error { get; init; } = default!;
    public DateTime Timestamp { get; init; }
    public int RetryCount { get; init; }
}