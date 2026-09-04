using Domain.Product.ValueObjects;
using Domain.Variant.Events;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Variant.Events;

public class VariantEventsTests
{
    [Fact]
    public void VariantCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();
        var sku = Sku.Create($"SKU-{Guid.NewGuid():N}"[..20]);
        var price = Money.FromDecimal(120_000m);

        var sut = new VariantCreatedEvent(variantId, productId, sku, price);

        sut.VariantId.ShouldBe(variantId);
        sut.ProductId.ShouldBe(productId);
        sut.Sku.ShouldBe(sku);
        sut.Price.ShouldBe(price);
    }

    [Fact]
    public void ProductVariantPriceChangedEvent_ExposesPreviousAndNewPrice()
    {
        var previous = Money.FromDecimal(100_000m);
        var next = Money.FromDecimal(130_000m);

        var sut = new ProductVariantPriceChangedEvent(
            VariantId.NewId(), ProductId.NewId(), previous, next);

        sut.PreviousPrice.ShouldBe(previous);
        sut.NewPrice.ShouldBe(next);
    }

    [Fact]
    public void VariantRemovedEvent_ExposesProductBeforeVariant()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();

        var sut = new VariantRemovedEvent(productId, variantId);

        sut.ProductId.ShouldBe(productId);
        sut.VariantId.ShouldBe(variantId);
    }

    [Fact]
    public void VariantAttributeSetEvent_ExposesIds()
    {
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();

        var sut = new VariantAttributeSetEvent(variantId, productId);

        sut.VariantId.ShouldBe(variantId);
        sut.ProductId.ShouldBe(productId);
    }

    [Fact]
    public void VariantShippingSetEvent_ExposesIds()
    {
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();

        var sut = new VariantShippingSetEvent(variantId, productId);

        sut.VariantId.ShouldBe(variantId);
        sut.ProductId.ShouldBe(productId);
    }
}
