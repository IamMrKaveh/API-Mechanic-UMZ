using Application.Common.Events;
using Application.Review.Contracts;
using Application.Review.EventHandlers;
using Application.Review.Features.Shared;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Application.Review.EventHandlers;

public class UpdateProductStatsOnReviewDeletedHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ILogger<UpdateProductStatsOnReviewDeletedHandler> _logger =
        Substitute.For<ILogger<UpdateProductStatsOnReviewDeletedHandler>>();

    private readonly UpdateProductStatsOnReviewDeletedHandler _sut;

    public UpdateProductStatsOnReviewDeletedHandlerTests()
    {
        _sut = new UpdateProductStatsOnReviewDeletedHandler(
            _reviewQueryService,
            _productRepository,
            _unitOfWork,
            _logger);
    }

    private static ReviewDeletedEvent CreateEvent(ProductId productId, ReviewId? reviewId = null, UserId? userId = null) =>
        new(reviewId ?? ReviewId.NewId(), productId, userId ?? UserId.NewId());

    private static DomainEventNotification<ReviewDeletedEvent> Wrap(ReviewDeletedEvent evt) => new(evt);

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
    public async Task Handle_WhenSummaryHasValues_RecalculatesStatsToReflectRemainingReviews()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(4.5d, 10);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.4d, 9));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(4.4d);
        product.ReviewCount.ShouldBe(9);

        _productRepository.Received(1).Update(product, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSummaryIsNull_DefaultsAverageAndCountToZero()
    {
        // Arrange - deletion of the last review => summary returns null => reset to zero
        var product = new ProductBuilder().Build();
        product.RecalculateReviewStats(5d, 1);
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
        product.RecalculateReviewStats(4.2d, 3);
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 3.8d, 0));

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
            .Returns(Summary(product.Id.Value, 2.6789d, 15));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        product.AverageRating.ShouldBe(2.68d);
        product.ReviewCount.ShouldBe(15);
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
            .Returns(Summary(product.Id.Value, 3.5d, 4));

        // Act
        await _sut.Handle(Wrap(evt), token);

        // Assert
        await _productRepository.Received(1).GetByIdAsync(product.Id, token);
        await _reviewQueryService.Received(1).GetProductReviewSummaryAsync(product.Id, token);
        await _unitOfWork.Received(1).SaveChangesAsync(token);
    }

    [Fact]
    public async Task Handle_QueriesSummaryAndUpdatesProductInSingleUnitOfWork()
    {
        // Arrange
        var product = new ProductBuilder().Build();
        var evt = CreateEvent(product.Id);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(Summary(product.Id.Value, 4.0d, 5));

        // Act
        await _sut.Handle(Wrap(evt), CancellationToken.None);

        // Assert
        Received.InOrder(async () =>
        {
            await _productRepository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>());
            await _reviewQueryService.GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>());
            _productRepository.Update(product, Arg.Any<byte[]?>());
            await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }
}
