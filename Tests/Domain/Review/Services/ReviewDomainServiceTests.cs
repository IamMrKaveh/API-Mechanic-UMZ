using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Events;
using Domain.Review.Interfaces;
using Domain.Review.Services;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Domain.Review.Services;

public class ReviewDomainServiceTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>();
    private readonly IPurchaseVerificationService _purchaseVerificationService = Substitute.For<IPurchaseVerificationService>();

    private ReviewDomainService CreateSut() => new(_purchaseVerificationService, _reviewRepository);

    [Fact]
    public void Ctor_WithNullPurchaseVerificationService_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ReviewDomainService(null!, _reviewRepository));
    }

    [Fact]
    public void Ctor_WithNullReviewRepository_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new ReviewDomainService(_purchaseVerificationService, null!));
    }

    [Fact]
    public async Task SubmitReviewAsync_WithNullProductId_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Should.ThrowAsync<ArgumentNullException>(() => sut.SubmitReviewAsync(
            null!, UserId.NewId(), Rating.Create(4), "t", "c", null, false));
    }

    [Fact]
    public async Task SubmitReviewAsync_WithNullUserId_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Should.ThrowAsync<ArgumentNullException>(() => sut.SubmitReviewAsync(
            ProductId.NewId(), null!, Rating.Create(4), "t", "c", null, false));
    }

    [Fact]
    public async Task SubmitReviewAsync_WithNullRating_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        await Should.ThrowAsync<ArgumentNullException>(() => sut.SubmitReviewAsync(
            ProductId.NewId(), UserId.NewId(), null!, "t", "c", null, false));
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenUserAlreadyReviewed_ReturnsReviewAlreadyExistsFailure()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, orderId, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(4), "t", "c", orderId, requirePurchaseVerification: false);

        result.ShouldFailWith("Review.AlreadyExists");
        result.Error.Type.ShouldBe(ErrorType.Validation);
        await _purchaseVerificationService.DidNotReceiveWithAnyArgs()
            .UserHasPurchasedProductAsync(default!, default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRequirePurchaseVerificationAndUserDidNotPurchase_ReturnsReviewNotPurchasedFailure()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(4), "t", "c", null, requirePurchaseVerification: true);

        result.ShouldFailWith("Review.NotPurchased");
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenRequirePurchaseVerificationAndUserPurchased_ReturnsSuccessWithVerifiedPurchaseTrue()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(5), "t", "c", null, requirePurchaseVerification: true);

        result.ShouldBeSuccess();
        result.Value.IsVerifiedPurchase.ShouldBeTrue();
        result.Value.ProductId.ShouldBe(productId);
        result.Value.UserId.ShouldBe(userId);
        result.Value.Rating.Value.ShouldBe(5);
        await _purchaseVerificationService.Received(1)
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenNotRequiringVerificationAndUserPurchased_ReturnsSuccessWithVerifiedPurchaseTrue()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(3), "t", "c", null, requirePurchaseVerification: false);

        result.ShouldBeSuccess();
        result.Value.IsVerifiedPurchase.ShouldBeTrue();
    }

    [Fact]
    public async Task SubmitReviewAsync_WhenNotRequiringVerificationAndUserDidNotPurchase_ReturnsSuccessWithVerifiedPurchaseFalse()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(false);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(2), "t", "c", null, requirePurchaseVerification: false);

        result.ShouldBeSuccess();
        result.Value.IsVerifiedPurchase.ShouldBeFalse();
    }

    [Fact]
    public async Task SubmitReviewAsync_OnSuccess_PropagatesTitleCommentAndOrderIdIntoAggregate()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, orderId, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(4), "  عنوان  ", "  متن نظر  ", orderId, requirePurchaseVerification: true);

        result.ShouldBeSuccess();
        var review = result.Value;
        review.Title.ShouldBe("عنوان");
        review.Comment.ShouldBe("متن نظر");
        review.OrderId.ShouldBe(orderId);
        review.Status.ShouldBe(ReviewStatus.Pending);
    }

    [Fact]
    public async Task SubmitReviewAsync_OnSuccess_ReturnsAggregateWithReviewSubmittedEventQueued()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, null, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        var result = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(5), null, null, null, requirePurchaseVerification: true);

        result.ShouldBeSuccess();
        result.Value.DomainEvents.Count.ShouldBe(1);
        result.Value.DomainEvents.ShouldContain(e => e is ReviewSubmittedEvent);
        result.Value.Version.ShouldBe(1);
    }

    [Fact]
    public async Task SubmitReviewAsync_QueriesRepositoryWithExactTriple()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, orderId, Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(userId, productId, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        _ = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(4), "t", "c", orderId, requirePurchaseVerification: true);

        await _reviewRepository.Received(1)
            .UserHasReviewedProductAsync(userId, productId, orderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReviewAsync_DoesNotCallVerificationServiceWhenDuplicateFound()
    {
        var productId = ProductId.NewId();
        var userId = UserId.NewId();
        _reviewRepository
            .UserHasReviewedProductAsync(userId, productId, null, Arg.Any<CancellationToken>())
            .Returns(true);
        var sut = CreateSut();

        _ = await sut.SubmitReviewAsync(
            productId, userId, Rating.Create(4), null, null, null, requirePurchaseVerification: true);

        await _purchaseVerificationService.DidNotReceiveWithAnyArgs()
            .UserHasPurchasedProductAsync(default!, default!, Arg.Any<CancellationToken>());
    }
}
