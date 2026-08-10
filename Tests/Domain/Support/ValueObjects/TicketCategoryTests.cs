using Domain.Support.ValueObjects;
using SharedKernel.Abstractions;

namespace Tests.Domain.Support.ValueObjects;

public class TicketCategoryTests
{
    [Theory]
    [InlineData("Billing")]
    [InlineData("Technical")]
    [InlineData("پرداخت")]
    public void Create_WithValidValue_ReturnsCategoryWithThatValue(string input)
    {
        TicketCategory.Create(input).Value.ShouldBe(input);
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsBeforeStoring()
    {
        TicketCategory.Create("  Billing  ").Value.ShouldBe("Billing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespace_ThrowsArgumentException(string? input)
    {
        Should.Throw<ArgumentException>(() => TicketCategory.Create(input!));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        TicketCategory.Create("Billing").ToString().ShouldBe("Billing");
    }

    [Fact]
    public void Equality_IsCaseInsensitive()
    {
        TicketCategory.Create("Billing").ShouldBe(TicketCategory.Create("BILLING"));
    }

    [Fact]
    public void Equality_ForDifferentValues_TreatsInstancesAsUnequal()
    {
        TicketCategory.Create("Billing").ShouldNotBe(TicketCategory.Create("Technical"));
    }

    [Fact]
    public void GetHashCode_IsCaseInsensitive()
    {
        TicketCategory.Create("Billing").GetHashCode()
            .ShouldBe(TicketCategory.Create("billing").GetHashCode());
    }

    [Fact]
    public void IsAssignableToValueObjectBase()
    {
        TicketCategory.Create("Billing").ShouldBeAssignableTo<ValueObject>();
    }
}
