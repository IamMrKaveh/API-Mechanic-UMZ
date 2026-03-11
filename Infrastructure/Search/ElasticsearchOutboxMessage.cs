namespace Infrastructure.Search;

public sealed class ElasticsearchOutboxMessage
{
    public int Id { get; private set; }
    public string IndexName { get; private set; } = string.Empty;
    public string DocumentId { get; private set; } = string.Empty;
    public string OperationType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private ElasticsearchOutboxMessage()
    { }

    public static ElasticsearchOutboxMessage Create(string indexName, string documentId, string operationType, string payload)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("Index name is required.", nameof(indexName));

        if (string.IsNullOrWhiteSpace(documentId))
            throw new ArgumentException("Document ID is required.", nameof(documentId));

        if (string.IsNullOrWhiteSpace(operationType))
            throw new ArgumentException("Operation type is required.", nameof(operationType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        return new ElasticsearchOutboxMessage
        {
            IndexName = indexName.Trim(),
            DocumentId = documentId.Trim(),
            OperationType = operationType.Trim(),
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message is required.", nameof(error));

        Error = error;
        RetryCount++;
    }

    public bool IsProcessed => ProcessedAt.HasValue;

    public bool HasExceededMaxRetries(int maxRetries) => RetryCount >= maxRetries;
}