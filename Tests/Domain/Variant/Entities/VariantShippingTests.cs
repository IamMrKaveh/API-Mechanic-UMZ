using Domain.Shipping.ValueObjects;
using Domain.Variant.Events;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Variant.Entities;

public class VariantShippingTests
{
    private static ShippingAssignment BuildAssignment(
        ShippingId? shippingId = null,
        decimal weight = 1m,
        decimal width = 10m,
        decimal height = 10m,
        decimal length = 10m)
    {
        return new ShippingAssignment(
            shippingId ?? ShippingId.NewId(),
            weight,
            width,
            height,
            length);
    }

    // ---------- Creation invariants (via ProductVariant.SetShippingMethods) ----------

    [Fact]
    public void SetShippingMethods_WithSingleAssignment_CreatesVariantShippingWithExpectedState()
    {
        var variant = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();
        var assignment = BuildAssignment(shippingId, weight: 2.5m, width: 15m, height: 20m, length: 30m);

        variant.SetShippingMethods(shippingMultiplier: 1.2m, new[] { assignment });

        variant.Shippings.Count.ShouldBe(1);
        var shipping = variant.Shippings.Single();
        shipping.Id.ShouldNotBeNull();
        shipping.VariantId.ShouldBe(variant.Id);
        shipping.ShippingId.ShouldBe(shippingId);
        shipping.Weight.ShouldBe(2.5m);
        shipping.Width.ShouldBe(15m);
        shipping.Height.ShouldBe(20m);
        shipping.Length.ShouldBe(30m);
        shipping.ShippingMultiplier.ShouldBe(1.2m);
    }

    [Fact]
    public void SetShippingMethods_WithZeroDimensions_AllowsZeroAsNonNegativeBoundary()
    {
        var variant = new ProductVariantBuilder().Build();
        var assignment = BuildAssignment(weight: 0m, width: 0m, height: 0m, length: 0m);

        variant.SetShippingMethods(shippingMultiplier: 1m, new[] { assignment });

        var shipping = variant.Shippings.Single();
        shipping.Weight.ShouldBe(0m);
        shipping.Width.ShouldBe(0m);
        shipping.Height.ShouldBe(0m);
        shipping.Length.ShouldBe(0m);
    }

    [Fact]
    public void SetShippingMethods_WithMultipleDistinctShippingIds_AddsAll()
    {
        var variant = new ProductVariantBuilder().Build();
        var a1 = BuildAssignment();
        var a2 = BuildAssignment();
        var a3 = BuildAssignment();

        variant.SetShippingMethods(shippingMultiplier: 1m, new[] { a1, a2, a3 });

        variant.Shippings.Count.ShouldBe(3);
    }

    [Fact]
    public void SetShippingMethods_WithDuplicateShippingIds_DeduplicatesKeepingFirst()
    {
        var variant = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();
        var first = BuildAssignment(shippingId, weight: 1m);
        var second = BuildAssignment(shippingId, weight: 2m);

        variant.SetShippingMethods(shippingMultiplier: 1m, new[] { first, second });

        variant.Shippings.Count.ShouldBe(1);
        variant.Shippings.Single().Weight.ShouldBe(1m);
    }

    [Fact]
    public void SetShippingMethods_WithEmptyAssignments_ClearsExistingShippings()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.SetShippingMethods(1m, new[] { BuildAssignment(), BuildAssignment() });

        variant.SetShippingMethods(1m, Array.Empty<ShippingAssignment>());

