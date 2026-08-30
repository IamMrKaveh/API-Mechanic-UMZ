using Application.Common.Events;
using Application.Review.Contracts;
using Application.Review.EventHandlers;
using Application.Review.Features.Shared;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Application.Product.EventHandlers;

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

    private static DomainEventNotification<ReviewApprovedEvent> BuildNotification(ProductId productId)
    {
        var evt = new ReviewApprovedEvent(
            ReviewId.NewId(),
            productId,
            new RatingBuilder().Build());
        return new DomainEventNotification<ReviewApprovedEvent>(evt);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_DoesNotQuerySummaryOrPersist()
    {
        var productId = ProductId.NewId();
        _productRepository
            .GetByIdAsync(productId, Arg.Any<CancellationToken>())
            .Returns((Products?)null);

        await _sut.Handle(BuildNotification(productId), CancellationToken.None);

        await _reviewQueryService.DidNotReceiveWithAnyArgs()
            .GetProductReviewSummaryAsync(default!, default);
        _productRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenSummaryIsNull_ResetsStatsToZeroAndSaves()
    {
        var product = new ProductBuilder().Build();
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((ReviewSummaryDto?)null);

        await _sut.Handle(BuildNotification(product.Id), CancellationToken.None);

        product.AverageRating.ShouldBe(0d);
        product.ReviewCount.ShouldBe(0);
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSummaryPresent_ApplyRoundedAverageAndTotalReviews()
    {
        var product = new ProductBuilder().Build();
        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);

        var summary = new ReviewSummaryDto
        {
            ProductId = product.Id.Value,
            AverageRating = 4.237d,
            TotalReviews = 12,
            TotalCount = 12
        };
        _reviewQueryService
            .GetProductReviewSummaryAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(summary);

        await _sut.Handle(BuildNotification(product.Id), CancellationToken.None);

        product.AverageRating.ShouldBe(4.24d);
        product.ReviewCount.ShouldBe(12);
        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToUnitOfWork()
    {
        using var cts = new CancellationTokenSource();
        var product = new ProductBuilder().Build();

        _productRepository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(product);
        _reviewQueryService
            .GetProductReviewSummaryAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto
            {
                ProductId = product.Id.Value,
                AverageRating = 3.0d,
                TotalReviews = 1,
                TotalCount = 1
            });

        await _sut.Handle(BuildNotification(product.Id), cts.Token);

        await _unitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }
}
