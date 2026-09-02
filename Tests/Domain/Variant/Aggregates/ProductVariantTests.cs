using Domain.Attribute.ValueObjects;
using Domain.Product.Exceptions;
using Domain.Product.ValueObjects;
using Domain.Shipping.ValueObjects;
using Domain.Variant.Events;
using Domain.Variant.Exceptions;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Variant.Aggregates;

public class ProductVariantTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedProductVariant()
    {
        var id = VariantId.NewId();
        var productId = ProductId.NewId();

        var sut = new ProductVariantBuilder()
            .WithId(id)
            .WithProductId(productId)
            .WithSku("SKU-1")
            .WithSellingPrice(100m)
            .WithOriginalPrice(150m)
            .Build();

        sut.Id.ShouldBe(id);
        sut.ProductId.ShouldBe(productId);
        sut.Sku.Value.ShouldBe("SKU-1");
        sut.SellingPrice.Amount.ShouldBe(100m);
        sut.OriginalPrice.Amount.ShouldBe(150m);
        sut.IsActive.ShouldBeTrue();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
        sut.UpdatedAt.ShouldBeNull();
        sut.Attributes.ShouldBeEmpty();
        sut.Shippings.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new ProductVariantBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithoutOriginalPrice_FallsBackToSellingPrice()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(120m)
            .WithOriginalPrice(null)
            .Build();

        sut.OriginalPrice.Amount.ShouldBe(120m);
        sut.OriginalPrice.Currency.ShouldBe(sut.SellingPrice.Currency);
    }

    [Fact]
    public void Create_RaisesExactlyOneVariantCreatedEvent()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<VariantCreatedEvent>();
        evt.VariantId.ShouldBe(sut.Id);
        evt.ProductId.ShouldBe(sut.ProductId);
        evt.Sku.ShouldBe(sut.Sku);
        evt.Price.Amount.ShouldBe(sut.SellingPrice.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveSellingPrice_ThrowsInvalidPriceException(decimal amount)
    {
        Should.Throw<InvalidPriceException>(
            () => new ProductVariantBuilder().WithSellingPrice(amount).Build());
    }

    [Fact]
    public void Create_WhenOriginalPriceLessThanSellingPrice_ThrowsInvalidPriceException()
    {
        Should.Throw<InvalidPriceException>(
            () => new ProductVariantBuilder()
                .WithSellingPrice(200m)
                .WithOriginalPrice(150m)
                .Build());
    }

    [Fact]
    public void IsDiscounted_WhenOriginalGreaterThanSelling_ReturnsTrue()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100m)
            .WithOriginalPrice(150m)
            .Build();

        sut.IsDiscounted.ShouldBeTrue();
    }

    [Fact]
    public void IsDiscounted_WhenOriginalEqualsSelling_ReturnsFalse()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100m)
            .WithOriginalPrice(100m)
            .Build();

        sut.IsDiscounted.ShouldBeFalse();
    }

    [Fact]
    public void DiscountPercentage_WhenDiscounted_ReturnsRoundedPercent()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(75m)
            .WithOriginalPrice(100m)
            .Build();

        sut.DiscountPercentage.ShouldBe(25m);
    }

    [Fact]
    public void DiscountPercentage_WhenNotDiscounted_ReturnsNull()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100m)
            .WithOriginalPrice(100m)
            .Build();

        sut.DiscountPercentage.ShouldBeNull();
    }

    [Fact]
    public void ChangePrice_WithValidInput_UpdatesPricesAndBumpsUpdatedAt()
    {
        var sut = new ProductVariantBuilder().WithSellingPrice(100m).Build();

        sut.ChangePrice(Money.Create(80m, "IRT"), Money.Create(120m, "IRT"));

        sut.SellingPrice.Amount.ShouldBe(80m);
        sut.OriginalPrice.Amount.ShouldBe(120m);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangePrice_WithDifferentSellingPrice_RaisesProductVariantPriceChangedEvent()
    {
        var sut = new ProductVariantBuilder().WithSellingPrice(100m).Build();
        sut.ClearDomainEvents();

        sut.ChangePrice(Money.Create(90m, "IRT"), Money.Create(120m, "IRT"));

        var evt = sut.DomainEvents.Single().ShouldBeOfType<ProductVariantPriceChangedEvent>();
        evt.VariantId.ShouldBe(sut.Id);
        evt.ProductId.ShouldBe(sut.ProductId);
        evt.NewPrice.Amount.ShouldBe(90m);
    }

    [Fact]
    public void ChangePrice_WithSameSellingAndOriginal_DoesNotRaisePriceChangedEvent()
    {
        var sut = new ProductVariantBuilder().WithSellingPrice(100m).WithOriginalPrice(150m).Build();
        sut.ClearDomainEvents();

        sut.ChangePrice(Money.Create(100m, "IRT"), Money.Create(150m, "IRT"));

        sut.DomainEvents.OfType<ProductVariantPriceChangedEvent>().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ChangePrice_WithNonPositiveSellingPrice_ThrowsInvalidPriceException(decimal amount)
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<InvalidPriceException>(
            () => sut.ChangePrice(amount));
    }

    [Fact]
    public void ChangePrice_WhenOriginalLessThanSelling_ThrowsInvalidPriceException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<InvalidPriceException>(
            () => sut.ChangePrice(Money.Create(200m, "IRT"), Money.Create(150m, "IRT")));
    }

    [Fact]
    public void ChangePrice_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.ChangePrice(Money.Create(50m, "IRT")));
    }

    [Fact]
    public void ChangeSku_WithDifferentValue_UpdatesSkuAndBumpsUpdatedAt()
    {
        var sut = new ProductVariantBuilder().WithSku("OLD-SKU").Build();
        var newSku = Sku.Create("NEW-SKU");

        sut.ChangeSku(newSku);

        sut.Sku.Value.ShouldBe("NEW-SKU");
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangeSku_WithSameValue_LeavesUpdatedAtUnchanged()
    {
        var sut = new ProductVariantBuilder().WithSku("SAME-SKU").Build();

        sut.ChangeSku(Sku.Create("SAME-SKU"));

        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void ChangeSku_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.ChangeSku(Sku.Create("NEW")));
    }

    [Fact]
    public void SetAttributes_WithNewAssignments_AddsAttributesAndRaisesEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();
        var assignment = new AttributeAssignment(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red");

        sut.SetAttributes(new[] { assignment });

        sut.Attributes.Count.ShouldBe(1);
        sut.Attributes.Single().ValueId.ShouldBe(assignment.ValueId);
        sut.Attributes.Single().DisplayValue.ShouldBe("Red");
        sut.DomainEvents.Single().ShouldBeOfType<VariantAttributeSetEvent>();
    }

    [Fact]
    public void SetAttributes_WithDuplicateValueIds_KeepsOnlyOneEntry()
    {
        var sut = new ProductVariantBuilder().Build();
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();
        var first = new AttributeAssignment(typeId, valueId, "Red");
        var second = new AttributeAssignment(typeId, valueId, "Rouge");

        sut.SetAttributes(new[] { first, second });

        sut.Attributes.Count.ShouldBe(1);
    }

    [Fact]
    public void SetAttributes_WhenCalledAgainWithSubset_RemovesMissingAssignmentsAndUpdatesRemaining()
    {
        var sut = new ProductVariantBuilder().Build();
        var typeId = AttributeTypeId.NewId();
        var keepValueId = AttributeValueId.NewId();
        var removeValueId = AttributeValueId.NewId();
        sut.SetAttributes(new[]
        {
            new AttributeAssignment(typeId, keepValueId, "Red"),
            new AttributeAssignment(typeId, removeValueId, "Blue")
        });

        sut.SetAttributes(new[]
        {
            new AttributeAssignment(typeId, keepValueId, "Crimson")
        });

        sut.Attributes.Count.ShouldBe(1);
        sut.Attributes.Single().ValueId.ShouldBe(keepValueId);
        sut.Attributes.Single().DisplayValue.ShouldBe("Crimson");
    }

    [Fact]
    public void SetAttributes_WithEmptyEnumeration_ClearsAllAttributes()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.SetAttributes(new[]
        {
            new AttributeAssignment(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red")
        });

        sut.SetAttributes(Array.Empty<AttributeAssignment>());

        sut.Attributes.ShouldBeEmpty();
    }

    [Fact]
    public void SetAttributes_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.SetAttributes(new[]
            {
                new AttributeAssignment(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red")
            }));
    }

    [Fact]
    public void SetShippingMethods_WithValidMultiplierAndAssignments_AddsShippingAndRaisesEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();
        var shippingId = ShippingId.NewId();
        var assignment = new ShippingAssignment(shippingId, 1m, 2m, 3m, 4m);

        sut.SetShippingMethods(1.5m, new[] { assignment });

        sut.Shippings.Count.ShouldBe(1);
        var single = sut.Shippings.Single();
        single.ShippingId.ShouldBe(shippingId);
        single.Weight.ShouldBe(1m);
        single.Width.ShouldBe(2m);
        single.Height.ShouldBe(3m);
        single.Length.ShouldBe(4m);
        single.ShippingMultiplier.ShouldBe(1.5m);
        sut.DomainEvents.Single().ShouldBeOfType<VariantShippingSetEvent>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void SetShippingMethods_WithNonPositiveMultiplier_ThrowsDomainException(decimal multiplier)
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(
            () => sut.SetShippingMethods(multiplier, new[] { new ShippingAssignment(ShippingId.NewId(), 1m, 1m, 1m, 1m) }));
    }

    [Fact]
    public void SetShippingMethods_WithDuplicateShippingIds_KeepsOnlyOneEntry()
    {
        var sut = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();

        sut.SetShippingMethods(1m, new[]
        {
            new ShippingAssignment(shippingId, 1m, 1m, 1m, 1m),
            new ShippingAssignment(shippingId, 2m, 2m, 2m, 2m)
        });

        sut.Shippings.Count.ShouldBe(1);
    }

    [Fact]
    public void SetShippingMethods_WhenCalledAgainWithSubset_RemovesMissingAssignments()
    {
        var sut = new ProductVariantBuilder().Build();
        var keepId = ShippingId.NewId();
        var removeId = ShippingId.NewId();
        sut.SetShippingMethods(1m, new[]
        {
            new ShippingAssignment(keepId, 1m, 1m, 1m, 1m),
            new ShippingAssignment(removeId, 2m, 2m, 2m, 2m)
        });

        sut.SetShippingMethods(1m, new[] { new ShippingAssignment(keepId, 5m, 5m, 5m, 5m) });

        sut.Shippings.Count.ShouldBe(1);
        sut.Shippings.Single().ShippingId.ShouldBe(keepId);
    }

    [Fact]
    public void SetShippingMethods_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.SetShippingMethods(1m, new[] { new ShippingAssignment(ShippingId.NewId(), 1m, 1m, 1m, 1m) }));
    }

    [Fact]
    public void Remove_MarksVariantAsInactiveAndDeleted()
    {
        var sut = new ProductVariantBuilder().Build();
        var deletedBy = Guid.NewGuid();

        sut.Remove(deletedBy);

        sut.IsActive.ShouldBeFalse();
        sut.IsDeleted.ShouldBeTrue();
        sut.DeletedAt.ShouldNotBeNull();
        sut.DeletedBy.ShouldBe(deletedBy);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Remove_RaisesVariantRemovedEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();

        sut.Remove();

        var evt = sut.DomainEvents.Single().ShouldBeOfType<VariantRemovedEvent>();
        evt.VariantId.ShouldBe(sut.Id);
        evt.ProductId.ShouldBe(sut.ProductId);
    }

    [Fact]
    public void Remove_WithoutDeletedBy_LeavesDeletedByNull()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.Remove();

        sut.DeletedBy.ShouldBeNull();
        sut.IsDeleted.ShouldBeTrue();
    }
}