        variant.Shippings.ShouldBeEmpty();
    }

    [Fact]
    public void SetShippingMethods_WithNullAssignments_ClearsExistingShippings()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.SetShippingMethods(1m, new[] { BuildAssignment() });

        variant.SetShippingMethods(1m, null!);

        variant.Shippings.ShouldBeEmpty();
    }

    // ---------- Update flow ----------

    [Fact]
    public void SetShippingMethods_UpdatingExistingShippingId_UpdatesDimensionsAndMultiplierInPlace()
    {
        var variant = new ProductVariantBuilder().Build();
        var shippingId = ShippingId.NewId();
        variant.SetShippingMethods(1m, new[]
        {
            BuildAssignment(shippingId, weight: 1m, width: 2m, height: 3m, length: 4m)
        });
        var originalId = variant.Shippings.Single().Id;

        variant.SetShippingMethods(2.5m, new[]
        {
            BuildAssignment(shippingId, weight: 5m, width: 6m, height: 7m, length: 8m)
        });

        variant.Shippings.Count.ShouldBe(1);
        var shipping = variant.Shippings.Single();
        shipping.Id.ShouldBe(originalId);
        shipping.Weight.ShouldBe(5m);
        shipping.Width.ShouldBe(6m);
        shipping.Height.ShouldBe(7m);
        shipping.Length.ShouldBe(8m);
        shipping.ShippingMultiplier.ShouldBe(2.5m);
    }

    [Fact]
    public void SetShippingMethods_ReplacingWithDifferentShippingId_RemovesOldAddsNew()
    {
        var variant = new ProductVariantBuilder().Build();
        var oldId = ShippingId.NewId();
        var newId = ShippingId.NewId();
        variant.SetShippingMethods(1m, new[] { BuildAssignment(oldId) });

        variant.SetShippingMethods(1m, new[] { BuildAssignment(newId) });

        variant.Shippings.Count.ShouldBe(1);
        variant.Shippings.Single().ShippingId.ShouldBe(newId);
    }

    [Fact]
    public void SetShippingMethods_MixedAddUpdateRemove_ResultsInDesiredStateOnly()
    {
        var variant = new ProductVariantBuilder().Build();
        var keepId = ShippingId.NewId();
        var removeId = ShippingId.NewId();
        variant.SetShippingMethods(1m, new[]
        {
            BuildAssignment(keepId, weight: 1m),
            BuildAssignment(removeId, weight: 2m)
        });

        var newId = ShippingId.NewId();
        variant.SetShippingMethods(3m, new[]
        {
            BuildAssignment(keepId, weight: 9m, width: 9m, height: 9m, length: 9m),
            BuildAssignment(newId, weight: 4m)
        });

        variant.Shippings.Count.ShouldBe(2);
        var kept = variant.Shippings.Single(s => s.ShippingId == keepId);
        kept.Weight.ShouldBe(9m);
        kept.ShippingMultiplier.ShouldBe(3m);
        variant.Shippings.ShouldContain(s => s.ShippingId == newId && s.Weight == 4m);
        variant.Shippings.ShouldNotContain(s => s.ShippingId == removeId);
    }

    // ---------- Invariant violations ----------

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void SetShippingMethods_WithNegativeWeight_ThrowsDomainException(decimal weight)
    {
        var variant = new ProductVariantBuilder().Build();
        var assignment = BuildAssignment(weight: weight);

        Should.Throw<DomainException>(() =>
            variant.SetShippingMethods(1m, new[] { assignment }));
    }

    [Fact]
    public void SetShippingMethods_WithNegativeWidth_ThrowsDomainException()
    {
        var variant = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(() =>
            variant.SetShippingMethods(1m, new[] { BuildAssignment(width: -1m) }));
    }

    [Fact]
    public void SetShippingMethods_WithNegativeHeight_ThrowsDomainException()
    {
        var variant = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(() =>
            variant.SetShippingMethods(1m, new[] { BuildAssignment(height: -1m) }));
    }

    [Fact]
    public void SetShippingMethods_WithNegativeLength_ThrowsDomainException()
    {
        var variant = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(() =>
            variant.SetShippingMethods(1m, new[] { BuildAssignment(length: -1m) }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void SetShippingMethods_WithNonPositiveMultiplier_ThrowsDomainException(decimal multiplier)
    {
        var variant = new ProductVariantBuilder().Build();

        Should.Throw<DomainException>(() =>
            variant.SetShippingMethods(multiplier, new[] { BuildAssignment() }));
    }

    // ---------- Interaction with aggregate ----------

    [Fact]
    public void SetShippingMethods_OnActiveVariant_RaisesVariantShippingSetEventAndTouchesUpdatedAt()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.ClearDomainEvents();

        variant.SetShippingMethods(1m, new[] { BuildAssignment() });

        variant.UpdatedAt.ShouldNotBeNull();
        variant.DomainEvents.OfType<VariantShippingSetEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void SetShippingMethods_OnRemovedVariant_ThrowsDomainException()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.Remove();

        Should.Throw<DomainException>(() =>
            variant.SetShippingMethods(1m, new[] { BuildAssignment() }));
    }
}
