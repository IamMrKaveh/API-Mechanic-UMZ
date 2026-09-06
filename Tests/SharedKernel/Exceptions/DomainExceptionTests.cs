using SharedKernel.Exceptions;

namespace Tests.SharedKernel.Exceptions;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessageOnly_UsesDomainErrorCodeAndEmptyArgs()
    {
        var ex = new DomainException("something went wrong");

        ex.Message.ShouldBe("something went wrong");
        ex.ErrorCode.ShouldBe("DOMAIN_ERROR");
        ex.Args.ShouldNotBeNull();
        ex.Args.Count.ShouldBe(0);
        ex.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithCodeAndMessage_PreservesBoth()
    {
        var ex = new DomainException("ORDER_CLOSED", "order is closed");

        ex.ErrorCode.ShouldBe("ORDER_CLOSED");
        ex.Message.ShouldBe("order is closed");
        ex.Args.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankCode_FallsBackToDomainError(string? code)
    {
        var ex = new DomainException(code!, "msg", null, null);

        ex.ErrorCode.ShouldBe("DOMAIN_ERROR");
    }

    [Fact]
    public void Constructor_WithArgs_PreservesArgs()
    {
        var args = new Dictionary<string, object?> { ["orderId"] = "123", ["amount"] = null };

        var ex = new DomainException("CODE", "msg", args);

        ex.Args.ShouldBe(args);
        ex.Args["orderId"].ShouldBe("123");
    }

    [Fact]
    public void Constructor_WithNullArgs_UsesEmptyDictionary()
    {
        var ex = new DomainException("CODE", "msg", null);

        ex.Args.ShouldNotBeNull();
        ex.Args.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInner()
    {
        var inner = new InvalidOperationException("root cause");

        var ex = new DomainException("wrapper", inner);

        ex.Message.ShouldBe("wrapper");
        ex.ErrorCode.ShouldBe("DOMAIN_ERROR");
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void FullConstructor_PreservesAllMembers()
    {
        var inner = new Exception("inner");
        var args = new Dictionary<string, object?> { ["k"] = "v" };

        var ex = new DomainException("CODE_X", "full", args, inner);

        ex.ErrorCode.ShouldBe("CODE_X");
        ex.Message.ShouldBe("full");
        ex.Args.ShouldBe(args);
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public void DomainException_IsAnException()
    {
        new DomainException("m").ShouldBeAssignableTo<Exception>();
    }

    [Fact]
    public void ErrorCode_IsVirtual_AllowsOverride()
    {
        var ex = new OverridingException();

        ex.ErrorCode.ShouldBe("OVERRIDDEN");
        ex.ShouldBeAssignableTo<DomainException>();
    }

    private sealed class OverridingException : DomainException
    {
        public OverridingException() : base("base message")
        { }

        public override string ErrorCode => "OVERRIDDEN";
    }
}
