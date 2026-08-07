using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Order.ValueObjects;

public class OrderItemSnapshotTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsSnapshotWithAllFields()
    {
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();
        var productName = ProductName.Create("Product X");
        var sku = Sku.Create("SKU-001");
        var unitPrice = Money.Create(100m, "IRT");

        var sut = OrderItemSnapshot.Create(variantId, productId, productName, sku, unitPrice, 3);

        sut.VariantId.ShouldBe(variantId);
        sut.ProductId.ShouldBe(productId);
        sut.ProductName.ShouldBe(productName);
        sut.Sku.ShouldBe(sku);
        sut.UnitPrice.ShouldBe(unitPrice);
        sut.Quantity.ShouldBe(3);
    }

    [Fact]
    public void Create_WithNullVariantId_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            OrderItemSnapshot.Create(null!, ProductId.NewId(),
                ProductName.Create("PP"), Sku.Create("S"), Money.Create(10m, "IRT"), 1));
    }

    [Fact]
    public void Create_WithNullProductId_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            OrderItemSnapshot.Create(VariantId.NewId(), null!,
                ProductName.Create("PP"), Sku.Create("S"), Money.Create(10m, "IRT"), 1));
    }

    [Fact]
    public void Create_WithNullProductName_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            OrderItemSnapshot.Create(VariantId.NewId(), ProductId.NewId(),
                null!, Sku.Create("S"), Money.Create(10m, "IRT"), 1));
    }

    [Fact]
    public void Create_WithNullSku_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            OrderItemSnapshot.Create(VariantId.NewId(), ProductId.NewId(),
                ProductName.Create("PP"), null!, Money.Create(10m, "IRT"), 1));
    }

    [Fact]
    public void Create_WithNullUnitPrice_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            OrderItemSnapshot.Create(VariantId.NewId(), ProductId.NewId(),
                ProductName.Create("PP"), Sku.Create("S"), null!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithZeroOrNegativeQuantity_ThrowsDomainException(int quantity)
    {
        Should.Throw<DomainException>(() =>
            OrderItemSnapshot.Create(VariantId.NewId(), ProductId.NewId(),
                ProductName.Create("PP"), Sku.Create("S"), Money.Create(10m, "IRT"), quantity));
    }

    [Fact]
    public void Equality_ForRecordWithSameMembers_TreatsInstancesAsEqual()
    {
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();
        var productName = ProductName.Create("PP");
        var sku = Sku.Create("S");
        var unitPrice = Money.Create(10m, "IRT");

        var a = OrderItemSnapshot.Create(variantId, productId, productName, sku, unitPrice, 1);
        var b = OrderItemSnapshot.Create(variantId, productId, productName, sku, unitPrice, 1);

        a.ShouldBe(b);
    }
}
