using Application.Common.Interfaces;
using Application.Review.Configuration;
using Application.Review.Features.Queries.CanReviewProduct;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.CanReviewProduct;

public class CanReviewProductHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly IPurchaseVerificationService _purchaseVerificationService = Substitute.For<IPurchaseVerificationService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private CanReviewProductHandler CreateSut(bool requirePurchaseVerification = false)
        => new(
            _reviewRepository,
            _purchaseVerificationService,
            _currentUser,
            Options.Create(new ReviewSettings { RequirePurchaseVerification = requirePurchaseVerification }));

    [Fact]
    public async Task Handle_WhenAnonymous_ReturnsSuccessWithCanReviewFalseAndLoginPrompt()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = CreateSut();

        var result = await sut.Handle(
            new CanReviewProductQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanReview.ShouldBeFalse();
        result.Value.HasReviewed.ShouldBeFalse();
        result.Value.HasPurchased.ShouldBeFalse();
        result.Value.Reason.ShouldNotBeNullOrWhiteSpace();

        await _reviewRepository.DidNotReceiveWithAnyArgs()
            .UserHasReviewedProductAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyReviewed_ReturnsCanReviewFalseWithReason()
    {
        var userGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userGuid);

        _reviewRepository
            .UserHasReviewedProductAsync(
                Arg.Any<UserId>(),
                Arg.Any<ProductId>(),
                Arg.Any<OrderId?>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(
                Arg.Any<UserId>(),
                Arg.Any<ProductId>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var sut = CreateSut();

        var result = await sut.Handle(
            new CanReviewProductQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanReview.ShouldBeFalse();
        result.Value.HasReviewed.ShouldBeTrue();
        result.Value.HasPurchased.ShouldBeTrue();
        result.Value.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WhenPurchaseVerificationRequiredAndUserHasNotPurchased_ReturnsCanReviewFalse()
    {
        var userGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userGuid);

        _reviewRepository
            .UserHasReviewedProductAsync(
                Arg.Any<UserId>(),
                Arg.Any<ProductId>(),
                Arg.Any<OrderId?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(
                Arg.Any<UserId>(),
                Arg.Any<ProductId>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut(requirePurchaseVerification: true);

        var result = await sut.Handle(
            new CanReviewProductQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanReview.ShouldBeFalse();
        result.Value.HasReviewed.ShouldBeFalse();
        result.Value.HasPurchased.ShouldBeFalse();
        result.Value.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Handle_WhenAuthenticatedNotReviewedAndPurchaseNotRequired_ReturnsCanReviewTrue()
    {
        var userGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userGuid);

        _reviewRepository
            .UserHasReviewedProductAsync(
                Arg.Any<UserId>(),
                Arg.Any<ProductId>(),
                Arg.Any<OrderId?>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _purchaseVerificationService
            .UserHasPurchasedProductAsync(
                Arg.Any<UserId>(),
                Arg.Any<ProductId>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut(requirePurchaseVerification: false);

        var result = await sut.Handle(
            new CanReviewProductQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.CanReview.ShouldBeTrue();
        result.Value.HasReviewed.ShouldBeFalse();
        result.Value.Reason.ShouldBeNull();
    }
}
