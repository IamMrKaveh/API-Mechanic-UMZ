using Application.Common.Interfaces;
using Application.Wishlist.Features.Commands.ClearWishlist;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wishlist.Features.Commands.ClearWishlist;

public class ClearWishlistHandlerTests : HandlerTestBase
{
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>(); private readonly ClearWishlistHandler _sut;

    public ClearWishlistHandlerTests()
    {
        _sut = new ClearWishlistHandler(_wishlistRepository, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenInvoked_ClearsWishlistForCurrentUserAndReturnsSuccess()
    {
        var userGuid = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)userGuid);

        var result = await _sut.Handle(new ClearWishlistCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistRepository.Received(1).ClearAsync(
            Arg.Is<UserId>(u => u == UserId.From(userGuid)),
            Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
