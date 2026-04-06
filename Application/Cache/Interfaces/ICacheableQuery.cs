namespace Application.Cache.Interfaces;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiry { get; }
}