using Domain.Attribute.ValueObjects;
using Domain.Variant.Events;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Variant.Entities;

public class VariantAttributeTests
{
    private static AttributeAssignment BuildAssignment(
        AttributeTypeId? attributeId = null,
        AttributeValueId? valueId = null,
        string displayValue = "Red")
    {
        return AttributeAssignment.Create(
            attributeId ?? AttributeTypeId.NewId(),
            valueId ?? AttributeValueId.NewId(),
            displayValue);
    }

    // ---------- Creation invariants (via ProductVariant.SetAttributes) ----------

    [Fact]
    public void SetAttributes_WithSingleAssignment_CreatesVariantAttributeWithExpectedState()
    {
        var variant = new ProductVariantBuilder().Build();
        var assignment = BuildAssignment(displayValue: "Red");

        variant.SetAttributes(new[] { assignment });

        variant.Attributes.Count.ShouldBe(1);
        var attribute = variant.Attributes.Single();
        attribute.Id.ShouldNotBeNull();
        attribute.VariantId.ShouldBe(variant.Id);
        attribute.AttributeTypeId.ShouldBe(assignment.AttributeId);
        attribute.ValueId.ShouldBe(assignment.ValueId);
        attribute.DisplayValue.ShouldBe("Red");
    }

    [Fact]
    public void SetAttributes_WithSurroundingWhitespaceInDisplayValue_TrimsDisplayValue()
    {
        var variant = new ProductVariantBuilder().Build();
        var assignment = BuildAssignment(displayValue: "   Blue   ");

        variant.SetAttributes(new[] { assignment });

        variant.Attributes.Single().DisplayValue.ShouldBe("Blue");
    }

    [Fact]
    public void SetAttributes_WithMultipleAssignmentsHavingDistinctValueIds_AddsAllAttributes()
    {
        var variant = new ProductVariantBuilder().Build();
        var a1 = BuildAssignment(displayValue: "Small");
        var a2 = BuildAssignment(displayValue: "Medium");
        var a3 = BuildAssignment(displayValue: "Large");

        variant.SetAttributes(new[] { a1, a2, a3 });

        variant.Attributes.Count.ShouldBe(3);
        variant.Attributes.Select(x => x.DisplayValue)
            .ShouldBe(new[] { "Small", "Medium", "Large" }, ignoreOrder: true);
    }

    [Fact]
    public void SetAttributes_WithDuplicateValueIds_DeduplicatesKeepingFirstOccurrence()
    {
        var variant = new ProductVariantBuilder().Build();
        var duplicateValueId = AttributeValueId.NewId();
        var first = AttributeAssignment.Create(AttributeTypeId.NewId(), duplicateValueId, "First");
        var second = AttributeAssignment.Create(AttributeTypeId.NewId(), duplicateValueId, "Second");

        variant.SetAttributes(new[] { first, second });

        variant.Attributes.Count.ShouldBe(1);
        variant.Attributes.Single().DisplayValue.ShouldBe("First");
    }

    [Fact]
    public void SetAttributes_WithEmptyEnumerable_ClearsExistingAttributes()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.SetAttributes(new[] { BuildAssignment(), BuildAssignment() });
        variant.Attributes.Count.ShouldBe(2);

        variant.SetAttributes(Array.Empty<AttributeAssignment>());

