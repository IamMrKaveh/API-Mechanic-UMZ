namespace Infrastructure.Cache.Redis.Lock;

public sealed class DistributedLockException(string message) : Exception(message)
{
}