using Domain.Attribute.Entities;
using Domain.Attribute.Exceptions;
using Domain.Attribute.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Attribute.Entities;

public class AttributeValueTests
{
    [Fact]
    public async Task AddValue_WithValidInput_ProducesInitializedAttributeValue()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow, "#FF0000", 2);

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.Value.ShouldBe("red");
        sut.DisplayValue.ShouldBe("Red");
        sut.HexCode.ShouldBe("#FF0000");
        sut.SortOrder.ShouldBe(2);
        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task AddValue_SetsCreatedAtCloseToUtcNow()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task AddValue_LinksChildToParentAttributeType()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        sut.AttributeTypeId.ShouldBe(parent.Id);
        sut.AttributeType.ShouldBeSameAs(parent);
    }

    [Fact]
    public async Task AddValue_TrimsValueDisplayValueAndHexCode()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("  red  ", "  Red  ", DateTime.UtcNow, "  #FF0000  ", 1);

        sut.Value.ShouldBe("red");
        sut.DisplayValue.ShouldBe("Red");
        sut.HexCode.ShouldBe("#FF0000");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddValue_WithNullOrWhitespaceDisplayValue_FallsBackToTrimmedValue(string? displayValue)
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("  red  ", displayValue!, DateTime.UtcNow);

        sut.DisplayValue.ShouldBe("red");
    }

    [Fact]
    public async Task AddValue_WithNullHexCode_LeavesHexCodeNull()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        sut.HexCode.ShouldBeNull();
    }

    [Fact]
    public async Task AddValue_DefaultsIsActiveToTrue()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        sut.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task AddValue_DefaultsSoftDeleteFieldsToInitialState()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        sut.ShouldBeAssignableTo<ISoftDeletable>();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateValue_WithValidInput_AppliesTrimmedFieldsAndSetsUpdatedAt()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow, "#FF0000", 1);
        var updatedAtBefore = sut.UpdatedAt;

        parent.UpdateValue(sut.Id, "  crimson  ", "  Crimson  ", "  #DC143C  ", 5, false, DateTime.UtcNow);

        sut.Value.ShouldBe("crimson");
        sut.DisplayValue.ShouldBe("Crimson");
        sut.HexCode.ShouldBe("#DC143C");
        sut.SortOrder.ShouldBe(5);
        sut.IsActive.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
        sut.UpdatedAt.ShouldNotBe(updatedAtBefore);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateValue_WithNullOrWhitespaceDisplayValue_FallsBackToTrimmedValue(string? displayValue)
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        parent.UpdateValue(sut.Id, "  crimson  ", displayValue!, null, 0, true, DateTime.UtcNow);

        sut.DisplayValue.ShouldBe("crimson");
    }

    [Fact]
    public async Task UpdateValue_WithNullHexCode_ClearsHexCode()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow, "#FF0000", 0);

        parent.UpdateValue(sut.Id, "red", "Red", null, 0, true, DateTime.UtcNow);

        sut.HexCode.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateValue_WithTrimmedHexCode_StoresTrimmedHexCode()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        parent.UpdateValue(sut.Id, "red", "Red", "  #ABCDEF  ", 0, true, DateTime.UtcNow);

        sut.HexCode.ShouldBe("#ABCDEF");
    }

    [Fact]
    public async Task UpdateValue_ChangingIsActiveToFalse_TogglesIsActive()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        parent.UpdateValue(sut.Id, "red", "Red", null, 0, false, DateTime.UtcNow);

        sut.IsActive.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateValue_WithNullOrWhitespaceValue_ThrowsArgumentException(string? value)
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        Should.Throw<ArgumentException>(
            () => parent.UpdateValue(sut.Id, value!, "Red", null, 0, true, DateTime.UtcNow));
    }

    [Fact]
    public async Task UpdateValue_WithNegativeSortOrder_ThrowsArgumentException()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        Should.Throw<ArgumentException>(
            () => parent.UpdateValue(sut.Id, "red", "Red", null, -1, true, DateTime.UtcNow));
    }

    [Fact]
    public async Task UpdateValue_WithUnknownValueId_ThrowsAttributeValueNotFoundException()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        Should.Throw<AttributeValueNotFoundException>(
            () => parent.UpdateValue(AttributeValueId.NewId(), "red", "Red", null, 0, true, DateTime.UtcNow));
    }

    [Fact]
    public async Task UpdateValue_WithNewValueColliding_LeavesTargetValueUnchanged()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();
        var target = parent.AddValue("red", "Red", DateTime.UtcNow);
        parent.AddValue("blue", "Blue", DateTime.UtcNow);

        Should.Throw<DuplicateAttributeException>(
            () => parent.UpdateValue(target.Id, "BLUE", "Blue", null, 0, true, DateTime.UtcNow));

        target.Value.ShouldBe("red");
    }

    [Fact]
    public async Task AttributeValue_ExposesAuditableContract()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        sut.ShouldBeAssignableTo<IAuditable>();
        sut.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task AttributeValue_ExposesActivatableContract()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var sut = parent.AddValue("red", "Red", DateTime.UtcNow);

        sut.ShouldBeAssignableTo<IActivatable>();
    }

    [Fact]
    public async Task AttributeValue_HasUniqueIdentityAcrossInstances()
    {
        var parent = await new AttributeTypeBuilder().BuildAsync();

        var first = parent.AddValue("red", "Red", DateTime.UtcNow);
        var second = parent.AddValue("blue", "Blue", DateTime.UtcNow);

        first.Id.ShouldNotBe(second.Id);
    }
}
