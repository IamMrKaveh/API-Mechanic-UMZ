using Domain.Inventory.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Inventory.ValueObjects;

public class WarehouseCodeTests
{
    [Theory]
    [InlineData("AB")]
    [InlineData("WH-01")]
    [InlineData("WAREHOUSE_MAIN_2024")]
    [InlineData("A1")]
    public void Create_WithValidCode_ReturnsNormalizedInstance(string input)
    {
        WarehouseCode.Create(input).Value.ShouldBe(input.Trim().ToUpperInvariant());
    }

    [Fact]
    public void Create_WithLowercaseInput_NormalizesToUppercase()
    {
        WarehouseCode.Create("wh-01").Value.ShouldBe("WH-01");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsThenUppercases()
    {
        WarehouseCode.Create("  wh01  ").Value.ShouldBe("WH01");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsDomainException(string? input)
    {
        var ex = Should.Throw<DomainException>(() => WarehouseCode.Create(input!));

        ex.Message.ShouldBe("کد انبار الزامی است.");
    }

    [Fact]
    public void Create_WithSingleCharacter_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() => WarehouseCode.Create("A"));

        ex.Message.ShouldContain("2");
        ex.Message.ShouldContain("20");
    }

    [Fact]
    public void Create_WithLengthAboveMax_ThrowsDomainException()
    {
        var tooLong = new string('A', 21);

        var ex = Should.Throw<DomainException>(() => WarehouseCode.Create(tooLong));

        ex.Message.ShouldContain("20");
    }

    [Theory]
    [InlineData("WH 01")]
    [InlineData("WH#01")]
    [InlineData("WH@01")]
    [InlineData("WH.01")]
    [InlineData("WH/01")]
    public void Create_WithInvalidCharacters_ThrowsDomainException(string input)
    {
        var ex = Should.Throw<DomainException>(() => WarehouseCode.Create(input));

        ex.Message.ShouldContain("کد انبار فقط می‌تواند");
    }

    [Fact]
    public void ImplicitOperator_ToString_ReturnsUnderlyingValue()
    {
        var code = WarehouseCode.Create("WH-01");

        string extracted = code;

        extracted.ShouldBe("WH-01");
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        WarehouseCode.Create("WH-01").ToString().ShouldBe("WH-01");
    }

    [Fact]
    public void Equality_TwoCodesWithSameNormalizedValue_TreatedAsEqual()
    {
        WarehouseCode.Create("wh-01").ShouldBe(WarehouseCode.Create("WH-01"));
    }
}
