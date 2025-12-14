namespace Domain.Search;

public class FailedElasticOperation
{
    public int Id { get; set; }
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string Document { get; set; } = default!;
    public string Error { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public string Status { get; set; } = default!;
}