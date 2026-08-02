using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.Events;
using Domain.Product.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Product.Aggregates;

public class ProductTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedProduct()
    {
        var name = ProductName.Create("Nike Air Max");
        var slug = ProductSlug.Create("nike-air-max");
        var brandId = BrandId.NewId();
        var categoryId = CategoryId.NewId();

        var sut = new ProductBuilder()
            .WithName(name)
            .WithSlug(slug)
            .WithDescription("Comfortable running shoes")
            .WithBrandId(brandId)
            .WithCategoryId(categoryId)
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.Name.ShouldBe(name);
        sut.Slug.ShouldBe(slug);
        sut.Description.ShouldBe("Comfortable running shoes");
        sut.BrandId.ShouldBe(brandId);
        sut.CategoryId.ShouldBe(categoryId);
        sut.IsActive.ShouldBeTrue();
        sut.IsFeatured.ShouldBeFalse();
        sut.AverageRating.ShouldBe(0d);
        sut.ReviewCount.ShouldBe(0);
    }

    [Fact]
    public void Create_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new ProductBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.UpdatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithNullDescription_StoresEmptyString()
    {
        var sut = new ProductBuilder().WithDescription(null!).Build();

        sut.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_LeavesSoftDeleteFieldsAtDefaults()
    {
        var sut = new ProductBuilder().Build();

        sut.ShouldBeAssignableTo<ISoftDeletable>();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedAt.ShouldBeNull();
        sut.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public void Create_InitializesVariantsCollectionAsEmpty()
    {
        var sut = new ProductBuilder().Build();

        sut.Variants.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ProducesProductWithVersionOne()
    {
        var sut = new ProductBuilder().Build();

        sut.Version.ShouldBe(1);
    }

    [Fact]
    public void Create_RaisesExactlyOneProductCreatedEvent()
    {
        var sut = new ProductBuilder().Build();

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductCreatedEvent>();
    }

    [Fact]
    public void UpdateDetails_WithNewValues_AppliesThemAndSetsUpdatedAt()
    {
        var sut = new ProductBuilder().Build();
        var updatedAtBefore = sut.UpdatedAt;
        var newName = ProductName.Create("Renamed");
        var newSlug = ProductSlug.Create("renamed");
        System.Threading.Thread.Sleep(2);

        sut.UpdateDetails(newName, newSlug, "new desc");

        sut.Name.ShouldBe(newName);
        sut.Slug.ShouldBe(newSlug);
        sut.Description.ShouldBe("new desc");
        sut.UpdatedAt.ShouldBeGreaterThan(updatedAtBefore);
    }

    [Fact]
    public void UpdateDetails_RaisesProductUpdatedEvent()
    {
        var sut = new ProductBuilder().Build();
        sut.ClearDomainEvents();

        sut.UpdateDetails(ProductName.Create("Renamed"), ProductSlug.Create("renamed"), "d");

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductUpdatedEvent>();
    }

    [Fact]
    public void UpdateDetails_IncrementsVersionByOne()
    {
        var sut = new ProductBuilder().Build();
        var versionBefore = sut.Version;

        sut.UpdateDetails(ProductName.Create("Renamed"), ProductSlug.Create("renamed"), "d");

        sut.Version.ShouldBe(versionBefore + 1);
    }

    [Fact]
    public void ChangeBrand_ToDifferentBrandId_UpdatesAndRaisesEvent()
    {
        var sut = new ProductBuilder().Build();
        var newBrand = BrandId.NewId();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangeBrand(newBrand);

        sut.BrandId.ShouldBe(newBrand);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductBrandChangedEvent>();
    }

    [Fact]
    public void ChangeBrand_ToSameBrandIdInstance_IsNoOp()
    {
        var brandId = BrandId.NewId();
        var sut = new ProductBuilder().WithBrandId(brandId).Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var updatedAtBefore = sut.UpdatedAt;

        sut.ChangeBrand(brandId);

        sut.Version.ShouldBe(versionBefore);
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeBrand_ToStructurallyEqualBrandId_IsNoOp()
    {
        var guid = Guid.NewGuid();
        var sut = new ProductBuilder().WithBrandId(BrandId.From(guid)).Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangeBrand(BrandId.From(guid));

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangeCategory_ToDifferentCategoryId_UpdatesAndRaisesEvent()
    {
        var sut = new ProductBuilder().Build();
        var newCategory = CategoryId.NewId();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangeCategory(newCategory);

        sut.CategoryId.ShouldBe(newCategory);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductCategoryChangedEvent>();
    }

    [Fact]
    public void ChangeCategory_ToSameCategoryIdInstance_IsNoOp()
    {
        var categoryId = CategoryId.NewId();
        var sut = new ProductBuilder().WithCategoryId(categoryId).Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.ChangeCategory(categoryId);

        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_IsNoOp()
    {
        var sut = new ProductBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var updatedAtBefore = sut.UpdatedAt;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore);
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Activate_OnPreviouslyDeactivatedProduct_SetsIsActiveTrueAndRaisesEvent()
    {
        var sut = new ProductBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Activate();

        sut.IsActive.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductActivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveFalseAndRaisesEvent()
    {
        var sut = new ProductBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<ProductDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsNoOp()
    {
        var sut = new ProductBuilder().Build();
        sut.Deactivate();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Deactivate();

        sut.IsActive.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAsFeatured_WhenNotFeatured_SetsIsFeaturedTrueWithoutRaisingEvent()
    {
        var sut = new ProductBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkAsFeatured();

        sut.IsFeatured.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAsFeatured_WhenAlreadyFeatured_IsNoOp()
    {
        var sut = new ProductBuilder().Build();
        sut.MarkAsFeatured();
        var updatedAtBefore = sut.UpdatedAt;
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkAsFeatured();

        sut.IsFeatured.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore);
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UnmarkAsFeatured_WhenFeatured_SetsIsFeaturedFalseWithoutRaisingEvent()
    {
        var sut = new ProductBuilder().Build();
        sut.MarkAsFeatured();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.UnmarkAsFeatured();

        sut.IsFeatured.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UnmarkAsFeatured_WhenNotFeatured_IsNoOp()
    {
        var sut = new ProductBuilder().Build();
        var updatedAtBefore = sut.UpdatedAt;
        sut.ClearDomainEvents();

        sut.UnmarkAsFeatured();

        sut.IsFeatured.ShouldBeFalse();
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Restore_WhenNotDeleted_IsNoOp()
    {
        var sut = new ProductBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var updatedAtBefore = sut.UpdatedAt;

        sut.Restore();

        sut.IsDeleted.ShouldBeFalse();
        sut.Version.ShouldBe(versionBefore);
        sut.UpdatedAt.ShouldBe(updatedAtBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RecalculateReviewStats_WithValidInput_StoresRoundedAverageAndCount()
    {
        var sut = new ProductBuilder().Build();

        sut.RecalculateReviewStats(4.2567d, 42);

        sut.AverageRating.ShouldBe(4.26d);
        sut.ReviewCount.ShouldBe(42);
    }

    [Fact]
    public void RecalculateReviewStats_WithZeroReviewCount_ForcesAverageRatingToZeroRegardlessOfInput()
    {
        var sut = new ProductBuilder().Build();

        sut.RecalculateReviewStats(5d, 0);

        sut.AverageRating.ShouldBe(0d);
        sut.ReviewCount.ShouldBe(0);
    }

    [Fact]
    public void RecalculateReviewStats_WithNegativeReviewCount_ThrowsDomainException()
    {
        var sut = new ProductBuilder().Build();

        Should.Throw<DomainException>(() => sut.RecalculateReviewStats(3d, -1));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(5.01)]
    [InlineData(10)]
    public void RecalculateReviewStats_WithAverageOutOfRange_ThrowsDomainException(double avg)
    {
        var sut = new ProductBuilder().Build();

        Should.Throw<DomainException>(() => sut.RecalculateReviewStats(avg, 10));
    }

    [Fact]
    public void RecalculateReviewStats_WhenValuesUnchanged_IsNoOpNotBumpingUpdatedAt()
    {
        var sut = new ProductBuilder().Build();
        sut.RecalculateReviewStats(4.5d, 10);
        var updatedAtBefore = sut.UpdatedAt;

        sut.RecalculateReviewStats(4.5d, 10);

        sut.UpdatedAt.ShouldBe(updatedAtBefore);
    }

    [Fact]
    public void RecalculateReviewStats_DoesNotRaiseAnyDomainEvent()
    {
        var sut = new ProductBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.RecalculateReviewStats(4.5d, 10);

        sut.DomainEvents.ShouldBeEmpty();
        sut.Version.ShouldBe(versionBefore);
    }

    [Fact]
    public void LifecycleSequence_CreateUpdateDeactivateActivateChangeBrandChangeCategory_AccumulatesEventsInOrder()
    {
        var sut = new ProductBuilder().Build();

        sut.UpdateDetails(ProductName.Create("Renamed"), ProductSlug.Create("renamed"), "d");
        sut.Deactivate();
        sut.Activate();
        sut.ChangeBrand(BrandId.NewId());
        sut.ChangeCategory(CategoryId.NewId());

        sut.DomainEvents.Count.ShouldBe(6);
        sut.DomainEvents.ElementAt(0).ShouldBeOfType<ProductCreatedEvent>();
        sut.DomainEvents.ElementAt(1).ShouldBeOfType<ProductUpdatedEvent>();
        sut.DomainEvents.ElementAt(2).ShouldBeOfType<ProductDeactivatedEvent>();
        sut.DomainEvents.ElementAt(3).ShouldBeOfType<ProductActivatedEvent>();
        sut.DomainEvents.ElementAt(4).ShouldBeOfType<ProductBrandChangedEvent>();
        sut.DomainEvents.ElementAt(5).ShouldBeOfType<ProductCategoryChangedEvent>();
    }

    [Fact]
    public void LifecycleSequence_VersionGrowsByOnePerRaisedEvent()
    {
        var sut = new ProductBuilder().Build();

        sut.Version.ShouldBe(1);
        sut.UpdateDetails(ProductName.Create("Renamed"), ProductSlug.Create("renamed"), "d");
        sut.Version.ShouldBe(2);
        sut.Deactivate();
        sut.Version.ShouldBe(3);
        sut.Activate();
        sut.Version.ShouldBe(4);
        sut.ChangeBrand(BrandId.NewId());
        sut.Version.ShouldBe(5);
        sut.ChangeCategory(CategoryId.NewId());
        sut.Version.ShouldBe(6);
    }

    [Fact]
    public void LifecycleSequence_FeaturedAndReviewStatsMutationsDoNotBumpVersion()
    {
        var sut = new ProductBuilder().Build();
        var versionAfterCreate = sut.Version;

        sut.MarkAsFeatured();
        sut.UnmarkAsFeatured();
        sut.MarkAsFeatured();
        sut.RecalculateReviewStats(4.5d, 10);
        sut.Restore();

        sut.Version.ShouldBe(versionAfterCreate);
    }

    [Fact]
    public void Equality_TwoProductsWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new ProductBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoProductsWithDifferentIds_AreConsideredUnequal()
    {
        var a = new ProductBuilder().Build();
        var b = new ProductBuilder().Build();

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
    }
}
