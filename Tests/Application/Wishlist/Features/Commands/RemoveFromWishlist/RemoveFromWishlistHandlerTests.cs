using Application.Common.Interfaces;
using Application.Wishlist.Features.Commands.RemoveFromWishlist;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Wishlists = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Application.Wishlist.Features.Commands.RemoveFromWishlist;

public class RemoveFromWishlistHandlerTests
{
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly RemoveFromWishlistHandler _sut;

    public RemoveFromWishlistHandlerTests()
    {
        _sut = new RemoveFromWishlistHandler(_wishlistRepository, _unitOfWork, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsSuccessWithoutRemovingOrSaving()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)userGuid);
        _wishlistRepository
            .GetByUserAndProductAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((Wishlists?)null);

        var result = await _sut.Handle(
            new RemoveFromWishlistCommand(productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistRepository.DidNotReceive().RemoveAsync(
            Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemExists_RemovesItemAndSavesAndReturnsSuccess()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();
        var existing = new WishlistBuilder()
            .WithUserId(UserId.From(userGuid))
            .WithProductId(ProductId.From(productGuid))
            .Build();

        _currentUserService.UserId.Returns((Guid?)userGuid);
        _wishlistRepository
            .GetByUserAndProductAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _sut.Handle(
            new RemoveFromWishlistCommand(productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistRepository.Received(1).RemoveAsync(
            Arg.Is<UserId>(u => u == UserId.From(userGuid)),
            Arg.Is<ProductId>(p => p == ProductId.From(productGuid)),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
