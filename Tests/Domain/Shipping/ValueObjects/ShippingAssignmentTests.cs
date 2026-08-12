using Domain.Shipping.ValueObjects;

namespace Tests.Domain.Shipping.ValueObjects;

public class ShippingAssignmentTests
{
    [Fact]
    public void Constructor_WithValidValues_StoresAllFields()
    {
        var shippingId = ShippingId.NewId();

        var sut = new ShippingAssignment(shippingId, 1.5m, 20m, 30m, 40m);

        sut.ShippingId.ShouldBe(shippingId);
        sut.Weight.ShouldBe(1.5m);
        sut.Width.ShouldBe(20m);
        sut.Height.ShouldBe(30m);
        sut.Length.ShouldBe(40m);
    }

    [Fact]
    public void Equality_ForSameFieldValues_TreatsInstancesAsEqual()
    {
        var shippingId = ShippingId.NewId();

        var a = new ShippingAssignment(shippingId, 1m, 2m, 3m, 4m);
        var b = new ShippingAssignment(shippingId, 1m, 2m, 3m, 4m);

        a.ShouldBe(b);
    }

    [Theory]
    [InlineData(1.0, 2, 3, 4, 1.1, 2, 3, 4)]
    [InlineData(1.0, 2, 3, 4, 1.0, 9, 3, 4)]
    [InlineData(1.0, 2, 3, 4, 1.0, 2, 9, 4)]
    [InlineData(1.0, 2, 3, 4, 1.0, 2, 3, 9)]
    public void Equality_ForAnyDifferentField_TreatsInstancesAsNotEqual(
        double w1, double wd1, double h1, double l1,
        double w2, double wd2, double h2, double l2)
    {
        var shippingId = ShippingId.NewId();

        var a = new ShippingAssignment(shippingId, (decimal)w1, (decimal)wd1, (decimal)h1, (decimal)l1);
        var b = new ShippingAssignment(shippingId, (decimal)w2, (decimal)wd2, (decimal)h2, (decimal)l2);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Equality_ForDifferentShippingId_TreatsInstancesAsNotEqual()
    {
        var a = new ShippingAssignment(ShippingId.NewId(), 1m, 2m, 3m, 4m);
        var b = new ShippingAssignment(ShippingId.NewId(), 1m, 2m, 3m, 4m);

        a.ShouldNotBe(b);
    }

    [Fact]
    public void WithExpression_ProducesModifiedCopyLeavingOriginalIntact()
    {
        var shippingId = ShippingId.NewId();
        var original = new ShippingAssignment(shippingId, 1m, 2m, 3m, 4m);

        var modified = original with { Weight = 9m };

        original.Weight.ShouldBe(1m);
        modified.Weight.ShouldBe(9m);
        modified.ShippingId.ShouldBe(original.ShippingId);
    }
}
