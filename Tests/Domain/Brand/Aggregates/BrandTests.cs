using Domain.Brand.Events;
using Domain.Brand.Exceptions;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Stubs;

namespace Tests.Domain.Brand.Aggregates;

public class BrandTests
{
    [Fact]
    public async Task Create_WithValidInputAndUniqueChecker_ReturnsInitializedBrand()
    {
        var name = new BrandNameBuilder().WithValue("Nike").Build();
        var slug = new BrandSlugBuilder().WithValue("nike").Build();
        var categoryId = CategoryId.NewId();
        var checker = new StubBrandUniquenessChecker();

        var brand = await new BrandBuilder()
            .WithName(name)
            .WithSlug(slug)
            .WithCategoryId(categoryId)
            .WithDescription("Sportswear")
            .WithLogoPath("brands/nike.png")
            .WithUniquenessChecker(checker)
            .BuildAsync();

        brand.ShouldNotBeNull();
        brand.Id.ShouldNotBeNull();
        brand.Id.Value.ShouldNotBe(Guid.Empty);
        brand.Name.ShouldBe(name);
        brand.Slug.ShouldBe(slug);
        brand.CategoryId.ShouldBe(categoryId);
        brand.Description.ShouldBe("Sportswear");
        brand.LogoPath.ShouldBe("brands/nike.png");
        brand.IsActive.ShouldBeTrue();
        brand.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var brand = await new BrandBuilder().BuildAsync();

        var after = DateTime.UtcNow.AddSeconds(1);
        brand.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        brand.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task Create_InvokesUniquenessCheckerOnceWithExcludeIdNull()
    {
        var checker = new StubBrandUniquenessChecker();
        var name = new BrandNameBuilder().WithValue("Adidas").Build();
        var slug = new BrandSlugBuilder().WithValue("adidas").Build();
        var categoryId = CategoryId.NewId();

        _ = await new BrandBuilder()
            .WithName(name)
            .WithSlug(slug)
            .WithCategoryId(categoryId)
            .WithUniquenessChecker(checker)
            .BuildAsync();

        checker.CallCount.ShouldBe(1);
        checker.LastName.ShouldBe(name);
        checker.LastSlug.ShouldBe(slug);
        checker.LastCategoryId.ShouldBe(categoryId);
        checker.LastExcludeId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_WhenCheckerReportsNotUnique_ThrowsBrandNameAlreadyExistsException()
    {
        var checker = new StubBrandUniquenessChecker().WithIsUnique(false);

        await Should.ThrowAsync<BrandNameAlreadyExistsException>(
            () => new BrandBuilder().WithUniquenessChecker(checker).BuildAsync());
    }

    [Fact]
    public async Task Create_WhenCheckerReportsNotUnique_ExceptionInheritsFromDomainException()
    {
        var checker = new StubBrandUniquenessChecker().WithIsUnique(false);

        var ex = await Should.ThrowAsync<BrandNameAlreadyExistsException>(
            () => new BrandBuilder().WithUniquenessChecker(checker).BuildAsync());

        ex.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public async Task Create_WithNullCategoryId_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => new BrandBuilder().WithCategoryId(null!).BuildAsync());
    }

    [Fact]
    public async Task Create_WithNullUniquenessChecker_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => new BrandBuilder().WithUniquenessChecker(null!).BuildAsync());
    }

    [Fact]
    public async Task Create_ProducesBrandWithVersionOne()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.Version.ShouldBe(1);
    }

    [Fact]
    public async Task Create_RaisesExactlyOneBrandCreatedEvent()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandCreatedEvent);
    }

    [Fact]
    public async Task Create_InitializesProductsCollectionAsEmpty()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.Products.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_LeavesSoftDeleteFieldsAtDefaults()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.ShouldBeAssignableTo<ISoftDeletable>();
        brand.IsDeleted.ShouldBeFalse();
        brand.DeletedAt.ShouldBeNull();
        brand.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateDetails_WithNewValues_AppliesThemAndSetsUpdatedAt()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();
        var newName = BrandName.Create("Renamed");
        var newSlug = BrandSlug.Create("renamed");

        await brand.UpdateDetails(newName, newSlug, checker, "new desc", "new/path.png", CancellationToken.None);

        brand.Name.ShouldBe(newName);
        brand.Slug.ShouldBe(newSlug);
        brand.Description.ShouldBe("new desc");
        brand.LogoPath.ShouldBe("new/path.png");
        brand.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateDetails_LeavesCategoryIdUnchanged()
    {
        var checker = new StubBrandUniquenessChecker();
        var originalCategory = CategoryId.NewId();
        var brand = await new BrandBuilder()
            .WithCategoryId(originalCategory)
            .WithUniquenessChecker(checker)
            .BuildAsync();

        await brand.UpdateDetails(
            BrandName.Create("Renamed"),
            BrandSlug.Create("renamed"),
            checker,
            null, null, CancellationToken.None);

        brand.CategoryId.ShouldBe(originalCategory);
    }

    [Fact]
    public async Task UpdateDetails_PassesCurrentIdAsExcludeIdToChecker()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();

        await brand.UpdateDetails(
            BrandName.Create("Renamed"),
            BrandSlug.Create("renamed"),
            checker,
            null, null, CancellationToken.None);

        checker.CallCount.ShouldBe(2);
        checker.LastExcludeId.ShouldBe(brand.Id);
    }

    [Fact]
    public async Task UpdateDetails_WhenCheckerReportsNotUnique_ThrowsBrandNameAlreadyExistsException()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();
        checker.WithIsUnique(false);

        await Should.ThrowAsync<BrandNameAlreadyExistsException>(
            () => brand.UpdateDetails(
                BrandName.Create("Renamed"),
                BrandSlug.Create("renamed"),
                checker, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateDetails_WithNullChecker_ThrowsArgumentNullException()
    {
        var brand = await new BrandBuilder().BuildAsync();

        await Should.ThrowAsync<ArgumentNullException>(
            () => brand.UpdateDetails(
                BrandName.Create("Renamed"),
                BrandSlug.Create("renamed"),
                null!, null, null, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateDetails_RaisesBrandUpdatedEvent()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();
        brand.ClearDomainEvents();

        await brand.UpdateDetails(
            BrandName.Create("Renamed"),
            BrandSlug.Create("renamed"),
            checker, null, null, CancellationToken.None);

        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandUpdatedEvent);
    }

    [Fact]
    public async Task UpdateDetails_IncrementsVersionByTwo()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();
        var versionBefore = brand.Version;

        await brand.UpdateDetails(
            BrandName.Create("Renamed"),
            BrandSlug.Create("renamed"),
            checker, null, null, CancellationToken.None);

        brand.Version.ShouldBe(versionBefore + 2);
    }

    [Fact]
    public async Task ChangeCategory_ToDifferentCategoryId_UpdatesCategoryAndSetsUpdatedAt()
    {
        var brand = await new BrandBuilder().BuildAsync();
        var newCategory = CategoryId.NewId();

        brand.ChangeCategory(newCategory);

        brand.CategoryId.ShouldBe(newCategory);
        brand.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ChangeCategory_ToDifferentCategoryId_RaisesBrandCategoryChangedEvent()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        brand.ChangeCategory(CategoryId.NewId());

        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandCategoryChangedEvent);
    }

    [Fact]
    public async Task ChangeCategory_ToDifferentCategoryId_IncrementsVersionByTwo()
    {
        var brand = await new BrandBuilder().BuildAsync();
        var versionBefore = brand.Version;

        brand.ChangeCategory(CategoryId.NewId());

        brand.Version.ShouldBe(versionBefore + 2);
    }

    [Fact]
    public async Task ChangeCategory_ToSameCategoryIdInstance_IsNoOp()
    {
        var categoryId = CategoryId.NewId();
        var brand = await new BrandBuilder().WithCategoryId(categoryId).BuildAsync();
        var versionBefore = brand.Version;
        brand.ClearDomainEvents();

        brand.ChangeCategory(categoryId);

        brand.CategoryId.ShouldBe(categoryId);
        brand.UpdatedAt.ShouldBeNull();
        brand.Version.ShouldBe(versionBefore);
        brand.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangeCategory_ToStructurallyEqualCategoryId_IsNoOp()
    {
        var guid = Guid.NewGuid();
        var originalCategory = CategoryId.From(guid);
        var equalButDifferentInstance = CategoryId.From(guid);
        var brand = await new BrandBuilder().WithCategoryId(originalCategory).BuildAsync();
        var versionBefore = brand.Version;
        brand.ClearDomainEvents();

        brand.ChangeCategory(equalButDifferentInstance);

        brand.Version.ShouldBe(versionBefore);
        brand.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangeCategory_WithNull_ThrowsArgumentNullException()
    {
        var brand = await new BrandBuilder().BuildAsync();

        Should.Throw<ArgumentNullException>(() => brand.ChangeCategory(null!));
    }

    [Fact]
    public async Task Activate_WhenAlreadyActive_ThrowsBrandAlreadyActiveException()
    {
        var brand = await new BrandBuilder().BuildAsync();

        Should.Throw<BrandAlreadyActiveException>(brand.Activate);
    }

    [Fact]
    public async Task Activate_WhenAlreadyActive_ExceptionInheritsFromDomainException()
    {
        var brand = await new BrandBuilder().BuildAsync();

        var ex = Should.Throw<BrandAlreadyActiveException>(brand.Activate);

        ex.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public async Task Activate_OnPreviouslyDeactivatedBrand_SetsIsActiveTrueAndRaisesEvent()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.Deactivate();
        brand.ClearDomainEvents();
        var versionBefore = brand.Version;

        brand.Activate();

        brand.IsActive.ShouldBeTrue();
        brand.UpdatedAt.ShouldNotBeNull();
        brand.Version.ShouldBe(versionBefore + 2);
        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandActivatedEvent);
    }

    [Fact]
    public async Task Deactivate_WhenActive_SetsIsActiveFalseAndRaisesEvent()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();
        var versionBefore = brand.Version;

        brand.Deactivate();

        brand.IsActive.ShouldBeFalse();
        brand.UpdatedAt.ShouldNotBeNull();
        brand.Version.ShouldBe(versionBefore + 2);
        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandDeactivatedEvent);
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyInactive_ThrowsBrandAlreadyDeactivatedException()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.Deactivate();

        Should.Throw<BrandAlreadyDeactivatedException>(brand.Deactivate);
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyInactive_ExceptionInheritsFromDomainException()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.Deactivate();

        var ex = Should.Throw<BrandAlreadyDeactivatedException>(brand.Deactivate);

        ex.ShouldBeAssignableTo<DomainException>();
    }

    [Fact]
    public async Task ClearDomainEvents_RemovesAllPendingEvents()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.DomainEvents.Count.ShouldBe(1);
        brand.ClearDomainEvents();
        brand.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task LifecycleSequence_CreateDeactivateActivateChangeCategoryUpdateDetails_AccumulatesEventsInOrder()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();

        brand.Deactivate();
        brand.Activate();
        brand.ChangeCategory(CategoryId.NewId());
        await brand.UpdateDetails(
            BrandName.Create("Renamed"),
            BrandSlug.Create("renamed"),
            checker, null, null, CancellationToken.None);

        brand.DomainEvents.Count.ShouldBe(5);
        brand.DomainEvents.ElementAt(0).ShouldBeOfType<BrandCreatedEvent>();
        brand.DomainEvents.ElementAt(1).ShouldBeOfType<BrandDeactivatedEvent>();
        brand.DomainEvents.ElementAt(2).ShouldBeOfType<BrandActivatedEvent>();
        brand.DomainEvents.ElementAt(3).ShouldBeOfType<BrandCategoryChangedEvent>();
        brand.DomainEvents.ElementAt(4).ShouldBeOfType<BrandUpdatedEvent>();
    }

    [Fact]
    public async Task LifecycleSequence_VersionGrowsByTwoPerMutationAfterCreate()
    {
        var checker = new StubBrandUniquenessChecker();
        var brand = await new BrandBuilder().WithUniquenessChecker(checker).BuildAsync();

        brand.Version.ShouldBe(1);
        brand.Deactivate();
        brand.Version.ShouldBe(3);
        brand.Activate();
        brand.Version.ShouldBe(5);
        brand.ChangeCategory(CategoryId.NewId());
        brand.Version.ShouldBe(7);
        await brand.UpdateDetails(
            BrandName.Create("Renamed"),
            BrandSlug.Create("renamed"),
            checker, null, null, CancellationToken.None);
        brand.Version.ShouldBe(9);
    }

    [Fact]
    public async Task Equality_TwoBrandsWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.Equals(brand).ShouldBeTrue();
    }

    [Fact]
    public async Task Equality_TwoBrandsWithDifferentIds_AreConsideredUnequal()
    {
        var a = await new BrandBuilder().BuildAsync();
        var b = await new BrandBuilder().BuildAsync();

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public async Task GetHashCode_ForSameBrand_IsStable()
    {
        var brand = await new BrandBuilder().BuildAsync();

        brand.GetHashCode().ShouldBe(brand.GetHashCode());
    }
}
