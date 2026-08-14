using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Commands.ToggleWishlist;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;
using Tests.TestInfrastructure.Assertions;
using Wishlists = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandlerTests
{
    private readonly IWishlistRepository _wishlistRepository = Substitute.For<IWishlistRepository>(); private readonly IWishlistQueryService _wishlistQueryService = Substitute.For<IWishlistQueryService>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ToggleWishlistHandler _sut;

    public ToggleWishlistHandlerTests()
    {
        _sut = new ToggleWishlistHandler(
            _wishlistRepository,
            _wishlistQueryService,
            _auditService,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenProductNotInWishlist_AddsAndReturnsSuccessWithTrue()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)userGuid);
        _wishlistQueryService
            .IsInWishlistAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Wishlists? added = null;
        _wishlistRepository
            .When(x => x.AddAsync(Arg.Any<Wishlists>(), Arg.Any<CancellationToken>()))
            .Do(ci => added = ci.Arg<Wishlists>());

        var result = await _sut.Handle(
            new ToggleWishlistCommand(productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeTrue();
        added.ShouldNotBeNull();
        added!.UserId.Value.ShouldBe(userGuid);
        added.ProductId.Value.ShouldBe(productGuid);
        await _wishlistRepository.Received(1).AddAsync(Arg.Any<Wishlists>(), Arg.Any<CancellationToken>());
        await _wishlistRepository.DidNotReceive().RemoveAsync(
            Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "ToggleWishlist",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductAlreadyInWishlist_RemovesAndReturnsSuccessWithFalse()
    {
        var userGuid = Guid.NewGuid();
        var productGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)userGuid);
        _wishlistQueryService
            .IsInWishlistAsync(Arg.Any<UserId>(), Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(
            new ToggleWishlistCommand(productGuid),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeFalse();
        await _wishlistRepository.Received(1).RemoveAsync(
            Arg.Is<UserId>(u => u == UserId.From(userGuid)),
            Arg.Is<ProductId>(p => p == ProductId.From(productGuid)),
            Arg.Any<CancellationToken>());
        await _wishlistRepository.DidNotReceive().AddAsync(
            Arg.Any<Wishlists>(), Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "ToggleWishlist",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
