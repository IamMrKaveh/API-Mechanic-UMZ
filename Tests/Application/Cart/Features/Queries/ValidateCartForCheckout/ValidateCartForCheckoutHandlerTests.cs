using Application.Cart.Contracts;
using Application.Cart.Features.Queries.ValidateCartForCheckout;
using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Cart.Features.Queries.ValidateCartForCheckout;

public class ValidateCartForCheckoutHandlerTests
{
    private readonly ICartQueryService _cartQueryService = Substitute.For<ICartQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ValidateCartForCheckoutHandler _sut;

    public ValidateCartForCheckoutHandlerTests()
    {
        _sut = new ValidateCartForCheckoutHandler(_cartQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsValidationFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(new ValidateCartForCheckoutQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _cartQueryService.DidNotReceiveWithAnyArgs()
            .ValidateCartForCheckoutAsync(default, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserIsAuthenticated_ReturnsSuccessWithValidationDto()
    {
        var expected = new CartCheckoutValidationDto { IsValid = true };
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.GuestToken.Returns((string?)null);
        _cartQueryService
            .ValidateCartForCheckoutAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new ValidateCartForCheckoutQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenValidationDtoReportsIssues_PropagatesDtoContentToCaller()
    {
        var expected = new CartCheckoutValidationDto
        {
            IsValid = false,
            Errors = { "cart is empty" },
            StockIssues =
        {
            new CartStockIssueDto
            {
                VariantId = Guid.NewGuid(),
                ProductName = "P",
                RequestedQuantity = 3,
                AvailableStock = 1
            }
        }
        };
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns("GUEST-TOKEN-CHK12345");
        _cartQueryService
            .ValidateCartForCheckoutAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new ValidateCartForCheckoutQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain("cart is empty");
        result.Value.StockIssues.Count.ShouldBe(1);
    }
}
