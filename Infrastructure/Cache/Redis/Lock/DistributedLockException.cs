namespace Infrastructure.Cache.Redis.Lock;

public sealed class DistributedLockException : Exception
{
    public DistributedLockException(string message) : base(message)
    {
    }
}