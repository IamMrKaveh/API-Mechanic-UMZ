using SharedKernel.Exceptions;

namespace Tests.SharedKernel.Exceptions;

public class ExternalServiceExceptionTests
{
    [Fact]
    public void Constructor_WithServiceAndMessage_SetsPropertiesAndNullCode()
    {
        var ex = new ExternalServiceException("Zarinpal", "gateway down");

        ex.ServiceName.ShouldBe("Zarinpal");
        ex.Message.ShouldBe("gateway down");
        ex.ErrorCode.ShouldBeNull();
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithErrorCode_PreservesCode()
    {
        var ex = new ExternalServiceException("Zarinpal", "not found", "-51");

        ex.ServiceName.ShouldBe("Zarinpal");
        ex.Message.ShouldBe("not found");
        ex.ErrorCode.ShouldBe("-51");
    }

    [Fact]
    public void Constructor_WithNullErrorCode_KeepsNull()
    {
        var ex = new ExternalServiceException("Svc", "msg", (string?)null);

        ex.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerAndNullCode()
    {
        var inner = new HttpRequestException("network");

        var ex = new ExternalServiceException("Svc", "failed", inner);

        ex.ServiceName.ShouldBe("Svc");
        ex.InnerException.ShouldBeSameAs(inner);
        ex.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithCodeAndInnerException_PreservesBoth()
    {
        var inner = new TimeoutException("timed out");

        var ex = new ExternalServiceException("Svc", "failed", "TIMEOUT", inner);

        ex.ServiceName.ShouldBe("Svc");
        ex.Message.ShouldBe("failed");
        ex.ErrorCode.ShouldBe("TIMEOUT");
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void Exception_IsAnException()
    {
        new ExternalServiceException("S", "m").ShouldBeAssignableTo<Exception>();
    }
}
