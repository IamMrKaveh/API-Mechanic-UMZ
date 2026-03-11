using Application.Cart;

namespace Tests.ApplicationTest.Cart;

public class RemoveFromCartHandlerTests
{
    private readonly ICartRepository _cartRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RemoveFromCartHandler _handler;

    public RemoveFromCartHandlerTests()
    {
        _cartRepository = Substitute.For<ICartRepository>();
        _cartQueryService = Substitute.For<ICartQueryService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new RemoveFromCartHandler(_cartRepository, _cartQueryService, _currentUser, _unitOfWork);

        _currentUser.UserId.Returns(1);
        _currentUser.GuestId.Returns((string?)null);
    }

    [Fact]
    public async Task Handle_WhenCartExistsAndItemFound_ShouldRemoveItemAndReturnSuccess()
    {
        var cart = new CartBuilder().ForUser(1).WithItem(5, 2, 50_000m).Build();
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(cart);
        _cartQueryService.GetCartDetailAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new CartDetailDto());

        var result = await _handler.Handle(new RemoveFromCartCommand(VariantId: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cart.ContainsVariant(5).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCartNotFound_ShouldReturnFailure()
    {
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns((Domain.Cart.Aggregates.Cart?)null);

        var result = await _handler.Handle(new RemoveFromCartCommand(VariantId: 5), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldCallSaveChanges()
    {
        var cart = new CartBuilder().ForUser(1).WithItem(5, 2, 50_000m).Build();
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(cart);
        _cartQueryService.GetCartDetailAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new CartDetailDto());

        await _handler.Handle(new RemoveFromCartCommand(VariantId: 5), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}