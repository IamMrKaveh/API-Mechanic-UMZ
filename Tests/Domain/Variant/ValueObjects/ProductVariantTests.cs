using Domain.Attribute.ValueObjects;
using Domain.Product.Exceptions;
using Domain.Product.ValueObjects;
using Domain.Shipping.ValueObjects;
using Domain.Variant.Events;
using Domain.Variant.Exceptions;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Variant.ValueObjects;

public class ProductVariantTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedVariant()
    {
        var id = VariantId.NewId();
        var productId = ProductId.NewId();
        var sku = Sku.Create("ABC-123");
        var selling = Money.Create(100_000m, "IRT");
        var original = Money.Create(120_000m, "IRT");

        var sut = new ProductVariantBuilder()
            .WithId(id)
            .WithProductId(productId)
            .WithSku(sku)
            .WithSellingPrice(selling)
            .WithOriginalPrice(original)
            .Build();

        sut.Id.ShouldBe(id);
        sut.ProductId.ShouldBe(productId);
        sut.Sku.ShouldBe(sku);
        sut.SellingPrice.Amount.ShouldBe(100_000m);
        sut.SellingPrice.Currency.ShouldBe("IRT");
        sut.OriginalPrice.Amount.ShouldBe(120_000m);
        sut.OriginalPrice.Currency.ShouldBe("IRT");
        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldBeNull();
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
    public void Create_LeavesSoftDeleteFieldsAtDefaults()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.ShouldBeAssignableTo<ISoftDeletable>();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Create_ProducesVariantWithVersionOne()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneVariantCreatedEvent()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<VariantCreatedEvent>();
    }

    [Fact]
    public void Create_WithNullOriginalPrice_UsesSellingPriceAsOriginal()
    {
        var selling = Money.Create(80_000m, "IRT");

        var sut = new ProductVariantBuilder()
            .WithSellingPrice(selling)
            .WithOriginalPrice(null)
            .Build();

        sut.OriginalPrice.Amount.ShouldBe(selling.Amount);
        sut.OriginalPrice.Currency.ShouldBe(selling.Currency);
    }

    [Fact]
    public void Create_WithZeroOriginalPrice_FallsBackToSellingPrice()
    {
        var selling = Money.Create(80_000m, "IRT");

        var sut = new ProductVariantBuilder()
            .WithSellingPrice(selling)
            .WithOriginalPrice(Money.Create(0m, "IRT"))
            .Build();

        sut.OriginalPrice.Amount.ShouldBe(selling.Amount);
    }

    [Fact]
    public void Create_WithSellingPriceAmountZero_ThrowsInvalidPriceException()
    {
        Should.Throw<InvalidPriceException>(() =>
            new ProductVariantBuilder()
                .WithSellingPrice(Money.Create(0m, "IRT"))
                .Build());
    }

    [Fact]
    public void Create_WithOriginalPriceBelowSellingPrice_ThrowsInvalidPriceException()
    {
        Should.Throw<InvalidPriceException>(() =>
            new ProductVariantBuilder()
                .WithSellingPrice(100_000m, "IRT")
                .WithOriginalPrice(50_000m, "IRT")
                .Build());
    }

    [Fact]
    public void Create_WithOriginalPriceEqualToSellingPrice_Succeeds()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(80_000m, "IRT")
            .WithOriginalPrice(80_000m, "IRT")
            .Build();

        sut.SellingPrice.Amount.ShouldBe(sut.OriginalPrice.Amount);
    }

    [Fact]
    public void Create_WithNullId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ProductVariantBuilder().WithId(null!).Build());
    }

    [Fact]
    public void Create_WithNullProductId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ProductVariantBuilder().WithProductId(null!).Build());
    }

    [Fact]
    public void Create_WithNullSku_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ProductVariantBuilder().WithSku(null!).Build());
    }

    [Fact]
    public void Create_WithNullSellingPrice_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new ProductVariantBuilder().WithSellingPrice(null!).Build());
    }

    [Fact]
    public void ChangePrice_WithDifferentSellingPrice_AppliesAndRaisesEventAndBumpsVersion()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(120_000m, "IRT")
            .Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangePrice(Money.Create(90_000m, "IRT"), Money.Create(120_000m, "IRT"));

        sut.SellingPrice.Amount.ShouldBe(90_000m);
        sut.UpdatedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductVariantPriceChangedEvent>();
    }

    [Fact]
    public void ChangePrice_WithSameSellingAndOriginal_DoesNotRaiseEventAndDoesNotBumpVersion()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(120_000m, "IRT")
            .Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangePrice(Money.Create(100_000m, "IRT"), Money.Create(120_000m, "IRT"));

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangePrice_WithSameSellingAndOriginal_StillBumpsUpdatedAt()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(120_000m, "IRT")
            .Build();

        sut.ChangePrice(Money.Create(100_000m, "IRT"), Money.Create(120_000m, "IRT"));

        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangePrice_WithSameSellingButDifferentOriginal_RaisesEvent()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(120_000m, "IRT")
            .Build();
        sut.ClearDomainEvents();

        sut.ChangePrice(Money.Create(100_000m, "IRT"), Money.Create(150_000m, "IRT"));

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductVariantPriceChangedEvent>();
    }

    [Fact]
    public void ChangePrice_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.ChangePrice(Money.Create(50_000m, "IRT")));
    }

    [Fact]
    public void ChangePrice_WithNullSellingPrice_ThrowsArgumentException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.ChangePrice(null!));
    }

    [Fact]
    public void ChangePrice_WithSellingPriceAmountZero_ThrowsInvalidPriceException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<InvalidPriceException>(
            () => sut.ChangePrice(Money.Create(0m, "IRT")));
    }

    [Fact]
    public void ChangePrice_WithOriginalPriceBelowSelling_ThrowsInvalidPriceException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<InvalidPriceException>(
            () => sut.ChangePrice(Money.Create(100_000m, "IRT"), Money.Create(50_000m, "IRT")));
    }

    [Fact]
    public void ChangeSku_WithDifferentSku_AppliesAndBumpsUpdatedAt()
    {
        var sut = new ProductVariantBuilder().WithSku("OLD-SKU").Build();
        var newSku = Sku.Create("NEW-SKU");

        sut.ChangeSku(newSku);

        sut.Sku.ShouldBe(newSku);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangeSku_WithDifferentSku_DoesNotRaiseEventAndDoesNotBumpVersion()
    {
        var sut = new ProductVariantBuilder().WithSku("OLD-SKU").Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangeSku(Sku.Create("NEW-SKU"));

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeSku_WithEqualSku_IsShortCircuitedNoOp()
    {
        var sut = new ProductVariantBuilder().WithSku("ABC").Build();
        var updatedAtBefore = sut.UpdatedAt;

        sut.ChangeSku(Sku.Create("abc"));

        sut.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    [Fact]
    public void ChangeSku_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(() => sut.ChangeSku(Sku.Create("NEW")));
    }

    [Fact]
    public void ChangeSku_WithNull_ThrowsArgumentException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<ArgumentException>(() => sut.ChangeSku(null!));
    }

    [Fact]
    public void SetAttributes_WithNewAssignments_AddsThemToTheAggregate()
    {
        var sut = new ProductVariantBuilder().Build();
        var a1 = AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red");
        var a2 = AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Small");

        sut.SetAttributes(new[] { a1, a2 });

        sut.Attributes.Count.ShouldBe(2);
        sut.Attributes.ShouldContain(x => x.ValueId == a1.ValueId && x.DisplayValue == "Red");
        sut.Attributes.ShouldContain(x => x.ValueId == a2.ValueId && x.DisplayValue == "Small");
    }

    [Fact]
    public void SetAttributes_ChildEntityIsLinkedToParentByVariantId()
    {
        var sut = new ProductVariantBuilder().Build();
        var a = AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red");

        sut.SetAttributes(new[] { a });

        sut.Attributes.Single().VariantId.ShouldBe(sut.Id);
    }

    [Fact]
    public void SetAttributes_WithNullEnumerable_ClearsAllAttributesAndRaisesEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red")
        });
        sut.ClearDomainEvents();

        sut.SetAttributes(null!);

        sut.Attributes.ShouldBeEmpty();
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<VariantAttributeSetEvent>();
    }

    [Fact]
    public void SetAttributes_WithDuplicateValueIds_DeduplicatesByFirstOccurrence()
    {
        var sut = new ProductVariantBuilder().Build();
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();
        var first = AttributeAssignment.Create(typeId, valueId, "Red");
        var second = AttributeAssignment.Create(typeId, valueId, "Crimson");

        sut.SetAttributes(new[] { first, second });

        sut.Attributes.Count.ShouldBe(1);
        sut.Attributes.Single().DisplayValue.ShouldBe("Red");
    }

    [Fact]
    public void SetAttributes_WhenExistingIsAbsentFromDesired_RemovesOrphans()
    {
        var sut = new ProductVariantBuilder().Build();
        var kept = AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red");
        var orphan = AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Small");
        sut.SetAttributes(new[] { kept, orphan });

        sut.SetAttributes(new[] { kept });

        sut.Attributes.Count.ShouldBe(1);
        sut.Attributes.Single().ValueId.ShouldBe(kept.ValueId);
    }

    [Fact]
    public void SetAttributes_WithSameValueIdButDifferentDisplay_UpdatesDisplayInPlace()
    {
        var sut = new ProductVariantBuilder().Build();
        var typeId = AttributeTypeId.NewId();
        var valueId = AttributeValueId.NewId();
        sut.SetAttributes(new[] { AttributeAssignment.Create(typeId, valueId, "Red") });
        var firstEntityId = sut.Attributes.Single().Id;

        sut.SetAttributes(new[] { AttributeAssignment.Create(typeId, valueId, "Rouge") });

        sut.Attributes.Count.ShouldBe(1);
        sut.Attributes.Single().Id.ShouldBe(firstEntityId);
        sut.Attributes.Single().DisplayValue.ShouldBe("Rouge");
    }

    [Fact]
    public void SetAttributes_AlwaysRaisesVariantAttributeSetEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();

        sut.SetAttributes(Array.Empty<AttributeAssignment>());

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<VariantAttributeSetEvent>();
    }

    [Fact]
    public void SetAttributes_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.SetAttributes(Array.Empty<AttributeAssignment>()));
    }

    [Fact]
    public void SetShippingMethods_WithValidInput_AddsShippingEntriesLinkedToVariant()
    {
        var sut = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();
        var assignment = new ShippingAssignment(shippingId, 1.5m, 20m, 10m, 30m);

        sut.SetShippingMethods(1.2m, new[] { assignment });

        sut.Shippings.Count.ShouldBe(1);
        var stored = sut.Shippings.Single();
        stored.ShippingId.ShouldBe(shippingId);
        stored.Weight.ShouldBe(1.5m);
        stored.Width.ShouldBe(20m);
        stored.Height.ShouldBe(10m);
        stored.Length.ShouldBe(30m);
        stored.ShippingMultiplier.ShouldBe(1.2m);
        stored.VariantId.ShouldBe(sut.Id);
    }

    [Fact]
    public void SetShippingMethods_WithZeroMultiplier_ThrowsDomainException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(
            () => sut.SetShippingMethods(0m, Array.Empty<ShippingAssignment>()));
    }

    [Fact]
    public void SetShippingMethods_WithNegativeMultiplier_ThrowsDomainException()
    {
        var sut = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(
            () => sut.SetShippingMethods(-1m, Array.Empty<ShippingAssignment>()));
    }

    [Fact]
    public void SetShippingMethods_WithNegativeDimension_ThrowsDomainException()
    {
        var sut = new ProductVariantBuilder().Build();
        var bad = new ShippingAssignment(ShippingId.NewId(), -1m, 1m, 1m, 1m);

        Should.Throw<DomainException>(
            () => sut.SetShippingMethods(1m, new[] { bad }));
    }

    [Fact]
    public void SetShippingMethods_WithDuplicateShippingIds_DeduplicatesByFirstOccurrence()
    {
        var sut = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();
        var first = new ShippingAssignment(shippingId, 1m, 1m, 1m, 1m);
        var second = new ShippingAssignment(shippingId, 5m, 5m, 5m, 5m);

        sut.SetShippingMethods(1m, new[] { first, second });

        sut.Shippings.Count.ShouldBe(1);
        sut.Shippings.Single().Weight.ShouldBe(1m);
    }

    [Fact]
    public void SetShippingMethods_WhenExistingIsAbsentFromDesired_RemovesOrphans()
    {
        var sut = new ProductVariantBuilder().Build();
        var keptId = ShippingId.NewId();
        var orphanId = ShippingId.NewId();
        sut.SetShippingMethods(1m, new[]
        {
            new ShippingAssignment(keptId, 1m, 1m, 1m, 1m),
            new ShippingAssignment(orphanId, 2m, 2m, 2m, 2m)
        });

        sut.SetShippingMethods(1m, new[] { new ShippingAssignment(keptId, 1m, 1m, 1m, 1m) });

        sut.Shippings.Count.ShouldBe(1);
        sut.Shippings.Single().ShippingId.ShouldBe(keptId);
    }

    [Fact]
    public void SetShippingMethods_WithSameIdButDifferentDimensions_UpdatesInPlace()
    {
        var sut = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();
        sut.SetShippingMethods(1m, new[] { new ShippingAssignment(shippingId, 1m, 1m, 1m, 1m) });
        var firstEntityId = sut.Shippings.Single().Id;

        sut.SetShippingMethods(2m, new[] { new ShippingAssignment(shippingId, 5m, 5m, 5m, 5m) });

        sut.Shippings.Count.ShouldBe(1);
        sut.Shippings.Single().Id.ShouldBe(firstEntityId);
        sut.Shippings.Single().Weight.ShouldBe(5m);
        sut.Shippings.Single().ShippingMultiplier.ShouldBe(2m);
    }

    [Fact]
    public void SetShippingMethods_AlwaysRaisesVariantShippingSetEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();

        sut.SetShippingMethods(1m, Array.Empty<ShippingAssignment>());

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<VariantShippingSetEvent>();
    }

    [Fact]
    public void SetShippingMethods_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.Remove();

        Should.Throw<InvalidVariantOperationException>(
            () => sut.SetShippingMethods(1m, Array.Empty<ShippingAssignment>()));
    }

    [Fact]
    public void Remove_SetsSoftDeleteFieldsAndDeactivates()
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
    public void Remove_WithNullDeletedBy_LeavesDeletedByNull()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.Remove(null);

        sut.DeletedBy.ShouldBeNull();
        sut.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Remove_RaisesVariantRemovedEvent()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();

        sut.Remove(Guid.NewGuid());

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<VariantRemovedEvent>();
    }

    [Fact]
    public void Remove_CalledTwice_IsNotIdempotentAndRaisesTwoEvents()
    {
        var sut = new ProductVariantBuilder().Build();
        sut.ClearDomainEvents();

        sut.Remove(Guid.NewGuid());
        sut.Remove(Guid.NewGuid());

        sut.DomainEvents.Count.ShouldBe(2);
        sut.DomainEvents.ShouldAllBe(e => e is VariantRemovedEvent);
    }

    [Fact]
    public void IsDiscounted_WhenOriginalEqualsSelling_ReturnsFalse()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(100_000m, "IRT")
            .Build();

        sut.IsDiscounted.ShouldBeFalse();
        sut.DiscountPercentage.ShouldBeNull();
    }

    [Fact]
    public void IsDiscounted_WhenOriginalGreaterThanSelling_ReturnsTrue()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(80_000m, "IRT")
            .WithOriginalPrice(100_000m, "IRT")
            .Build();

        sut.IsDiscounted.ShouldBeTrue();
    }

    [Fact]
    public void DiscountPercentage_WhenDiscounted_ReturnsRoundedTwoDecimalValue()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(80_000m, "IRT")
            .WithOriginalPrice(100_000m, "IRT")
            .Build();

        sut.DiscountPercentage.ShouldBe(20.00m);
    }

    [Fact]
    public void DiscountPercentage_WhenPricesEqual_ReturnsNull()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(50_000m, "IRT")
            .WithOriginalPrice(50_000m, "IRT")
            .Build();

        sut.DiscountPercentage.ShouldBeNull();
    }

    [Fact]
    public void Equality_TwoVariantsWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new ProductVariantBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoVariantsWithDifferentIds_AreConsideredUnequal()
    {
        var a = new ProductVariantBuilder().Build();
        var b = new ProductVariantBuilder().Build();

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void LifecycleSequence_CreateChangePriceSetAttributesRemove_AccumulatesEventsInOrder()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(120_000m, "IRT")
            .Build();

        sut.ChangePrice(Money.Create(90_000m, "IRT"), Money.Create(120_000m, "IRT"));
        sut.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), AttributeValueId.NewId(), "Red")
        });
        sut.Remove(Guid.NewGuid());

        sut.DomainEvents.Count.ShouldBe(4);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<VariantCreatedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<ProductVariantPriceChangedEvent>();
        sut.DomainEvents.ElementAt(2).ShouldBeOfType<VariantAttributeSetEvent>();
        sut.DomainEvents.ElementAt(3).ShouldBeOfType<VariantRemovedEvent>();
    }

    [Fact]
    public void LifecycleSequence_VersionGrowsByOnePerRaisedEvent()
    {
        var sut = new ProductVariantBuilder()
            .WithSellingPrice(100_000m, "IRT")
            .WithOriginalPrice(120_000m, "IRT")
            .Build();

        sut.Version.ShouldBe(1);
        sut.ChangePrice(Money.Create(90_000m, "IRT"), Money.Create(120_000m, "IRT"));
        sut.Version.ShouldBe(2);
        sut.SetAttributes(Array.Empty<AttributeAssignment>());
        sut.Version.ShouldBe(3);
        sut.SetShippingMethods(1m, Array.Empty<ShippingAssignment>());
        sut.Version.ShouldBe(4);
        sut.Remove();
        sut.Version.ShouldBe(5);
    }
}
