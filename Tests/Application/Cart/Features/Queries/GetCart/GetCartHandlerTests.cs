using Application.Cart.Contracts;
using Application.Cart.Features.Queries.GetCart;
using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Cart.Features.Queries.GetCart;

public class GetCartHandlerTests
{
    private readonly ICartQueryService _cartQueryService = Substitute.For<ICartQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetCartHandler _sut;

    public GetCartHandlerTests()
    {
        _sut = new GetCartHandler(_cartQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsSuccessWithEmptyDto()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(new GetCartQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(Guid.Empty);
        result.Value.Items.ShouldBeEmpty();
        await _cartQueryService.DidNotReceiveWithAnyArgs()
            .GetCartDetailAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCart_ReturnsSuccessWithEmptyDto()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.GuestToken.Returns((string?)null);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns((CartDetailDto?)null);

        var result = await _sut.Handle(new GetCartQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUserHasCart_ReturnsSuccessWithServiceProvidedDto()
    {
        var expected = new CartDetailDto
        {
            Id = Guid.NewGuid(),
            TotalItems = 3,
            TotalPrice = 150m
        };
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.GuestToken.Returns((string?)null);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetCartQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenGuestTokenValid_PassesGuestTokenToQueryService()
    {
        UserId? capturedUserId = null;
        GuestToken? capturedGuestToken = null;
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns("GUEST-TOKEN-ABC12345");
        _cartQueryService
            .GetCartDetailAsync(
                Arg.Do<UserId?>(u => capturedUserId = u),
                Arg.Do<GuestToken?>(g => capturedGuestToken = g),
                Arg.Any<CancellationToken>())
            .Returns(new CartDetailDto());

        var result = await _sut.Handle(new GetCartQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        capturedUserId.ShouldBeNull();
        capturedGuestToken.ShouldNotBeNull();
        capturedGuestToken!.Value.ShouldBe("GUEST-TOKEN-ABC12345");
    }
}
