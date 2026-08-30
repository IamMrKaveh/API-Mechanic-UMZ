using Application.Review.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Entities;
using Domain.Review.Enums;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Review.QueryServices;
using Tests.TestInfrastructure.Stubs;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;
using Products = Domain.Product.Aggregates.Product;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Review.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ReviewQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private ReviewQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new ReviewQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> SeedUserAsync(bool isActive = true)
    {
        var user = new UserBuilder().Build();
        if (!isActive)
            user.Deactivate();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Products> SeedProductAsync()
    {
        var catName = $"Cat-{Guid.NewGuid():N}"[..20];
        var category = await Categories.Create(
            CategoryId.NewId(), CategoryName.Create(catName), CategorySlug.GenerateFrom(catName),
            new StubCategoryUniquenessChecker(), null, 0, CancellationToken.None);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var brandName = $"Brand-{Guid.NewGuid():N}"[..20];
        var brand = await Brands.Create(
            BrandName.Create(brandName), BrandSlug.GenerateFrom(brandName), category.Id,
            new StubBrandUniquenessChecker(), null, null, CancellationToken.None);
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var product = new ProductBuilder().WithBrandId(brand.Id).WithCategoryId(category.Id).Build();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    private async Task<ProductReview> SeedReviewAsync(
        Products product,
        Users user,
        int rating = 4,
        ReviewStatus? status = null,
        bool isVerified = true,
        string? title = "عنوان",
        string? comment = "متن نظر")
    {
        var review = new ProductReviewBuilder()
            .WithProductId(product.Id)
            .WithUserId(user.Id)
            .WithRating(rating)
            .WithTitle(title)
            .WithComment(comment)
            .WithVerifiedPurchase(isVerified)
            .WithoutOrderId()
            .Build();

        if (status is not null)
        {
            if (status == ReviewStatus.Approved) review.Approve();
            else if (status == ReviewStatus.Rejected) review.Reject("دلیل رد");
        }

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    private async Task SeedVoteAsync(ReviewId reviewId, UserId userId, VoteType type)
    {
        var vote = ReviewVote.Create(reviewId, userId, type);
        _context.Set<ReviewVote>().Add(vote);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetApprovedProductReviewsAsync_WithNoReviews_ReturnsEmpty()
    {
        var result = await _sut.GetApprovedProductReviewsAsync(
            ProductId.NewId(), page: 1, pageSize: 10, sortBy: "Newest",
            minRating: null, verifiedOnly: false, currentUserId: null);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetApprovedProductReviewsAsync_ReturnsOnlyApprovedReviewsForActiveUsers()
    {
        var product = await SeedProductAsync();
        var activeUser = await SeedUserAsync();
        var inactiveUser = await SeedUserAsync(isActive: false);

        await SeedReviewAsync(product, activeUser, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, activeUser, status: ReviewStatus.Pending);
        await SeedReviewAsync(product, activeUser, status: ReviewStatus.Rejected);
        await SeedReviewAsync(product, inactiveUser, status: ReviewStatus.Approved);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: 1, pageSize: 10, sortBy: "Newest",
            minRating: null, verifiedOnly: false, currentUserId: null);

        result.TotalCount.ShouldBe(1);
        result.Items.ShouldAllBe(r => r.Status == "Approved");
    }

    [Fact]
    public async Task GetApprovedProductReviewsAsync_FiltersByMinRating()
    {
        var product = await SeedProductAsync();
        var user = await SeedUserAsync();

        await SeedReviewAsync(product, user, rating: 2, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 3, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 5, status: ReviewStatus.Approved);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: 1, pageSize: 10, sortBy: "Newest",
            minRating: 3, verifiedOnly: false, currentUserId: null);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(r => r.Rating >= 3);
    }

    [Fact]
    public async Task GetApprovedProductReviewsAsync_VerifiedOnly_FiltersOutUnverified()
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), isVerified: true, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), isVerified: false, status: ReviewStatus.Approved);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: 1, pageSize: 10, sortBy: "Newest",
            minRating: null, verifiedOnly: true, currentUserId: null);

        result.TotalCount.ShouldBe(1);
        result.Items[0].IsVerifiedPurchase.ShouldBeTrue();
    }

    [Theory]
    [InlineData("HighestRated")]
    [InlineData("LowestRated")]
    [InlineData("Newest")]
    public async Task GetApprovedProductReviewsAsync_SortsAsSpecified(string sortBy)
    {
        var product = await SeedProductAsync();
        var r1 = await SeedReviewAsync(product, await SeedUserAsync(), rating: 3, status: ReviewStatus.Approved);
        await Task.Delay(20);
        var r2 = await SeedReviewAsync(product, await SeedUserAsync(), rating: 5, status: ReviewStatus.Approved);
        await Task.Delay(20);
        var r3 = await SeedReviewAsync(product, await SeedUserAsync(), rating: 1, status: ReviewStatus.Approved);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: 1, pageSize: 10, sortBy: sortBy,
            minRating: null, verifiedOnly: false, currentUserId: null);

        result.Items.Count.ShouldBe(3);
        switch (sortBy)
        {
            case "HighestRated":
                result.Items[0].Rating.ShouldBe(5);
                result.Items[^1].Rating.ShouldBe(1);
                break;

            case "LowestRated":
                result.Items[0].Rating.ShouldBe(1);
                result.Items[^1].Rating.ShouldBe(5);
                break;

            case "Newest":
                result.Items[0].Id.ShouldBe(r3.Id.Value);
                break;
        }
    }

    [Fact]
    public async Task GetApprovedProductReviewsAsync_WithCurrentUserVote_PopulatesUserVoteField()
    {
        var product = await SeedProductAsync();
        var author = await SeedUserAsync();
        var voter = await SeedUserAsync();

        var review = await SeedReviewAsync(product, author, status: ReviewStatus.Approved);
        await SeedVoteAsync(review.Id, voter.Id, VoteType.Like);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: 1, pageSize: 10, sortBy: "Newest",
            minRating: null, verifiedOnly: false, currentUserId: voter.Id);

        result.Items.Count.ShouldBe(1);
        result.Items[0].UserVote.ShouldBe("Like");
    }

    [Fact]
    public async Task GetApprovedProductReviewsAsync_WithoutCurrentUser_LeavesUserVoteNull()
    {
        var product = await SeedProductAsync();
        var author = await SeedUserAsync();
        var review = await SeedReviewAsync(product, author, status: ReviewStatus.Approved);
        await SeedVoteAsync(review.Id, (await SeedUserAsync()).Id, VoteType.Like);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: 1, pageSize: 10, sortBy: "Newest",
            minRating: null, verifiedOnly: false, currentUserId: null);

        result.Items[0].UserVote.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, -5)]
    public async Task GetApprovedProductReviewsAsync_NormalizesInvalidPaginationValues(int page, int pageSize)
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);

        var result = await _sut.GetApprovedProductReviewsAsync(
            product.Id, page: page, pageSize: pageSize, sortBy: "Newest",
            minRating: null, verifiedOnly: false, currentUserId: null);

        result.Page.ShouldBeGreaterThanOrEqualTo(1);
        result.PageSize.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetProductReviewSummaryAsync_WithNoApprovedReviews_ReturnsNull()
    {
        var product = await SeedProductAsync();

        var result = await _sut.GetProductReviewSummaryAsync(product.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetProductReviewSummaryAsync_ReturnsCorrectStatsAndDistribution()
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 5, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 5, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 4, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 3, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 1, status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 2, status: ReviewStatus.Pending);

        var result = await _sut.GetProductReviewSummaryAsync(product.Id);

        result.ShouldNotBeNull();
        result.ProductId.ShouldBe(product.Id.Value);
        result.TotalReviews.ShouldBe(5);
        result.TotalCount.ShouldBe(5);
        result.FiveStarCount.ShouldBe(2);
        result.FourStarCount.ShouldBe(1);
        result.ThreeStarCount.ShouldBe(1);
        result.TwoStarCount.ShouldBe(0);
        result.OneStarCount.ShouldBe(1);
        result.AverageRating.ShouldBe(Math.Round((5 + 5 + 4 + 3 + 1) / 5d, 2));
        result.RatingDistribution[5].ShouldBe(2);
        result.RatingDistribution[4].ShouldBe(1);
        result.RatingDistribution[3].ShouldBe(1);
        result.RatingDistribution[2].ShouldBe(0);
        result.RatingDistribution[1].ShouldBe(1);
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_WithStatusAll_ReturnsAllNonDeletedReviews()
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Pending);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Rejected);

        var filter = new AdminReviewFilter(
            Status: "All", Page: 1, PageSize: 10, SearchText: null,
            MinRating: null, ProductId: null, DateFrom: null, DateTo: null);

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_FiltersByStatus()
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Pending);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);

        var filter = new AdminReviewFilter(
            Status: "Approved", Page: 1, PageSize: 10, SearchText: null,
            MinRating: null, ProductId: null, DateFrom: null, DateTo: null);

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(r => r.Status == "Approved");
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_FiltersByProductId()
    {
        var productA = await SeedProductAsync();
        var productB = await SeedProductAsync();
        await SeedReviewAsync(productA, await SeedUserAsync(), status: ReviewStatus.Pending);
        await SeedReviewAsync(productB, await SeedUserAsync(), status: ReviewStatus.Pending);

        var filter = new AdminReviewFilter(
            Status: "All", Page: 1, PageSize: 10, SearchText: null,
            MinRating: null, ProductId: productA.Id.Value, DateFrom: null, DateTo: null);

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].ProductId.ShouldBe(productA.Id.Value);
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_FiltersByMinRating()
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 2);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 4);
        await SeedReviewAsync(product, await SeedUserAsync(), rating: 5);

        var filter = new AdminReviewFilter(
            Status: "All", Page: 1, PageSize: 10, SearchText: null,
            MinRating: 4, ProductId: null, DateFrom: null, DateTo: null);

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(r => r.Rating >= 4);
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_FiltersByDateRange()
    {
        var product = await SeedProductAsync();
        var older = await SeedReviewAsync(product, await SeedUserAsync());
        await Task.Delay(30);
        var mid = await SeedReviewAsync(product, await SeedUserAsync());
        await Task.Delay(30);
        var newer = await SeedReviewAsync(product, await SeedUserAsync());

        var filter = new AdminReviewFilter(
            Status: "All", Page: 1, PageSize: 10, SearchText: null,
            MinRating: null, ProductId: null,
            DateFrom: mid.CreatedAt.AddMilliseconds(-1),
            DateTo: mid.CreatedAt.AddMilliseconds(1));

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(mid.Id.Value);
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_OrdersByCreatedAtDescending()
    {
        var product = await SeedProductAsync();
        var r1 = await SeedReviewAsync(product, await SeedUserAsync());
        await Task.Delay(20);
        var r2 = await SeedReviewAsync(product, await SeedUserAsync());
        await Task.Delay(20);
        var r3 = await SeedReviewAsync(product, await SeedUserAsync());

        var filter = new AdminReviewFilter(
            Status: "All", Page: 1, PageSize: 10, SearchText: null,
            MinRating: null, ProductId: null, DateFrom: null, DateTo: null);

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.Items.Select(x => x.Id).ToList()
            .ShouldBe(new[] { r3.Id.Value, r2.Id.Value, r1.Id.Value });
    }

    [Fact]
    public async Task GetReviewsByStatusAsync_ExcludesDeletedReviews()
    {
        var product = await SeedProductAsync();
        var kept = await SeedReviewAsync(product, await SeedUserAsync());
        var deleted = await SeedReviewAsync(product, await SeedUserAsync());
        deleted.MarkAsDeleted();
        await _context.SaveChangesAsync();

        var filter = new AdminReviewFilter(
            Status: "All", Page: 1, PageSize: 10, SearchText: null,
            MinRating: null, ProductId: null, DateFrom: null, DateTo: null);

        var result = await _sut.GetReviewsByStatusAsync(filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(kept.Id.Value);
    }

    [Fact]
    public async Task GetAdminReviewStatsAsync_WithNoReviews_ReturnsZerosForAll()
    {
        var result = await _sut.GetAdminReviewStatsAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result.Pending.ShouldBe(0);
        result.Approved.ShouldBe(0);
        result.Rejected.ShouldBe(0);
        result.Total.ShouldBe(0);
    }

    [Fact]
    public async Task GetAdminReviewStatsAsync_CountsReviewsByEachStatus()
    {
        var product = await SeedProductAsync();
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Pending);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Pending);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);
        await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Rejected);

        var result = await _sut.GetAdminReviewStatsAsync(CancellationToken.None);

        result.Pending.ShouldBe(2);
        result.Approved.ShouldBe(1);
        result.Rejected.ShouldBe(1);
        result.Total.ShouldBe(4);
    }

    [Fact]
    public async Task GetAdminReviewStatsAsync_ExcludesDeletedReviews()
    {
        var product = await SeedProductAsync();
        var kept = await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);
        var deleted = await SeedReviewAsync(product, await SeedUserAsync(), status: ReviewStatus.Approved);
        deleted.MarkAsDeleted();
        await _context.SaveChangesAsync();

        var result = await _sut.GetAdminReviewStatsAsync(CancellationToken.None);

        result.Approved.ShouldBe(1);
        result.Total.ShouldBe(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(ReviewId.NewId(), currentUserId: null, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedReview_ReturnsNull()
    {
        var product = await SeedProductAsync();
        var review = await SeedReviewAsync(product, await SeedUserAsync());
        review.MarkAsDeleted();
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(review.Id, currentUserId: null, CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingReview_ReturnsDto()
    {
        var product = await SeedProductAsync();
        var user = await SeedUserAsync();
        var review = await SeedReviewAsync(product, user, rating: 4, status: ReviewStatus.Approved);

        var result = await _sut.GetByIdAsync(review.Id, currentUserId: null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(review.Id.Value);
        result.ProductId.ShouldBe(product.Id.Value);
        result.UserId.ShouldBe(user.Id.Value);
        result.Rating.ShouldBe(4);
        result.Status.ShouldBe("Approved");
    }

    [Fact]
    public async Task GetByIdAsync_WithCurrentUserVote_ReturnsUserVoteInDto()
    {
        var product = await SeedProductAsync();
        var author = await SeedUserAsync();
        var voter = await SeedUserAsync();
        var review = await SeedReviewAsync(product, author, status: ReviewStatus.Approved);
        await SeedVoteAsync(review.Id, voter.Id, VoteType.Dislike);

        var result = await _sut.GetByIdAsync(review.Id, currentUserId: voter.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result.UserVote.ShouldBe("Dislike");
    }

    [Fact]
    public async Task GetUserReviewsAsync_ReturnsOnlyReviewsBelongingToUser()
    {
        var product = await SeedProductAsync();
        var userA = await SeedUserAsync();
        var userB = await SeedUserAsync();
        await SeedReviewAsync(product, userA);
        await SeedReviewAsync(product, userA);
        await SeedReviewAsync(product, userB);

        var result = await _sut.GetUserReviewsAsync(userA.Id, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(r => r.UserId == userA.Id.Value);
    }

    [Fact]
    public async Task GetUserReviewsAsync_ExcludesDeletedReviews()
    {
        var product = await SeedProductAsync();
        var user = await SeedUserAsync();
        var kept = await SeedReviewAsync(product, user);
        var deleted = await SeedReviewAsync(product, user);
        deleted.MarkAsDeleted();
        await _context.SaveChangesAsync();

        var result = await _sut.GetUserReviewsAsync(user.Id, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(kept.Id.Value);
    }

    [Fact]
    public async Task GetUserReviewsAsync_OrdersByCreatedAtDescending()
    {
        var product = await SeedProductAsync();
        var user = await SeedUserAsync();
        var r1 = await SeedReviewAsync(product, user);
        await Task.Delay(20);
        var r2 = await SeedReviewAsync(product, user);

        var result = await _sut.GetUserReviewsAsync(user.Id, page: 1, pageSize: 10, CancellationToken.None);

        result.Items.Select(x => x.Id).ToList().ShouldBe(new[] { r2.Id.Value, r1.Id.Value });
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-2, 0)]
    public async Task GetUserReviewsAsync_NormalizesInvalidPagination(int page, int pageSize)
    {
        var user = await SeedUserAsync();

        var result = await _sut.GetUserReviewsAsync(user.Id, page: page, pageSize: pageSize, CancellationToken.None);

        result.Page.ShouldBeGreaterThanOrEqualTo(1);
        result.PageSize.ShouldBeGreaterThan(0);
    }
}
