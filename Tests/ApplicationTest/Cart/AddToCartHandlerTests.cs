using Application.Cart;
using Domain.Product.Interfaces;
using Domain.Variant.Aggregates;

namespace Tests.ApplicationTest.Cart;

public class AddToCartHandlerTests
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICartQueryService _cartQueryService;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CartItemValidationService _validationService;
    private readonly ILogger<AddToCartHandler> _logger;
    private readonly AddToCartHandler _handler;

    public AddToCartHandlerTests()
    {
        _cartRepository = Substitute.For<ICartRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _cartQueryService = Substitute.For<ICartQueryService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _validationService = new CartItemValidationService();
        _logger = Substitute.For<ILogger<AddToCartHandler>>();
        _handler = new AddToCartHandler(
            _cartRepository,
            _productRepository,
            _cartQueryService,
            _currentUser,
            _unitOfWork,
            _validationService,
            _logger);

        _currentUser.UserId.Returns(1);
        _currentUser.GuestId.Returns((string?)null);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ShouldReturnFailure()
    {
        _productRepository.GetVariantByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((ProductVariant?)null);

        var result = await _handler.Handle(new AddToCartCommand(VariantId: 1, Quantity: 2), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenVariantInactive_ShouldReturnFailure()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.Deactivate();
        _productRepository.GetVariantByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(variant);

        var result = await _handler.Handle(new AddToCartCommand(1, 1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenCartNotExists_ShouldCreateNewCartAndAddItem()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();
        _productRepository.GetVariantByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(variant);
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns((Domain.Cart.Aggregates.Cart?)null);
        _cartQueryService.GetCartDetailAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new CartDetailDto());

        var result = await _handler.Handle(new AddToCartCommand(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _cartRepository.Received(1).AddAsync(Arg.Any<Domain.Cart.Aggregates.Cart>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCartExists_ShouldAddItemToExistingCart()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();
        var cart = new CartBuilder().ForUser(1).Build();
        _productRepository.GetVariantByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(variant);
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(cart);
        _cartQueryService.GetCartDetailAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new CartDetailDto());

        var result = await _handler.Handle(new AddToCartCommand(1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cart.ContainsVariant(1).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenStockInsufficient_ShouldReturnFailure()
    {
        var variant = new ProductVariantBuilder().WithStock(1).Build();
        var cart = new CartBuilder().ForUser(1).Build();
        _productRepository.GetVariantByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(variant);
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(cart);

        var result = await _handler.Handle(new AddToCartCommand(1, 100), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldCallSaveChanges()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();
        var cart = new CartBuilder().ForUser(1).Build();
        _productRepository.GetVariantByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(variant);
        _cartRepository.GetCartAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(cart);
        _cartQueryService.GetCartDetailAsync(Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new CartDetailDto());

        await _handler.Handle(new AddToCartCommand(1, 1), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}