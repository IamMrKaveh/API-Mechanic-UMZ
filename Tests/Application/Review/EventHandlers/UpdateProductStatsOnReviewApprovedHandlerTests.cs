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

public class UpdateProductStatsOnReviewApprovedHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<UpdateProductStatsOnReviewApprovedHandler> _logger =
        Substitute.For<ILogger<UpdateProductStatsOnReviewApprovedHandler>>();

    private readonly UpdateProductStatsOnReviewApprovedHandler _sut;

    public UpdateProductStatsOnReviewApprovedHandlerTests()
    {
        _sut = new UpdateProductStatsOnReviewApprovedHandler(
            _reviewQueryService,
            _productRepository,
            _unitOfWork,
            _logger);
    }

    private static ReviewApprovedEvent CreateEvent(ProductId productId, ReviewId? reviewId = null, int rating = 4) =>
        new(reviewId ?? ReviewId.NewId(), productId, Rating.Create(rating));

    private static DomainEventNotification<ReviewApprovedEvent> Wrap(ReviewApprovedEvent evt) => new(evt);

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
    public async Task Handle_WhenProductExistsAndSummaryHasValues_RecalculatesWithSummaryValues()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);

        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.4d, 10));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(4.4d);
        product.ReviewCount.ShouldBe(10);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSummaryIsNull_RecalculatesWithZeroAverageAndCount()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(3.5d, 5);

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
            .Returns(Summary(product.Id.Value, 4.2567d, 42));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(4.26d);
        product.ReviewCount.ShouldBe(42);
    }

    [Fact]
    public async Task Handle_WhenSummaryReturnsZeroReviews_ForcesAverageRatingToZero()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.2d, 3);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.7d, 0));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.ReviewCount.ShouldBe(0);
        product.AverageRating.ShouldBe(0d);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
            .Returns(Summary(product.Id.Value, 3.0d, 2));

        // Act
        await _sut.Handle(Wrap(evt), token);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(product.Id, token);
        await _reviewQueryService.Received(1).GetProductReviewSummaryAsync(product.Id, token);
        await _unitOfWork.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task Handle_UsesProductIdFromEventWhenFetchingProduct()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 5d, 1));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(
            Arg.Is<ProductId>(p => p == evt.ProductId),
            Arg.Any<CancellationToken>());
        await _reviewQueryService.Received(1).GetProductReviewSummaryAsync(
            Arg.Is<ProductId>(p => p == evt.ProductId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotRaiseDomainEventsOnProductWhenRecalculating()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.ClearDomainEvents();
        var versionBefore = product.Version;
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.2d, 7));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.DomainEvents.ShouldBeEmpty();
        product.Version.ShouldBe(versionBefore);
    }
}
