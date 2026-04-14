using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Xml.Linq;

namespace Infrastructure.DataProtection.Repositories;

public sealed class ResilientRedisXmlRepository(
    IConnectionMultiplexer redis,
    ILogger<ResilientRedisXmlRepository> logger,
    string keyPrefix,
    TimeSpan keyExpiration) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var elements = new List<XElement>();

        try
        {
            if (!redis.IsConnected)
            {
                logger.LogWarning("Redis not connected. Returning empty data protection key collection.");
                return elements;
            }

            var db = redis.GetDatabase();
            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{keyPrefix}:*").ToList();

            if (!keys.Any())
                return elements;

            var values = db.StringGet(keys.Select(k => (RedisKey)k.ToString()).ToArray());

            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    try
                    {
                        elements.Add(XElement.Parse(value!));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to parse data protection XML element from Redis.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading data protection keys from Redis.");
        }

        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        try
        {
            if (!redis.IsConnected)
            {
                logger.LogWarning("Redis not connected. Cannot store data protection key.");
                return;
            }

            var db = redis.GetDatabase();
            var key = $"{keyPrefix}:{friendlyName}";
            db.StringSet(key, element.ToString(), keyExpiration);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error storing data protection key to Redis for {FriendlyName}.", friendlyName);
        }
    }
}