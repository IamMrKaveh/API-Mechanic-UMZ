using Domain.Order.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Order.ValueObjects;

public class OrderNumberTests
{
    [Fact]
    public void Generate_ProducesOrderNumberWithOrdPrefixAndYyyymmddDateSegment()
    {
        var date = new DateOnly(2026, 8, 4);

        var sut = OrderNumber.Generate(date);

        sut.Value.ShouldStartWith("ORD-20260804-");
    }

    [Fact]
    public void Generate_UniquePartIs8UppercaseHexCharacters()
    {
        var date = new DateOnly(2026, 8, 4);

        var sut = OrderNumber.Generate(date);

        sut.Value.Length.ShouldBe(4 + 8 + 1 + 8);
        var uniquePart = sut.Value["ORD-20260804-".Length..];
        uniquePart.Length.ShouldBe(8);
        uniquePart.ShouldMatch("^[0-9A-F]{8}$");
    }

    [Fact]
    public void Generate_TwoInvocationsProduceDifferentNumbers()
    {
        var date = new DateOnly(2026, 8, 4);

        OrderNumber.Generate(date).Value.ShouldNotBe(OrderNumber.Generate(date).Value);
    }

    [Fact]
    public void Create_WithValidValue_UppercasesAndTrims()
    {
        OrderNumber.Create("  ord-20260804-abcd1234  ").Value.ShouldBe("ORD-20260804-ABCD1234");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        Should.Throw<DomainException>(() => OrderNumber.Create(input!));
    }

    [Fact]
    public void Equality_ForRecordWithSameValue_TreatsInstancesAsEqual()
    {
        OrderNumber.Create("ORD-1").ShouldBe(OrderNumber.Create("ord-1"));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var sut = OrderNumber.Create("ORD-ABC");

        sut.ToString().ShouldBe("ORD-ABC");
    }
}
