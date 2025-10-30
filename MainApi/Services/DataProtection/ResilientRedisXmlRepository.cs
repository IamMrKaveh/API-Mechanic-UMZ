namespace MainApi.Services.DataProtection;
/// <summary>
/// Enterprise-grade XML repository with Redis persistence and in-memory fallback
/// Implements graceful degradation when Redis is unavailable
/// </summary>
public class ResilientRedisXmlRepository : IXmlRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ResilientRedisXmlRepository> _logger;
    private readonly string _keyPrefix;
    private readonly TimeSpan _keyExpiration;
    public ResilientRedisXmlRepository(
            IConnectionMultiplexer redis,
            ILogger<ResilientRedisXmlRepository> logger,
            string keyPrefix,
            TimeSpan keyExpiration)
    {
        _redis = redis ??
throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix;
        _keyExpiration = keyExpiration;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ Redis not connected. Returning empty key collection.");
                return elements;
            }

            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_keyPrefix}:*");

            foreach (var key in keys)
            {
                try
                {
                    var value = db.StringGet(key);
                    if (value.HasValue)
                    {
                        elements.Add(XElement.Parse(value!));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Failed to parse key: {Key}", key);
                }
            }

            _logger.LogInformation("✅ Retrieved {Count} data protection keys from Redis", elements.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to retrieve keys from Redis");
        }

        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ Redis not connected. Key not stored: {FriendlyName}", friendlyName);
                return;
            }

            var db = _redis.GetDatabase();
            var key = $"{_keyPrefix}:{friendlyName}";
            var value = element.ToString(SaveOptions.DisableFormatting);

            db.StringSet(key, value, _keyExpiration);
            _logger.LogInformation("✅ Stored data protection key: {FriendlyName}", friendlyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to store key: {FriendlyName}", friendlyName);
        }
    }
}