        variant.Attributes.ShouldBeEmpty();
    }

    [Fact]
    public void SetAttributes_WithNullEnumerable_ClearsExistingAttributes()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.SetAttributes(new[] { BuildAssignment() });

        variant.SetAttributes(null!);

        variant.Attributes.ShouldBeEmpty();
    }

    // ---------- Mutation / Update flow ----------

    [Fact]
    public void SetAttributes_UpdatingExistingValueId_UpdatesDisplayValueAndAttributeTypeIdInPlace()
    {
        var variant = new ProductVariantBuilder().Build();
        var valueId = AttributeValueId.NewId();
        var initial = AttributeAssignment.Create(AttributeTypeId.NewId(), valueId, "Old");
        variant.SetAttributes(new[] { initial });
        var originalId = variant.Attributes.Single().Id;

        var newAttributeTypeId = AttributeTypeId.NewId();
        var update = AttributeAssignment.Create(newAttributeTypeId, valueId, "New");
        variant.SetAttributes(new[] { update });

        variant.Attributes.Count.ShouldBe(1);
        var attribute = variant.Attributes.Single();
        attribute.Id.ShouldBe(originalId);
        attribute.AttributeTypeId.ShouldBe(newAttributeTypeId);
        attribute.DisplayValue.ShouldBe("New");
    }

    [Fact]
    public void SetAttributes_UpdatingWithWhitespaceDisplayValue_TrimsBeforeStoring()
    {
        var variant = new ProductVariantBuilder().Build();
        var valueId = AttributeValueId.NewId();
        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), valueId, "Old")
        });

        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), valueId, "   Trimmed   ")
        });

        variant.Attributes.Single().DisplayValue.ShouldBe("Trimmed");
    }

    [Fact]
    public void SetAttributes_ReplacingExistingWithDifferentValueIds_RemovesOldAndAddsNew()
    {
        var variant = new ProductVariantBuilder().Build();
        var oldValueId = AttributeValueId.NewId();
        var newValueId = AttributeValueId.NewId();
        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), oldValueId, "Old")
        });

        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), newValueId, "New")
        });

        variant.Attributes.Count.ShouldBe(1);
        variant.Attributes.Single().ValueId.ShouldBe(newValueId);
        variant.Attributes.Single().DisplayValue.ShouldBe("New");
    }

    [Fact]
    public void SetAttributes_MixedAddUpdateRemove_ResultsInDesiredStateOnly()
    {
        var variant = new ProductVariantBuilder().Build();
        var keepValueId = AttributeValueId.NewId();
        var removeValueId = AttributeValueId.NewId();
        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), keepValueId, "Keep"),
            AttributeAssignment.Create(AttributeTypeId.NewId(), removeValueId, "Remove")
        });

        var newValueId = AttributeValueId.NewId();
        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(AttributeTypeId.NewId(), keepValueId, "Kept-Updated"),
            AttributeAssignment.Create(AttributeTypeId.NewId(), newValueId, "Added")
        });

        variant.Attributes.Count.ShouldBe(2);
        variant.Attributes.ShouldContain(a => a.ValueId == keepValueId && a.DisplayValue == "Kept-Updated");
        variant.Attributes.ShouldContain(a => a.ValueId == newValueId && a.DisplayValue == "Added");
        variant.Attributes.ShouldNotContain(a => a.ValueId == removeValueId);
    }

    // ---------- Invariant violations ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetAttributes_WithBlankDisplayValue_ThrowsArgumentException(string displayValue)
    {
        var variant = new ProductVariantBuilder().Build();
        var assignment = new AttributeAssignment(
            AttributeTypeId.NewId(),
            AttributeValueId.NewId(),
            displayValue);

        Should.Throw<ArgumentException>(() => variant.SetAttributes(new[] { assignment }));
    }

    [Fact]
    public void SetAttributes_WithNullDisplayValue_ThrowsArgumentException()
    {
        var variant = new ProductVariantBuilder().Build();
        var assignment = new AttributeAssignment(
            AttributeTypeId.NewId(),
            AttributeValueId.NewId(),
            null!);

        Should.Throw<ArgumentException>(() => variant.SetAttributes(new[] { assignment }));
    }

    // ---------- Interaction with the aggregate ----------

    [Fact]
    public void SetAttributes_OnActiveVariant_RaisesVariantAttributeSetEventAndTouchesUpdatedAt()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.ClearDomainEvents();

        variant.SetAttributes(new[] { BuildAssignment() });

        variant.UpdatedAt.ShouldNotBeNull();
        variant.DomainEvents.OfType<VariantAttributeSetEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void SetAttributes_OnRemovedVariant_ThrowsInvalidVariantOperationException()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.Remove();

        Should.Throw<DomainException>(() => variant.SetAttributes(new[] { BuildAssignment() }));
    }
}
