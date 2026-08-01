using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class IpAddressTests
{
    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("2001:db8::1")]
    public void Create_WithValidIp_ReturnsInstance(string input)
    {
        IpAddress.Create(input).Value.ShouldBe(input.Trim());
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        IpAddress.Create("  10.0.0.1  ").Value.ShouldBe("10.0.0.1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        Should.Throw<ArgumentException>(() => IpAddress.Create(input));
    }

    [Theory]
    [InlineData("not-an-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("256.0.0.1")]
    [InlineData("1.2.3")]
    public void Create_WithInvalidIp_ThrowsArgumentException(string input)
    {
        Should.Throw<ArgumentException>(() => IpAddress.Create(input));
    }

    [Fact]
    public void Unknown_HasZeroAddress()
    {
        IpAddress.Unknown.Value.ShouldBe("0.0.0.0");
    }

    [Fact]
    public void System_HasLoopbackAddress()
    {
        IpAddress.System.Value.ShouldBe("127.0.0.1");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        IpAddress.Create("10.0.0.1").ToString().ShouldBe("10.0.0.1");
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValue_TreatsInstancesAsEqual()
    {
        IpAddress.Create("10.0.0.1").ShouldBe(IpAddress.Create("10.0.0.1"));
    }
}
