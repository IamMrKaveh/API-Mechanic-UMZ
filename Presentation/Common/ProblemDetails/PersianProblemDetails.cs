namespace Presentation.Common.ProblemDetails;

public sealed record PersianProblemDetails
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "about:blank";

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("errors")]
    public IDictionary<string, string[]>? Errors { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
