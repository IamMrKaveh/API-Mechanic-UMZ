namespace Infrastructure.Security.Models;

public sealed class RateLimitEntry
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string WindowKey { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime ExpiresAt { get; set; }
}