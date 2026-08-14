using Application.Common.Interfaces;
using Application.Wishlist.Features.Commands.ClearWishlist;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wishlist.Features.Commands.ClearWishlist;

public class ClearWishlistHandlerTests
{
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ClearWishlistHandler _sut;

    public ClearWishlistHandlerTests()
    {
        _sut = new ClearWishlistHandler(_wishlistRepository, _unitOfWork, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenInvoked_ClearsWishlistForCurrentUserAndSavesAndReturnsSuccess()
    {
        var userGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userGuid);

        var result = await _sut.Handle(new ClearWishlistCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistRepository.Received(1).ClearAsync(
            Arg.Is<UserId>(u => u == UserId.From(userGuid)),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
