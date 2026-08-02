using Domain.Attribute.Events;
using Domain.Attribute.Exceptions;
using Domain.Attribute.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Stubs;

namespace Tests.Domain.Attribute.Aggregates;

public class AttributeTypeTests
{
    [Fact]
    public async Task Create_WithValidInput_ReturnsInitializedAttributeType()
    {
        var sut = await new AttributeTypeBuilder()
            .WithName("color")
            .WithDisplayName("Color")
            .WithSortOrder(3)
            .WithIsActive(true)
            .BuildAsync();

        sut.ShouldNotBeNull();
        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.Name.ShouldBe("color");
        sut.DisplayName.ShouldBe("Color");
        sut.SortOrder.ShouldBe(3);
        sut.IsActive.ShouldBeTrue();
        sut.UpdatedAt.ShouldBeNull();
        sut.Values.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = await new AttributeTypeBuilder().BuildAsync();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task Create_TrimsNameAndDisplayName()
    {
        var sut = await new AttributeTypeBuilder()
            .WithName("  color  ")
            .WithDisplayName("  Color  ")
            .BuildAsync();

        sut.Name.ShouldBe("color");
        sut.DisplayName.ShouldBe("Color");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithEmptyOrWhitespaceDisplayName_FallsBackToTrimmedName(string? displayName)
    {
        var sut = await new AttributeTypeBuilder()
            .WithName("  color  ")
            .WithDisplayName(displayName!)
            .BuildAsync();

        sut.DisplayName.ShouldBe("color");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithNullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        await Should.ThrowAsync<ArgumentException>(
            () => new AttributeTypeBuilder().WithName(name!).BuildAsync());
    }

    [Fact]
    public async Task Create_WithNullUniquenessChecker_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(
            () => new AttributeTypeBuilder().WithUniquenessChecker(null!).BuildAsync());
    }

    [Fact]
    public async Task Create_WithNegativeSortOrder_ThrowsArgumentException()
    {
        await Should.ThrowAsync<ArgumentException>(
            () => new AttributeTypeBuilder().WithSortOrder(-1).BuildAsync());
    }

    [Fact]
    public async Task Create_InvokesUniquenessCheckerOnceWithTrimmedNameAndNoExcludeId()
    {
        var checker = new StubAttributeTypeUniquenessChecker();

        _ = await new AttributeTypeBuilder()
            .WithName("  color  ")
            .WithUniquenessChecker(checker)
            .BuildAsync();

        checker.CallCount.ShouldBe(1);
        checker.LastName.ShouldBe("color");
        checker.LastExcludeId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_WhenCheckerReportsNotUnique_ThrowsDuplicateAttributeException()
    {
        var checker = new StubAttributeTypeUniquenessChecker().WithIsUnique(false);

        await Should.ThrowAsync<DuplicateAttributeException>(
            () => new AttributeTypeBuilder().WithUniquenessChecker(checker).BuildAsync());
    }

    [Fact]
    public async Task Create_ProducesAttributeTypeWithVersionOne()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        sut.Version.ShouldBe(1);
    }

    [Fact]
    public async Task Create_RaisesExactlyOneAttributeTypeCreatedEvent()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<AttributeTypeCreatedEvent>();
    }

    [Fact]
    public async Task Create_LeavesSoftDeleteFieldsAtDefaults()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        sut.ShouldBeAssignableTo<ISoftDeletable>();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task Update_WithChangedName_AppliesTrimmedFieldsAndSetsUpdatedAt()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithName("color").WithUniquenessChecker(checker).BuildAsync();

        await sut.Update("  size  ", "  Size  ", 9, false, checker);

        sut.Name.ShouldBe("size");
        sut.DisplayName.ShouldBe("Size");
        sut.SortOrder.ShouldBe(9);
        sut.IsActive.ShouldBeFalse();
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Update_WithChangedName_InvokesUniquenessCheckerWithCurrentIdAsExcludeId()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithName("color").WithUniquenessChecker(checker).BuildAsync();
        var callsAfterCreate = checker.CallCount;

