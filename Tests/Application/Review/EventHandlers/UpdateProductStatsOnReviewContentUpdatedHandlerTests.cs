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

public class UpdateProductStatsOnReviewContentUpdatedHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<UpdateProductStatsOnReviewContentUpdatedHandler> _logger =
        Substitute.For<ILogger<UpdateProductStatsOnReviewContentUpdatedHandler>>();

    private readonly UpdateProductStatsOnReviewContentUpdatedHandler _sut;

    public UpdateProductStatsOnReviewContentUpdatedHandlerTests()
    {
        _sut = new UpdateProductStatsOnReviewContentUpdatedHandler(
            _reviewQueryService,
            _productRepository,
            _unitOfWork,
            _logger);
    }

    private static ReviewContentUpdatedEvent CreateEvent(ProductId productId, ReviewId? reviewId = null, int newRating = 5) =>
        new(reviewId ?? ReviewId.NewId(), productId, newRating);

    private static DomainEventNotification<ReviewContentUpdatedEvent> Wrap(ReviewContentUpdatedEvent evt) => new(evt);

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
    public async Task Handle_WhenSummaryHasValues_RecalculatesProductStatsFromSummary()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id, newRating: 5);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 3.75d, 8));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(3.75d);
        product.ReviewCount.ShouldBe(8);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSummaryIsNull_DefaultsAverageAndCountToZero()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.5d, 6);

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
            .Returns(Summary(product.Id.Value, 3.3333d, 3));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(3.33d);
        product.ReviewCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WhenSummaryReturnsZeroReviews_ForcesAverageToZero()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.0d, 2);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.9d, 0));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(0d);
        product.ReviewCount.ShouldBe(0);
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
            .Returns(Summary(product.Id.Value, 4.0d, 4));

        // Act
        await _sut.Handle(Wrap(evt), token);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(product.Id, token);
        await _reviewQueryService.Received(1).GetProductReviewSummaryAsync(product.Id, token);
        await _unitOfWork.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task Handle_UsesProductIdFromEventWhenQueryingSummary()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.2d, 5));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        await _reviewQueryService.Received(1).GetProductReviewSummaryAsync(
            Arg.Is<ProductId>(p => p == evt.ProductId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStatsUnchanged_StillCallsUpdateAndSaveChanges()
    {
        // Arrange - handler always invokes Update + SaveChanges even when Recalculate is a no-op
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.5d, 10);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.5d, 10));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(4.5d);
        product.ReviewCount.ShouldBe(10);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
