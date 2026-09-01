using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Infrastructure.Review.Repositories;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;
using Products = Domain.Product.Aggregates.Product;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Review.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ReviewRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IReviewRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new ReviewRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Categories> PersistCategoryAsync()
    {
        var catName = $"Cat-{Guid.NewGuid():N}"[..20];
        var category = await Categories.Create(
            CategoryId.NewId(),
            CategoryName.Create(catName),
            CategorySlug.GenerateFrom(catName),
            new StubCategoryUniquenessChecker(),
            null,
            0,
            CancellationToken.None);

        category.ClearDomainEvents();
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
        return category;
    }

    private async Task<Brands> PersistBrandAsync(CategoryId categoryId)
    {
        var brandName = $"Brand-{Guid.NewGuid():N}"[..20];
        var brand = await Brands.Create(
            BrandName.Create(brandName),
            BrandSlug.GenerateFrom(brandName),
            categoryId,
            new StubBrandUniquenessChecker(),
            null,
            null,
            CancellationToken.None);

        brand.ClearDomainEvents();
        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();
        return brand;
    }

    private async Task<Products> PersistProductAsync()
    {
        var category = await PersistCategoryAsync();
        var brand = await PersistBrandAsync(category.Id);

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        product.ClearDomainEvents();
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return product;
    }

    private async Task<Users> PersistUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<(Users user, Products product)> PersistUserAndProductAsync()
    {
        var user = await PersistUserAsync();
        var product = await PersistProductAsync();
        _context.ChangeTracker.Clear();
        return (user, product);
    }

    [Fact]
    public async Task AddAsync_ValidReview_PersistsAcrossContexts()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithRating(5)
            .WithTitle("عنوان تست")
            .WithComment("متن نظر تست")
            .WithoutOrderId()
            .WithVerifiedPurchase(false)
            .Build();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new ReviewRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(review.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(review.Id);
        loaded.UserId.ShouldBe(user.Id);
        loaded.ProductId.ShouldBe(product.Id);
        loaded.Rating.Value.ShouldBe(5);
        loaded.Title.ShouldBe("عنوان تست");
        loaded.Comment.ShouldBe("متن نظر تست");
        loaded.Status.ShouldBe(ReviewStatus.Pending);
        loaded.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenReviewDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(ReviewId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenReviewIsSoftDeleted_ReturnsNull()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.MarkAsDeleted();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(review.Id);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdIncludingDeletedAsync_WhenSoftDeleted_ReturnsReview()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.MarkAsDeleted();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdIncludingDeletedAsync(review.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(review.Id);
        loaded.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task UserHasReviewedProductAsync_WhenReviewExists_ReturnsTrue()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.UserHasReviewedProductAsync(user.Id, product.Id, null, CancellationToken.None);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task UserHasReviewedProductAsync_WhenNoReviewExists_ReturnsFalse()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var exists = await _sut.UserHasReviewedProductAsync(user.Id, product.Id, null, CancellationToken.None);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasReviewedProductAsync_WhenReviewIsSoftDeleted_ReturnsFalse()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.MarkAsDeleted();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.UserHasReviewedProductAsync(user.Id, product.Id, null, CancellationToken.None);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByUserAndProductAsync_WhenReviewExists_ReturnsIt()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserAndProductAsync(user.Id, product.Id, null);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(review.Id);
    }

    [Fact]
    public async Task ListByUserAsync_ReturnsOnlyReviewsForThatUserOrderedByCreatedAtDescending()
    {
        var (userA, productA) = await PersistUserAndProductAsync();
        var (userB, productB) = await PersistUserAndProductAsync();

        var first = new ProductReviewBuilder()
            .WithUserId(userA.Id)
            .WithProductId(productA.Id)
            .WithoutOrderId()
            .WithTitle("first")
            .Build();
        first.ClearDomainEvents();
        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();

        await Task.Delay(20);

        var second = new ProductReviewBuilder()
            .WithUserId(userA.Id)
            .WithProductId(productB.Id)
            .WithoutOrderId()
            .WithTitle("second")
            .Build();
        second.ClearDomainEvents();
        await _sut.AddAsync(second);

        var otherUserReview = new ProductReviewBuilder()
            .WithUserId(userB.Id)
            .WithProductId(productA.Id)
            .WithoutOrderId()
            .Build();
        otherUserReview.ClearDomainEvents();
        await _sut.AddAsync(otherUserReview);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.ListByUserAsync(userA.Id);

        results.Count.ShouldBe(2);
        results.ShouldContain(r => r.Id == first.Id);
        results.ShouldContain(r => r.Id == second.Id);
        results.ShouldNotContain(r => r.Id == otherUserReview.Id);
        results[0].CreatedAt.ShouldBeGreaterThanOrEqualTo(results[1].CreatedAt);
    }

    [Fact]
    public async Task ListByProductAsync_ReturnsOnlyNonDeletedReviewsForThatProduct()
    {
        var (userA, product) = await PersistUserAndProductAsync();
        var userB = await PersistUserAsync();
        _context.ChangeTracker.Clear();

        var visible = new ProductReviewBuilder()
            .WithUserId(userA.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        visible.ClearDomainEvents();

        var deleted = new ProductReviewBuilder()
            .WithUserId(userB.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        deleted.MarkAsDeleted();
        deleted.ClearDomainEvents();

        await _sut.AddAsync(visible);
        await _sut.AddAsync(deleted);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.ListByProductAsync(product.Id);

        results.ShouldContain(r => r.Id == visible.Id);
        results.ShouldNotContain(r => r.Id == deleted.Id);
    }

    [Fact]
    public async Task Update_AfterApprove_PersistsApprovedStatus()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(review.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Approve();
        reloaded.ClearDomainEvents();
        _sut.Update(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new ReviewRepository(freshContext);
        var final = await freshRepo.GetByIdAsync(review.Id);

        final.ShouldNotBeNull();
        final!.Status.ShouldBe(ReviewStatus.Approved);
    }

    [Fact]
    public async Task Remove_ExistingReview_DeletesFromDatabase()
    {
        var (user, product) = await PersistUserAndProductAsync();

        var review = new ProductReviewBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .WithoutOrderId()
            .Build();
        review.ClearDomainEvents();

        await _sut.AddAsync(review);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove = await _sut.GetByIdIncludingDeletedAsync(review.Id);
        toRemove.ShouldNotBeNull();
        _sut.Remove(toRemove!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        (await _sut.GetByIdIncludingDeletedAsync(review.Id)).ShouldBeNull();
    }
}
