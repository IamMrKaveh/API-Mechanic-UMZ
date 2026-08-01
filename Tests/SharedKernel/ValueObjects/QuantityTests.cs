using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.SharedKernel.ValueObjects;

public class QuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void Create_WithNonNegativeValue_ReturnsQuantity(int value)
    {
        Quantity.Create(value).Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNegativeValue_ThrowsDomainException(int value)
    {
        Should.Throw<DomainException>(() => Quantity.Create(value));
    }

    [Fact]
    public void Add_TwoQuantities_ReturnsSum()
    {
        var sum = Quantity.Create(3).Add(Quantity.Create(4));

        sum.Value.ShouldBe(7);
    }

    [Fact]
    public void Subtract_WithSmallerRhs_ReturnsDifference()
    {
        var diff = Quantity.Create(10).Subtract(Quantity.Create(3));

        diff.Value.ShouldBe(7);
    }

    [Fact]
    public void Subtract_WithEqualRhs_ReturnsZero()
    {
        Quantity.Create(5).Subtract(Quantity.Create(5)).Value.ShouldBe(0);
    }

    [Fact]
    public void Subtract_WithLargerRhs_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => Quantity.Create(3).Subtract(Quantity.Create(4)));
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsUnderlyingValue()
    {
        int i = Quantity.Create(9);

        i.ShouldBe(9);
    }

    [Fact]
    public void ImplicitConversion_FromInt_ProducesQuantity()
    {
        Quantity q = 12;

        q.Value.ShouldBe(12);
    }

    [Fact]
    public void ImplicitConversion_FromNegativeInt_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => { Quantity q = -1; });
    }

    [Fact]
    public void Equality_ForValueObjectWithSameValue_TreatsInstancesAsEqual()
    {
        Quantity.Create(5).ShouldBe(Quantity.Create(5));
    }

    [Fact]
    public void Equality_ForValueObjectWithDifferentValue_TreatsInstancesAsUnequal()
    {
        Quantity.Create(5).ShouldNotBe(Quantity.Create(6));
    }
}
