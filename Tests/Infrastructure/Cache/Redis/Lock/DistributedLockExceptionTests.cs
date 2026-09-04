using Infrastructure.Cache.Redis.Lock;

namespace Tests.Infrastructure.Cache.Redis.Lock;

public class DistributedLockExceptionTests
{
    [Fact]
    public void Constructor_PreservesMessage()
    {
        var sut = new DistributedLockException("lock timeout");

        sut.Message.ShouldBe("lock timeout");
        sut.ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void Constructor_WithoutInnerException_HasNullInner()
    {
        var sut = new DistributedLockException("x");

        sut.InnerException.ShouldBeNull();
    }
}
