using Domain.Category.Events;
using Domain.Category.Exceptions;
using Domain.Category.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Stubs;

namespace Tests.Domain.Category.Aggregates;

public class CategoryTests
{
    [Fact]
    public async Task Create_WithValidInput_ReturnsInitializedCategory()
    {
        var id = CategoryId.NewId();
        var name = new CategoryNameBuilder().WithValue("Books").Build();
        var slug = new CategorySlugBuilder().WithValue("books").Build();

        var category = await new CategoryBuilder()
            .WithId(id)
            .WithName(name)
            .WithSlug(slug)
            .WithDescription("All kinds of books")
            .WithSortOrder(5)
            .BuildAsync();

        category.ShouldNotBeNull();
        category.Id.ShouldBe(id);
        category.Name.ShouldBe(name);
        category.Slug.ShouldBe(slug);
        category.Description.ShouldBe("All kinds of books");
        category.SortOrder.ShouldBe(5);
        category.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_SetsCreatedAtAndUpdatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var category = await new CategoryBuilder().BuildAsync();

        var after = DateTime.UtcNow.AddSeconds(1);
        category.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        category.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        category.UpdatedAt.ShouldBeGreaterThanOrEqualTo(before);
        category.UpdatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public async Task Create_InvokesUniquenessCheckerOnceWithExcludeIdNull()
    {
        var checker = new StubCategoryUniquenessChecker();
        var name = new CategoryNameBuilder().WithValue("Games").Build();
        var slug = new CategorySlugBuilder().WithValue("games").Build();

        _ = await new CategoryBuilder()
            .WithName(name)
            .WithSlug(slug)
            .WithUniquenessChecker(checker)
            .BuildAsync();

        checker.CallCount.ShouldBe(1);
        checker.LastName.ShouldBe(name);
        checker.LastSlug.ShouldBe(slug);
        checker.LastExcludeId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_WhenCheckerReportsNotUnique_ThrowsDuplicateCategoryNameException()
    {
        var checker = new StubCategoryUniquenessChecker().WithIsUnique(false);

        await Should.ThrowAsync<DuplicateCategoryNameException>(
            () => new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync());
    }

    [Fact]
    public async Task Create_WithNullUniquenessChecker_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => new CategoryBuilder().WithUniquenessChecker(null!).BuildAsync());
    }

    [Fact]
    public async Task Create_ProducesCategoryWithVersionOne()
    {
        var category = await new CategoryBuilder().BuildAsync();

        category.Version.ShouldBe(1);
    }

    [Fact]
    public async Task Create_RaisesExactlyOneCategoryCreatedEvent()
    {
        var category = await new CategoryBuilder().BuildAsync();

        category.DomainEvents.Count.ShouldBe(1);
        category.DomainEvents.Single().ShouldBeOfType<CategoryCreatedEvent>();
    }

    [Fact]
    public async Task Create_InitializesBrandsCollectionAsEmpty()
    {
        var category = await new CategoryBuilder().BuildAsync();

        category.Brands.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateDetails_WithNewValues_AppliesThemAndBumpsUpdatedAt()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();
        var updatedAtBefore = category.UpdatedAt;
        var newName = CategoryName.Create("Renamed");
        var newSlug = CategorySlug.Create("renamed");
        await Task.Delay(2);

        await category.UpdateDetails(newName, newSlug, checker, "new desc", 42, CancellationToken.None);

        category.Name.ShouldBe(newName);
        category.Slug.ShouldBe(newSlug);
        category.Description.ShouldBe("new desc");
        category.SortOrder.ShouldBe(42);
        category.UpdatedAt.ShouldBeGreaterThan(updatedAtBefore);
    }

    [Fact]
    public async Task UpdateDetails_PassesCurrentIdAsExcludeIdToChecker()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();

        await category.UpdateDetails(
            CategoryName.Create("Renamed"),
            CategorySlug.Create("renamed"),
            checker, null, 1, CancellationToken.None);

