using Application.Cart.Contracts;
using Application.Cart.Features.Queries.GetCartSummary;
using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Cart.Features.Queries.GetCartSummary;

public class GetCartSummaryHandlerTests
{
    private readonly ICartQueryService _cartQueryService = Substitute.For<ICartQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetCartSummaryHandler _sut;

    public GetCartSummaryHandlerTests()
    {
        _sut = new GetCartSummaryHandler(_cartQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsValidationFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(new GetCartSummaryQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _cartQueryService.DidNotReceiveWithAnyArgs()
            .GetCartSummaryAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserIsAuthenticated_ReturnsSuccessWithSummary()
    {
        var expected = new CartSummaryDto { ItemCount = 2, TotalQuantity = 5, TotalPrice = 200m };
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.GuestToken.Returns((string?)null);
        _cartQueryService
            .GetCartSummaryAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetCartSummaryQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenGuestTokenValid_ReturnsSuccessWithSummary()
    {
        var expected = new CartSummaryDto { ItemCount = 1, TotalQuantity = 1, TotalPrice = 50m };
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns("GUEST-TOKEN-XYZ98765");
        _cartQueryService
            .GetCartSummaryAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetCartSummaryQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }
}
