namespace Domain.Search;

public class ElasticsearchOutboxMessage
{
    public int Id { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OperationType { get; set; }
    public string? Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Document { get; set; }
    public string? ChangeType { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}