        await sut.Update("size", "Size", 0, true, checker);

        checker.CallCount.ShouldBe(callsAfterCreate + 1);
        checker.LastName.ShouldBe("size");
        checker.LastExcludeId.ShouldBe(sut.Id);
    }

    [Fact]
    public async Task Update_WhenNameUnchangedCaseInsensitive_DoesNotInvokeUniquenessChecker()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithName("Color").WithUniquenessChecker(checker).BuildAsync();
        var callsAfterCreate = checker.CallCount;

        await sut.Update("COLOR", "Color", 5, true, checker);

        checker.CallCount.ShouldBe(callsAfterCreate);
        sut.Name.ShouldBe("COLOR");
    }

    [Fact]
    public async Task Update_WhenNameChangedAndNotUnique_ThrowsDuplicateAttributeException()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithName("color").WithUniquenessChecker(checker).BuildAsync();
        checker.WithIsUnique(false);

        await Should.ThrowAsync<DuplicateAttributeException>(
            () => sut.Update("size", "Size", 0, true, checker));
    }

    [Fact]
    public async Task Update_DoesNotRaiseAnyDomainEvent()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithUniquenessChecker(checker).BuildAsync();
        var eventsBefore = sut.DomainEvents.Count;

        await sut.Update("newname", "New Name", 0, true, checker);

        sut.DomainEvents.Count.ShouldBe(eventsBefore);
    }

    [Fact]
    public async Task Update_DoesNotIncrementVersion()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithUniquenessChecker(checker).BuildAsync();
        var versionBefore = sut.Version;

        await sut.Update("newname", "New Name", 0, true, checker);

        sut.Version.ShouldBe(versionBefore);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_WithNullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        await Should.ThrowAsync<ArgumentException>(
            () => sut.Update(name!, "d", 0, true, new StubAttributeTypeUniquenessChecker()));
    }

    [Fact]
    public async Task Update_WithNullUniquenessChecker_ThrowsArgumentException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        await Should.ThrowAsync<ArgumentException>(
            () => sut.Update("newname", "d", 0, true, null!));
    }

    [Fact]
    public async Task Update_WithNegativeSortOrder_ThrowsArgumentException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        await Should.ThrowAsync<ArgumentException>(
            () => sut.Update("newname", "d", -1, true, new StubAttributeTypeUniquenessChecker()));
    }

    [Fact]
    public async Task AddValue_WithValidInput_AddsValueAndSetsUpdatedAt()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        var added = sut.AddValue("red", "Red", "#FF0000", 1);

        sut.Values.Count.ShouldBe(1);
        added.Value.ShouldBe("red");
        added.DisplayValue.ShouldBe("Red");
        added.HexCode.ShouldBe("#FF0000");
        added.SortOrder.ShouldBe(1);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddValue_TrimsValueDisplayValueAndHexCode()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        var added = sut.AddValue("  red  ", "  Red  ", "  #FF0000  ", 0);

        added.Value.ShouldBe("red");
        added.DisplayValue.ShouldBe("Red");
        added.HexCode.ShouldBe("#FF0000");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddValue_WithEmptyOrWhitespaceDisplayValue_FallsBackToTrimmedValue(string? displayValue)
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        var added = sut.AddValue("  red  ", displayValue!);

        added.DisplayValue.ShouldBe("red");
    }

    [Fact]
    public async Task AddValue_WithNullHexCode_LeavesHexCodeNull()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        var added = sut.AddValue("red", "Red");

        added.HexCode.ShouldBeNull();
    }

    [Fact]
    public async Task AddValue_LinksChildEntityToParentAggregate()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        var added = sut.AddValue("red", "Red");

        added.AttributeTypeId.ShouldBe(sut.Id);
        added.AttributeType.ShouldBeSameAs(sut);
    }

    [Fact]
    public async Task AddValue_TwiceWithSameValueCaseInsensitive_ThrowsDuplicateAttributeException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        sut.AddValue("red", "Red");

        Should.Throw<DuplicateAttributeException>(() => sut.AddValue("RED", "Rouge"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddValue_WithNullOrWhitespaceValue_ThrowsArgumentException(string? value)
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        Should.Throw<ArgumentException>(() => sut.AddValue(value!, "Red"));
    }

    [Fact]
    public async Task AddValue_WithNegativeSortOrder_ThrowsArgumentException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        Should.Throw<ArgumentException>(() => sut.AddValue("red", "Red", null, -1));
    }

    [Fact]
    public async Task AddValue_RaisesAttributeValueAddedEvent()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        sut.ClearDomainEvents();

        var added = sut.AddValue("red", "Red");

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<AttributeValueAddedEvent>();
        evt.AttributeTypeId.ShouldBe(sut.Id);
        evt.AttributeValueId.ShouldBe(added.Id);
        evt.Value.ShouldBe("red");
        evt.DisplayValue.ShouldBe("Red");
    }

    [Fact]
    public async Task AddValue_IncrementsVersionByOne()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var versionBefore = sut.Version;

        sut.AddValue("red", "Red");

        sut.Version.ShouldBe(versionBefore + 1);
    }

    [Fact]
    public async Task UpdateValue_WithExistingId_AppliesTrimmedFieldsToChildEntity()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red", "#FF0000", 1);

        sut.UpdateValue(added.Id, "  crimson  ", "  Crimson  ", "  #DC143C  ", 2, false);

        added.Value.ShouldBe("crimson");
        added.DisplayValue.ShouldBe("Crimson");
        added.HexCode.ShouldBe("#DC143C");
        added.SortOrder.ShouldBe(2);
        added.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateValue_SetsAggregateUpdatedAt()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");
        var updatedAtBefore = sut.UpdatedAt;
        await Task.Delay(2);

        sut.UpdateValue(added.Id, "crimson", "Crimson", null, 0, true);

        sut.UpdatedAt.ShouldNotBeNull();
        sut.UpdatedAt.Value.ShouldBeGreaterThan(updatedAtBefore!.Value);
    }

    [Fact]
    public async Task UpdateValue_WhenValueSameCaseInsensitive_DoesNotThrow()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");
        sut.AddValue("blue", "Blue");

        Should.NotThrow(() => sut.UpdateValue(added.Id, "RED", "Red", null, 0, true));
        added.Value.ShouldBe("RED");
    }

    [Fact]
    public async Task UpdateValue_WhenNewValueCollidesWithSiblingCaseInsensitive_ThrowsDuplicateAttributeException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");
        sut.AddValue("blue", "Blue");

        Should.Throw<DuplicateAttributeException>(
            () => sut.UpdateValue(added.Id, "BLUE", "Blue", null, 0, true));
    }

    [Fact]
    public async Task UpdateValue_WithUnknownValueId_ThrowsAttributeValueNotFoundException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        Should.Throw<AttributeValueNotFoundException>(
            () => sut.UpdateValue(AttributeValueId.NewId(), "red", "Red", null, 0, true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateValue_WithNullOrWhitespaceValue_ThrowsArgumentException(string? value)
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");

        Should.Throw<ArgumentException>(
            () => sut.UpdateValue(added.Id, value!, "d", null, 0, true));
    }

    [Fact]
    public async Task UpdateValue_WithNegativeSortOrder_ThrowsArgumentException()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");

        Should.Throw<ArgumentException>(
            () => sut.UpdateValue(added.Id, "red", "Red", null, -1, true));
    }

    [Fact]
    public async Task UpdateValue_DoesNotRaiseAnyDomainEvent()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");
        var eventsBefore = sut.DomainEvents.Count;

        sut.UpdateValue(added.Id, "crimson", "Crimson", null, 0, true);

        sut.DomainEvents.Count.ShouldBe(eventsBefore);
    }

    [Fact]
    public async Task UpdateValue_DoesNotIncrementVersion()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var added = sut.AddValue("red", "Red");
        var versionBefore = sut.Version;

        sut.UpdateValue(added.Id, "crimson", "Crimson", null, 0, true);

        sut.Version.ShouldBe(versionBefore);
    }

    [Fact]
    public async Task MarkAsDeleted_OnLiveAggregate_SetsIsDeletedTrueAndIsActiveFalse()
    {
        var sut = await new AttributeTypeBuilder().WithIsActive(true).BuildAsync();
        var deletedBy = Guid.NewGuid();

        sut.MarkAsDeleted(deletedBy);

        sut.IsDeleted.ShouldBeTrue();
        sut.IsActive.ShouldBeFalse();
        sut.DeletedAt.ShouldNotBeNull();
        sut.DeletedBy.ShouldBe(deletedBy);
        sut.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task MarkAsDeleted_WithNullDeletedBy_LeavesDeletedByNull()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        sut.MarkAsDeleted(null);

        sut.IsDeleted.ShouldBeTrue();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsDeleted_WhenAlreadyDeleted_IsNoOp()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var firstUserId = Guid.NewGuid();
        sut.MarkAsDeleted(firstUserId);
        var firstDeletedAt = sut.DeletedAt;
        var firstUpdatedAt = sut.UpdatedAt;

        sut.MarkAsDeleted(Guid.NewGuid());

        sut.DeletedBy.ShouldBe(firstUserId);
        sut.DeletedAt.ShouldBe(firstDeletedAt);
        sut.UpdatedAt.ShouldBe(firstUpdatedAt);
    }

    [Fact]
    public async Task MarkAsDeleted_DoesNotRaiseAnyDomainEvent()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var eventsBefore = sut.DomainEvents.Count;

        sut.MarkAsDeleted(Guid.NewGuid());

        sut.DomainEvents.Count.ShouldBe(eventsBefore);
    }

    [Fact]
    public async Task MarkAsDeleted_DoesNotIncrementVersion()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var versionBefore = sut.Version;

        sut.MarkAsDeleted(Guid.NewGuid());

        sut.Version.ShouldBe(versionBefore);
    }

    [Fact]
    public async Task LifecycleSequence_CreateAddValueAddValueUpdateValue_ProducesTwoValues()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();
        var red = sut.AddValue("red", "Red");
        sut.AddValue("blue", "Blue");

        sut.UpdateValue(red.Id, "crimson", "Crimson", null, 0, true);

        sut.Values.Count.ShouldBe(2);
        sut.Values.ShouldContain(v => v.Value == "crimson");
        sut.Values.ShouldContain(v => v.Value == "blue");
    }

    [Fact]
    public async Task LifecycleSequence_VersionOnlyGrowsFromCreateAndAddValueNotFromUpdateOrUpdateValueOrMarkAsDeleted()
    {
        var checker = new StubAttributeTypeUniquenessChecker();
        var sut = await new AttributeTypeBuilder().WithUniquenessChecker(checker).BuildAsync();
        sut.Version.ShouldBe(1);

        var added = sut.AddValue("red", "Red");
        sut.Version.ShouldBe(2);

        await sut.Update("newname", "New Name", 0, true, checker);
        sut.Version.ShouldBe(2);

        sut.UpdateValue(added.Id, "crimson", "Crimson", null, 0, true);
        sut.Version.ShouldBe(2);

        sut.MarkAsDeleted(Guid.NewGuid());
        sut.Version.ShouldBe(2);
    }

    [Fact]
    public async Task Equality_TwoAggregatesWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = await new AttributeTypeBuilder().BuildAsync();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public async Task Equality_TwoAggregatesWithDifferentIds_AreConsideredUnequal()
    {
        var a = await new AttributeTypeBuilder().BuildAsync();
        var b = await new AttributeTypeBuilder().BuildAsync();

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
    }
}
