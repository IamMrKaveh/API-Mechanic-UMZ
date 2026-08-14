using Application.Common.Interfaces;
using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Queries.CheckWishlistStatus;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wishlist.Features.Queries.CheckWishlistStatus;

public class CheckWishlistStatusHandlerTests
{
    private readonly IWishlistQueryService _wishlistQueryService = Substitute.For<IWishlistQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly CheckWishlistStatusHandler _sut;

    public CheckWishlistStatusHandlerTests()
    {
        _sut = new CheckWishlistStatusHandler(_wishlistQueryService, _currentUserService);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenQueryServiceReturnsStatus_ReturnsSuccessWithStatus(bool isInWishlist)
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)userGuid);
        _wishlistQueryService
            .IsInWishlistAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(isInWishlist);

        var result = await _sut.Handle(
            new CheckWishlistStatusQuery(productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(isInWishlist);
    }

    [Fact]
    public async Task Handle_WhenCalled_QueriesWithCurrentUserAndProductIds()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)userGuid);
        _wishlistQueryService
            .IsInWishlistAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new CheckWishlistStatusQuery(productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistQueryService.Received(1).IsInWishlistAsync(
            Arg.Is<UserId>(u => u == UserId.From(userGuid)),
            Arg.Is<ProductId>(p => p == ProductId.From(productGuid)),
            Arg.Any<CancellationToken>());
    }
}
