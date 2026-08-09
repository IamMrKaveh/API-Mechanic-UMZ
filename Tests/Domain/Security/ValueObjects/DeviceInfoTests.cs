using Domain.Security.ValueObjects;
using SharedKernel.Abstractions;

namespace Tests.Domain.Security.ValueObjects;

public class DeviceInfoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ReturnsUnknown(string? input)
    {
        DeviceInfo.Create(input!).Value.ShouldBe("Unknown");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBeforeStoring()
    {
        DeviceInfo.Create("  iPhone 15 Pro  ").Value.ShouldBe("iPhone 15 Pro");
    }

    [Fact]
    public void Create_WithValueBelowMaxLength_ReturnsValueUnchanged()
    {
        var input = "Chrome 120 on Windows 11";

        DeviceInfo.Create(input).Value.ShouldBe(input);
    }

    [Fact]
    public void Create_AtExactlyFiveHundredChars_ReturnsValueUnchanged()
    {
        var input = new string('a', 500);

        DeviceInfo.Create(input).Value.Length.ShouldBe(500);
    }

    [Fact]
    public void Create_AboveFiveHundredChars_TruncatesToFiveHundred()
    {
        var input = new string('a', 750);

        DeviceInfo.Create(input).Value.Length.ShouldBe(500);
    }

    [Fact]
    public void Unknown_StaticFactory_ReturnsInstanceWithUnknownLiteral()
    {
        DeviceInfo.Unknown.Value.ShouldBe("Unknown");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        DeviceInfo.Create("Firefox 121").ToString().ShouldBe("Firefox 121");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        string s = DeviceInfo.Create("Safari 17");

        s.ShouldBe("Safari 17");
    }

    [Fact]
    public void Equality_IsCaseInsensitiveViaGetEqualityComponents()
    {
        DeviceInfo.Create("Chrome").ShouldBe(DeviceInfo.Create("CHROME"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        DeviceInfo.Create("Chrome").ShouldNotBe(DeviceInfo.Create("Firefox"));
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        DeviceInfo.Create("Any").ShouldBeAssignableTo<ValueObject>();
    }
}
