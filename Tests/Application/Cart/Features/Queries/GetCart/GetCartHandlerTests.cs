using Application.Cart.Contracts;
using Application.Cart.Features.Queries.GetCart;
using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Cart.Features.Queries.GetCart;

public class GetCartHandlerTests : HandlerTestBase
{
    private readonly ICartQueryService _cartQueryService = Substitute.For<ICartQueryService>(); private readonly GetCartHandler _sut;

    public GetCartHandlerTests()
    {
        _sut = new GetCartHandler(_cartQueryService, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsSuccessWithEmptyDto()
    {
        CurrentUserService.UserId.Returns((Guid?)null);
        CurrentUserService.GuestToken.Returns((string?)null);

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
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.GuestToken.Returns((string?)null);
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
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.GuestToken.Returns((string?)null);
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
        CurrentUserService.UserId.Returns((Guid?)null);
        CurrentUserService.GuestToken.Returns("GUEST-TOKEN-ABC12345");
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
