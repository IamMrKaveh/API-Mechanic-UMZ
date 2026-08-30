using Application.Common.Events;
using Application.Review.Contracts;
using Application.Review.EventHandlers;
using Application.Review.Features.Shared;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Application.Review.EventHandlers;

public class UpdateProductStatsOnReviewRejectedHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<UpdateProductStatsOnReviewRejectedHandler> _logger =
        Substitute.For<ILogger<UpdateProductStatsOnReviewRejectedHandler>>();

    private readonly UpdateProductStatsOnReviewRejectedHandler _sut;

    public UpdateProductStatsOnReviewRejectedHandlerTests()
    {
        _sut = new UpdateProductStatsOnReviewRejectedHandler(
            _reviewQueryService,
            _productRepository,
            _unitOfWork,
            _logger);
    }

    private static ReviewRejectedEvent CreateEvent(
        ProductId productId,
        ReviewId? reviewId = null,
        string? reason = "inappropriate content") =>
        new(reviewId ?? ReviewId.NewId(), productId, reason);

    private static DomainEventNotification<ReviewRejectedEvent> Wrap(ReviewRejectedEvent evt) => new(evt);

    private static ReviewSummaryDto Summary(Guid productId, double avg, int total) => new()
    {
        ProductId = productId,
        AverageRating = avg,
        TotalReviews = total,
        TotalCount = total
    };

    [Fact]
    public async Task Handle_WhenProductNotFound_LogsWarningAndReturnsWithoutRecalculating()
    {
        // Arrange
        var evt = CreateEvent(ProductId.NewId());
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((Products?)null);

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(evt.ProductId, Arg.Any<CancellationToken>());
        await _reviewQueryService.DidNotReceiveWithAnyArgs().GetProductReviewSummaryAsync(default!, default);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenSummaryHasValues_RecalculatesStatsExcludingRejectedReview()
    {
        // Arrange - after rejection the summary only counts approved reviews
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.5d, 8);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.6d, 7));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(4.6d);
        product.ReviewCount.ShouldBe(7);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSummaryIsNull_ResetsAverageAndCountToZero()
    {
        // Arrange - rejecting the only approved review leaves no approved reviews
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.0d, 1);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns((ReviewSummaryDto?)null);

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(0d);
        product.ReviewCount.ShouldBe(0);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSummaryReturnsZeroReviews_ForcesAverageToZero()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.9d, 2);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.3d, 0));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(0d);
        product.ReviewCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_RoundsAverageRatingToTwoDecimalPlaces()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 3.14159d, 12));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(3.14d);
        product.ReviewCount.ShouldBe(12);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToDependencies()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepository
            .GetByIdAsync(product.Id, token)
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, token)
            .Returns(Summary(product.Id.Value, 4.2d, 6));

        // Act
        await _sut.Handle(Wrap(evt), token);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(product.Id, token);
        await _reviewQueryService.Received(1).GetProductReviewSummaryAsync(product.Id, token);
        await _unitOfWork.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task Handle_HandlesNullReasonInEvent()
    {
        // Arrange - Reason may be null since ReviewRejectedEvent.Reason is nullable
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id, reason: null);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.0d, 3));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        evt.Reason.ShouldBeNull();
        product.AverageRating.ShouldBe(4.0d);
        product.ReviewCount.ShouldBe(3);
        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesProductIdFromEventWhenLookingUpProduct()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.0d, 3));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(
            Arg.Is<ProductId>(p => p == evt.ProductId),
            Arg.Any<CancellationToken>());
    }
}
