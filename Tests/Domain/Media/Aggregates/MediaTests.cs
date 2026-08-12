using Domain.Media.Events;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Media.Aggregates;

public class MediaTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedMedia()
    {
        var entityId = Guid.NewGuid();

        var media = new MediaBuilder()
            .WithFilePath("uploads/products/hero.png")
            .WithFileName("hero.png")
            .WithFileType("image/png")
            .WithFileSize(2048)
            .WithEntityType("Product")
            .WithEntityId(entityId)
            .WithSortOrder(3)
            .WithIsPrimary(true)
            .WithAltText("Hero image")
            .Build();

        media.ShouldNotBeNull();
        media.Id.ShouldNotBeNull();
        media.Id.Value.ShouldNotBe(Guid.Empty);
        media.FileType.ShouldBe("image/png");
        media.EntityType.ShouldBe("Product");
        media.EntityId.ShouldBe(entityId);
        media.SortOrder.ShouldBe(3);
        media.IsPrimary.ShouldBeTrue();
        media.AltText.ShouldBe("Hero image");
        media.IsActive.ShouldBeTrue();
        media.IsDeleted.ShouldBeFalse();
        media.DeletedAt.ShouldBeNull();
        media.DeletedBy.ShouldBeNull();
        media.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var media = new MediaBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        media.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        media.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithUppercaseFileType_NormalizesToLowercase()
    {
        var media = new MediaBuilder().WithFileType("IMAGE/PNG").Build();

        media.FileType.ShouldBe("image/png");
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceOnFileType_TrimsAndLowercases()
    {
        var media = new MediaBuilder().WithFileType("  Image/JPEG  ").Build();

        media.FileType.ShouldBe("image/jpeg");
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceOnEntityType_TrimsEntityType()
    {
        var media = new MediaBuilder().WithEntityType("  Product  ").Build();

        media.EntityType.ShouldBe("Product");
    }

    [Fact]
    public void Create_WithSurroundingWhitespaceOnAltText_TrimsAltText()
    {
        var media = new MediaBuilder().WithAltText("  hello  ").Build();

        media.AltText.ShouldBe("hello");
    }

    [Fact]
    public void Create_WithNullAltText_LeavesAltTextNull()
    {
        var media = new MediaBuilder().WithAltText(null).Build();

        media.AltText.ShouldBeNull();
    }

    [Fact]
    public void Create_WithAltTextExactlyAtMaxLength_Succeeds()
    {
        var maxText = new string('a', 500);

        var media = new MediaBuilder().WithAltText(maxText).Build();

        media.AltText.ShouldBe(maxText);
    }

    [Fact]
    public void Create_WithAltTextExceedingMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('a', 501);

        Should.Throw<DomainException>(() => new MediaBuilder().WithAltText(tooLong).Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingFileType_ThrowsArgumentException(string? fileType)
    {
        Should.Throw<ArgumentException>(() => new MediaBuilder().WithFileType(fileType!).Build());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingEntityType_ThrowsArgumentException(string? entityType)
    {
        Should.Throw<ArgumentException>(() => new MediaBuilder().WithEntityType(entityType!).Build());
    }

    [Fact]
    public void Create_WithNegativeSortOrder_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() => new MediaBuilder().WithSortOrder(-1).Build());
    }

    [Fact]
    public void Create_WithZeroSortOrder_Succeeds()
    {
        new MediaBuilder().WithSortOrder(0).Build().SortOrder.ShouldBe(0);
    }

    [Fact]
    public void Create_WithFilePathWithoutDirectory_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            new MediaBuilder().WithFilePath("hero.png").WithFileName("hero.png").Build());
    }

    [Fact]
    public void Create_WithBackslashesInFilePath_NormalizesToForwardSlashes()
    {
        var media = new MediaBuilder()
            .WithFilePath(@"uploads\products\hero.png")
            .WithFileName("hero.png")
            .Build();

        media.FilePath.ShouldBe("uploads/products/hero.png");
    }

    [Fact]
    public void Create_ExposesFileNameAndExtensionFromUnderlyingFilePath()
    {
        var media = new MediaBuilder()
            .WithFilePath("uploads/products/hero.png")
            .WithFileName("hero.png")
            .Build();

        media.FileName.ShouldBe("hero.png");
        media.Extension.ShouldBe("png");
    }

    [Fact]
    public void Create_ExposesFileSizeFromUnderlyingSize()
    {
        var media = new MediaBuilder().WithFileSize(4096).Build();

        media.FileSize.ShouldBe(4096L);
    }

    [Fact]
    public void Create_RaisesExactlyOneMediaCreatedEvent()
    {
        var media = new MediaBuilder().Build();

        media.DomainEvents.Count.ShouldBe(1);
        media.DomainEvents.ShouldContain(e => e is MediaCreatedEvent);
    }

    [Fact]
    public void Create_MediaCreatedEvent_CarriesIdAndEntityCoordinates()
    {
        var entityId = Guid.NewGuid();

        var media = new MediaBuilder()
            .WithEntityType("Brand")
            .WithEntityId(entityId)
            .Build();

        var evt = media.DomainEvents.OfType<MediaCreatedEvent>().Single();
        evt.MediaId.ShouldBe(media.Id);
        evt.EntityType.ShouldBe("Brand");
        evt.EntityId.ShouldBe(entityId);
    }

    [Fact]
    public void Create_ImplementsAuditableActivatableAndSoftDeletableContracts()
    {
        var media = new MediaBuilder().Build();

        media.ShouldBeAssignableTo<IAuditable>();
        media.ShouldBeAssignableTo<IActivatable>();
        media.ShouldBeAssignableTo<ISoftDeletable>();
    }

    [Fact]
    public void UpdateSortOrder_WithNonNegativeValue_UpdatesSortOrderAndSetsUpdatedAt()
    {
        var media = new MediaBuilder().WithSortOrder(0).Build();

        media.UpdateSortOrder(7);

        media.SortOrder.ShouldBe(7);
        media.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void UpdateSortOrder_WithZero_Succeeds()
    {
        var media = new MediaBuilder().WithSortOrder(5).Build();

        media.UpdateSortOrder(0);

        media.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void UpdateSortOrder_WithNegativeValue_ThrowsDomainException()
    {
        var media = new MediaBuilder().Build();

        Should.Throw<DomainException>(() => media.UpdateSortOrder(-1));
    }

    [Fact]
    public void UpdateSortOrder_RaisesNoDomainEvent()
    {
        var media = new MediaBuilder().Build();
        media.ClearDomainEvents();

        media.UpdateSortOrder(2);

        media.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetAsPrimary_WhenNotPrimary_MarksPrimaryAndSetsUpdatedAtAndRaisesEvent()
    {
        var media = new MediaBuilder().WithIsPrimary(false).Build();
        media.ClearDomainEvents();

        media.SetAsPrimary();

        media.IsPrimary.ShouldBeTrue();
        media.UpdatedAt.ShouldNotBeNull();
        media.DomainEvents.Count.ShouldBe(1);
        media.DomainEvents.ShouldContain(e => e is MediaSetAsPrimaryEvent);
    }

    [Fact]
    public void SetAsPrimary_Event_CarriesIdAndEntityCoordinates()
    {
        var entityId = Guid.NewGuid();
        var media = new MediaBuilder()
            .WithEntityType("Category")
            .WithEntityId(entityId)
            .WithIsPrimary(false)
            .Build();
        media.ClearDomainEvents();

        media.SetAsPrimary();

        var evt = media.DomainEvents.OfType<MediaSetAsPrimaryEvent>().Single();
        evt.MediaId.ShouldBe(media.Id);
        evt.EntityType.ShouldBe("Category");
        evt.EntityId.ShouldBe(entityId);
    }

    [Fact]
    public void SetAsPrimary_WhenAlreadyPrimary_IsNoOp()
    {
        var media = new MediaBuilder().WithIsPrimary(true).Build();
        media.ClearDomainEvents();
        var updatedAtBefore = media.UpdatedAt;

        media.SetAsPrimary();

        media.IsPrimary.ShouldBeTrue();
        media.UpdatedAt.ShouldBe(updatedAtBefore);
        media.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void SetAsPrimary_WhenDeleted_ThrowsDomainException()
    {
        var media = new MediaBuilder().BuildDeleted();

        Should.Throw<DomainException>(media.SetAsPrimary);
    }

    [Fact]
    public void RemovePrimary_WhenPrimary_ClearsPrimaryAndSetsUpdatedAt()
    {
        var media = new MediaBuilder().WithIsPrimary(true).Build();

        media.RemovePrimary();

        media.IsPrimary.ShouldBeFalse();
        media.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void RemovePrimary_WhenAlreadyNotPrimary_IsNoOp()
    {
        var media = new MediaBuilder().WithIsPrimary(false).Build();
        var updatedAtBefore = media.UpdatedAt;
        media.ClearDomainEvents();

        media.RemovePrimary();

        media.IsPrimary.ShouldBeFalse();
        media.UpdatedAt.ShouldBe(updatedAtBefore);
        media.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemovePrimary_RaisesNoDomainEvent()
    {
        var media = new MediaBuilder().WithIsPrimary(true).Build();
        media.ClearDomainEvents();

        media.RemovePrimary();

        media.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RequestDeletion_WithUserId_MarksDeletedAndInactiveAndRaisesEvent()
    {
        var deletedBy = UserId.From(Guid.NewGuid());
        var media = new MediaBuilder().WithIsPrimary(true).Build();
        media.ClearDomainEvents();

        media.RequestDeletion(deletedBy);

        media.IsDeleted.ShouldBeTrue();
        media.IsActive.ShouldBeFalse();
        media.IsPrimary.ShouldBeFalse();
        media.DeletedAt.ShouldNotBeNull();
        media.DeletedBy.ShouldBe(deletedBy.Value);
        media.UpdatedAt.ShouldNotBeNull();
        media.DomainEvents.Count.ShouldBe(1);
        media.DomainEvents.ShouldContain(e => e is MediaDeletedEvent);
    }

    [Fact]
    public void RequestDeletion_Event_CarriesIdEntityCoordinatesAndDeletedBy()
    {
        var deletedBy = UserId.From(Guid.NewGuid());
        var entityId = Guid.NewGuid();
        var media = new MediaBuilder()
            .WithEntityType("Product")
            .WithEntityId(entityId)
            .Build();
        media.ClearDomainEvents();

        media.RequestDeletion(deletedBy);

        var evt = media.DomainEvents.OfType<MediaDeletedEvent>().Single();
        evt.MediaId.ShouldBe(media.Id);
        evt.EntityType.ShouldBe("Product");
        evt.EntityId.ShouldBe(entityId);
        evt.DeletedBy.ShouldBe(deletedBy);
    }

    [Fact]
    public void RequestDeletion_WithNullUserId_LeavesDeletedByNullOnAggregateAndEvent()
    {
        var media = new MediaBuilder().Build();
        media.ClearDomainEvents();

        media.RequestDeletion(null);

        media.DeletedBy.ShouldBeNull();
        var evt = media.DomainEvents.OfType<MediaDeletedEvent>().Single();
        evt.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void RequestDeletion_SetsDeletedAtAndUpdatedAtToTheSameInstant()
    {
        var media = new MediaBuilder().Build();

        media.RequestDeletion();

        media.DeletedAt.ShouldBe(media.UpdatedAt);
    }

    [Fact]
    public void RequestDeletion_WhenAlreadyDeleted_IsNoOp()
    {
        var media = new MediaBuilder().Build();
        media.RequestDeletion(UserId.From(Guid.NewGuid()));
        var deletedAtBefore = media.DeletedAt;
        var deletedByBefore = media.DeletedBy;
        media.ClearDomainEvents();

        media.RequestDeletion(UserId.From(Guid.NewGuid()));

        media.DeletedAt.ShouldBe(deletedAtBefore);
        media.DeletedBy.ShouldBe(deletedByBefore);
        media.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void CanBeSetAsPrimary_WhenActiveAndNotPrimaryAndNotDeleted_ReturnsTrue()
    {
        var media = new MediaBuilder().WithIsPrimary(false).Build();

        media.CanBeSetAsPrimary().ShouldBeTrue();
    }

    [Fact]
    public void CanBeSetAsPrimary_WhenAlreadyPrimary_ReturnsFalse()
    {
        var media = new MediaBuilder().WithIsPrimary(true).Build();

        media.CanBeSetAsPrimary().ShouldBeFalse();
    }

    [Fact]
    public void CanBeSetAsPrimary_WhenDeleted_ReturnsFalse()
    {
        var media = new MediaBuilder().BuildDeleted();

        media.CanBeSetAsPrimary().ShouldBeFalse();
    }

    [Fact]
    public void LifecycleSequence_CreateSetAsPrimaryUpdateSortOrderRemovePrimaryRequestDeletion_AccumulatesEventsInOrder()
    {
        var media = new MediaBuilder().WithIsPrimary(false).Build();

        media.SetAsPrimary();
        media.UpdateSortOrder(4);
        media.RemovePrimary();
        media.RequestDeletion(UserId.From(Guid.NewGuid()));

        media.DomainEvents.Count.ShouldBe(3);
        media.DomainEvents.ElementAt(0).ShouldBeOfType<MediaCreatedEvent>();
        media.DomainEvents.ElementAt(1).ShouldBeOfType<MediaSetAsPrimaryEvent>();
        media.DomainEvents.ElementAt(2).ShouldBeOfType<MediaDeletedEvent>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        var media = new MediaBuilder().Build();

        media.DomainEvents.Count.ShouldBe(1);
        media.ClearDomainEvents();
        media.DomainEvents.ShouldBeEmpty();
    }
}
