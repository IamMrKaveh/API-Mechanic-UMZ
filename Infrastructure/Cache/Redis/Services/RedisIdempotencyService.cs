using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Services;

public sealed class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisIdempotencyService> _logger;
    private readonly string _keyPrefix;
    private readonly TimeSpan _defaultTtl;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisIdempotencyService(
        IConnectionMultiplexer redis,
        IOptions<CacheOptions> options,
        ILogger<RedisIdempotencyService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
        var opts = options.Value ?? new CacheOptions();
        _keyPrefix = string.IsNullOrWhiteSpace(opts.KeyPrefix)
            ? "idem"
            : $"{opts.KeyPrefix}:idem";
        _defaultTtl = TimeSpan.FromHours(24);
    }

    public async Task<bool> HasBeenProcessedAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        if (idempotencyKey == Guid.Empty) return false;

        try
        {
            return await _db.KeyExistsAsync(BuildKey(idempotencyKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency HasBeenProcessed check failed for key {Key}", idempotencyKey);
            return false;
        }
    }

    public async Task MarkAsProcessedAsync(Guid idempotencyKey, string result, CancellationToken ct = default)
    {
        if (idempotencyKey == Guid.Empty) return;

        try
        {
            var payload = new IdempotencyEnvelope
            {
                Result = result,
                ProcessedAt = DateTime.UtcNow
            };

            var serialized = JsonSerializer.Serialize(payload, SerializerOptions);
            await _db.StringSetAsync(
                BuildKey(idempotencyKey),
                serialized,
                _defaultTtl,
                When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Idempotency MarkAsProcessed failed for key {Key}", idempotencyKey);
        }
    }

    public async Task<string?> GetResultAsync(Guid idempotencyKey, CancellationToken ct = default)
    {
        if (idempotencyKey == Guid.Empty) return null;

        try
        {
            var value = await _db.StringGetAsync(BuildKey(idempotencyKey));
            if (!value.HasValue) return null;

            var envelope = JsonSerializer.Deserialize<IdempotencyEnvelope>(value!, SerializerOptions);
            return envelope?.Result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Idempotency GetResult failed for key {Key}", idempotencyKey);
            return null;
        }
    }

    private string BuildKey(Guid idempotencyKey) => $"{_keyPrefix}:{idempotencyKey:N}";

    private sealed class IdempotencyEnvelope
    {
        public string Result { get; init; } = string.Empty;
        public DateTime ProcessedAt { get; init; }
    }
}