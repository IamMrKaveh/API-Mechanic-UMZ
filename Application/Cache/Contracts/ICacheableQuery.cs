namespace Application.Cache.Contracts;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiry { get; }
}