        checker.CallCount.ShouldBe(2);
        checker.LastExcludeId.ShouldBe(category.Id);
    }

    [Fact]
    public async Task UpdateDetails_WhenCheckerReportsNotUnique_ThrowsDuplicateCategoryNameException()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();
        checker.WithIsUnique(false);

        await Should.ThrowAsync<DuplicateCategoryNameException>(
            () => category.UpdateDetails(
                CategoryName.Create("Renamed"),
                CategorySlug.Create("renamed"),
                checker, null, 0, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateDetails_WithNullChecker_ThrowsArgumentNullException()
    {
        var category = await new CategoryBuilder().BuildAsync();

        await Should.ThrowAsync<ArgumentNullException>(
            () => category.UpdateDetails(
                CategoryName.Create("Renamed"),
                CategorySlug.Create("renamed"),
                null!, null, 0, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateDetails_RaisesCategoryUpdatedEvent()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();
        category.ClearDomainEvents();

        await category.UpdateDetails(
            CategoryName.Create("Renamed"),
            CategorySlug.Create("renamed"),
            checker, "d", 3, CancellationToken.None);

        category.DomainEvents.Count.ShouldBe(1);
        category.DomainEvents.Single().ShouldBeOfType<CategoryUpdatedEvent>();
    }

    [Fact]
    public async Task UpdateDetails_IncrementsVersionByTwo()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();
        var versionBefore = category.Version;

        await category.UpdateDetails(
            CategoryName.Create("Renamed"),
            CategorySlug.Create("renamed"),
            checker, null, 0, CancellationToken.None);

        category.Version.ShouldBe(versionBefore + 2);
    }

    [Fact]
    public async Task Activate_WhenAlreadyActive_IsNoOpNotThrowingAndProducesNoEvent()
    {
        var category = await new CategoryBuilder().BuildAsync();
        var versionBefore = category.Version;
        var updatedAtBefore = category.UpdatedAt;
        category.ClearDomainEvents();

        Should.NotThrow(category.Activate);

        category.IsActive.ShouldBeTrue();
        category.Version.ShouldBe(versionBefore);
        category.UpdatedAt.ShouldBe(updatedAtBefore);
        category.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Activate_OnPreviouslyDeactivatedCategory_SetsIsActiveTrueAndRaisesEvent()
    {
        var category = await new CategoryBuilder().BuildAsync();
        category.Deactivate();
        category.ClearDomainEvents();
        var versionBefore = category.Version;
        var updatedAtBefore = category.UpdatedAt;
        await Task.Delay(2);

        category.Activate();

        category.IsActive.ShouldBeTrue();
        category.Version.ShouldBe(versionBefore + 2);
        category.UpdatedAt.ShouldBeGreaterThan(updatedAtBefore);
        category.DomainEvents.Count.ShouldBe(1);
        category.DomainEvents.Single().ShouldBeOfType<CategoryActivatedEvent>();
    }

    [Fact]
    public async Task Deactivate_WhenActive_SetsIsActiveFalseAndRaisesEvent()
    {
        var category = await new CategoryBuilder().BuildAsync();
        category.ClearDomainEvents();
        var versionBefore = category.Version;
        var updatedAtBefore = category.UpdatedAt;
        await Task.Delay(2);

        category.Deactivate();

        category.IsActive.ShouldBeFalse();
        category.Version.ShouldBe(versionBefore + 2);
        category.UpdatedAt.ShouldBeGreaterThan(updatedAtBefore);
        category.DomainEvents.Count.ShouldBe(1);
        category.DomainEvents.Single().ShouldBeOfType<CategoryDeactivatedEvent>();
    }

    [Fact]
    public async Task Deactivate_WhenAlreadyInactive_IsNoOpNotThrowingAndProducesNoEvent()
    {
        var category = await new CategoryBuilder().BuildAsync();
        category.Deactivate();
        category.ClearDomainEvents();
        var versionBefore = category.Version;
        var updatedAtBefore = category.UpdatedAt;

        Should.NotThrow(category.Deactivate);

        category.IsActive.ShouldBeFalse();
        category.Version.ShouldBe(versionBefore);
        category.UpdatedAt.ShouldBe(updatedAtBefore);
        category.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearDomainEvents_RemovesAllPendingEvents()
    {
        var category = await new CategoryBuilder().BuildAsync();

        category.DomainEvents.Count.ShouldBe(1);
        category.ClearDomainEvents();
        category.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task LifecycleSequence_CreateDeactivateActivateUpdateDetails_AccumulatesEventsInOrder()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();

        category.Deactivate();
        category.Activate();
        await category.UpdateDetails(
            CategoryName.Create("Renamed"),
            CategorySlug.Create("renamed"),
            checker, null, 0, CancellationToken.None);

        category.DomainEvents.Count.ShouldBe(4);
        category.DomainEvents.ElementAt(0).ShouldBeOfType<CategoryCreatedEvent>();
        category.DomainEvents.ElementAt(1).ShouldBeOfType<CategoryDeactivatedEvent>();
        category.DomainEvents.ElementAt(2).ShouldBeOfType<CategoryActivatedEvent>();
        category.DomainEvents.ElementAt(3).ShouldBeOfType<CategoryUpdatedEvent>();
    }

    [Fact]
    public async Task LifecycleSequence_VersionGrowsByTwoPerRealMutation()
    {
        var checker = new StubCategoryUniquenessChecker();
        var category = await new CategoryBuilder().WithUniquenessChecker(checker).BuildAsync();

        category.Version.ShouldBe(1);
        category.Deactivate();
        category.Version.ShouldBe(3);
        category.Activate();
        category.Version.ShouldBe(5);
        await category.UpdateDetails(
            CategoryName.Create("Renamed"),
            CategorySlug.Create("renamed"),
            checker, null, 0, CancellationToken.None);
        category.Version.ShouldBe(7);
    }

    [Fact]
    public async Task Equality_TwoCategoriesWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var category = await new CategoryBuilder().BuildAsync();

        category.Equals(category).ShouldBeTrue();
    }

    [Fact]
    public async Task Equality_TwoCategoriesWithDifferentIds_AreConsideredUnequal()
    {
        var a = await new CategoryBuilder().BuildAsync();
        var b = await new CategoryBuilder().BuildAsync();

        a.Equals(b).ShouldBeFalse();
        (a == b).ShouldBeFalse();
    }
}
