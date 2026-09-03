using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Cart.Aggregates;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Order.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutOrchestrationServiceTests
{
    private readonly ICheckoutAddressResolverService _addressResolver = Substitute.For<ICheckoutAddressResolverService>();
    private readonly ICheckoutCartItemBuilderService _cartItemBuilder = Substitute.For<ICheckoutCartItemBuilderService>();
    private readonly ICheckoutShippingValidatorService _shippingValidator = Substitute.For<ICheckoutShippingValidatorService>();
    private readonly ICheckoutDiscountApplicatorService _discountApplicator = Substitute.For<ICheckoutDiscountApplicatorService>();
    private readonly ICheckoutStockValidatorService _stockValidator = Substitute.For<ICheckoutStockValidatorService>();
    private readonly ICheckoutPriceValidatorService _priceValidator = Substitute.For<ICheckoutPriceValidatorService>();
    private readonly ICheckoutOrderCreationService _orderCreation = Substitute.For<ICheckoutOrderCreationService>();
    private readonly ICheckoutPaymentStrategyResolver _strategyResolver = Substitute.For<ICheckoutPaymentStrategyResolver>();
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>();
    private readonly ICheckoutPaymentStrategy _strategy = Substitute.For<ICheckoutPaymentStrategy>();
    private readonly CheckoutOrchestrationService _sut;

    public CheckoutOrchestrationServiceTests()
    {
        _sut = new CheckoutOrchestrationService(
            _addressResolver,
            _cartItemBuilder,
            _shippingValidator,
            _discountApplicator,
            _stockValidator,
            _priceValidator,
            _orderCreation,
            _strategyResolver,
            _cartRepository);
    }

    private static CheckoutFromCartCommand NewCommand(Guid? userId = null) =>
        new(
            CartId: Guid.NewGuid(),
            ShippingId: Guid.NewGuid(),
            AddressId: Guid.NewGuid(),
            DiscountCode: null,
            PaymentMethod: null,
            PaymentMethodId: null,
            IdempotencyKey: Guid.NewGuid())
        {
            UserId = userId ?? Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            UserAgent = "agent/1.0"
        };

    private static IReadOnlyList<OrderItemSnapshot> NewItems() =>
        [new OrderItemSnapshotBuilder().WithUnitPrice(100_000m, "IRT").Build()];

    private void StubHappyPath(CheckoutFromCartCommand command, CheckoutResultDto? orderDto = null)
    {
        var receiver = ReceiverInfo.Create("Ali Rezaei", "09121234567");
        var address = DeliveryAddress.Create("Tehran", "Tehran", "Valiasr St 123", "1234567890");
        var items = NewItems();
        orderDto ??= new CheckoutResultDto { OrderId = Guid.NewGuid(), OrderNumber = "ON-1", FinalAmount = 150_000m };

        _strategyResolver
            .ResolveAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ICheckoutPaymentStrategy>.Success(_strategy));
        _addressResolver
            .ResolveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(ReceiverInfo, DeliveryAddress)>.Success((receiver, address)));
        _cartItemBuilder
            .BuildAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutCartItemsResult>.Success(new CheckoutCartItemsResult(items, 100_000m)));
        _stockValidator
            .ValidateAsync(Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
        _priceValidator
            .ValidateAsync(Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
        _shippingValidator
            .ValidateAndCalculateCostAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Money>.Success(Money.FromDecimal(50_000m)));
        _discountApplicator
            .ApplyAsync(Arg.Any<string?>(), Arg.Any<Money>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(Money, Guid?)>.Success((Money.FromDecimal(0m), null)));
        _orderCreation
            .CreateAsync(
                Arg.Any<Guid>(), Arg.Any<ReceiverInfo>(), Arg.Any<DeliveryAddress>(),
                Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<Money>(), Arg.Any<Money>(),
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutResultDto>.Success(orderDto));
        _strategy
            .ExecuteAsync(
                Arg.Any<CheckoutResultDto>(), Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(), Arg.Any<Money>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => ServiceResult<CheckoutResultDto>.Success(
                (CheckoutResultDto)call.Arg<CheckoutResultDto>() with { PaymentAuthority = "AUTH-1" }));
    }

    private static global::Domain.Cart.Aggregates.Cart NewCheckoutableCart(Guid userId)
    {
        var cart = global::Domain.Cart.Aggregates.Cart.CreateForUser(global::Domain.User.ValueObjects.UserId.From(userId));
        new CartItemParametersBuilder().AddTo(cart);
        cart.ClearDomainEvents();
        return cart;
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenStrategyResolutionFails_ShortCircuits()
    {
        var command = NewCommand();
        _strategyResolver
            .ResolveAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ICheckoutPaymentStrategy>.Failure("unsupported"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _addressResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default, default, default);
        await _orderCreation.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenAddressResolutionFails_ShortCircuits()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _addressResolver
            .ResolveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(ReceiverInfo, DeliveryAddress)>.NotFound("user missing"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _cartItemBuilder.DidNotReceiveWithAnyArgs().BuildAsync(default, default, default);
        await _orderCreation.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenCartBuildFails_ShortCircuits()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _cartItemBuilder
            .BuildAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutCartItemsResult>.NotFound("cart missing"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _stockValidator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenStockValidationFails_ShortCircuits()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _stockValidator
            .ValidateAsync(Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("out of stock"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _priceValidator.DidNotReceiveWithAnyArgs().ValidateAsync(default!, default);
        await _orderCreation.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenPriceValidationFails_ShortCircuits()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _priceValidator
            .ValidateAsync(Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("price changed"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _shippingValidator.DidNotReceiveWithAnyArgs()
            .ValidateAndCalculateCostAsync(default, default, default!, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenShippingValidationFails_ShortCircuits()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _shippingValidator
            .ValidateAndCalculateCostAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Money>.Failure("no shipping"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _discountApplicator.DidNotReceiveWithAnyArgs().ApplyAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenDiscountApplicationFails_ShortCircuits()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _discountApplicator
            .ApplyAsync(Arg.Any<string?>(), Arg.Any<Money>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<(Money, Guid?)>.Failure("bad code"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _orderCreation.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenOrderCreationFails_ShortCircuitsBeforePayment()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _orderCreation
            .CreateAsync(
                Arg.Any<Guid>(), Arg.Any<ReceiverInfo>(), Arg.Any<DeliveryAddress>(),
                Arg.Any<IReadOnlyCollection<OrderItemSnapshot>>(), Arg.Any<Money>(), Arg.Any<Money>(),
                Arg.Any<Guid?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutResultDto>.Conflict("duplicate"));

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await _strategy.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, default!, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenStrategyExecutionFails_ReturnsFailureWithoutCartCheckout()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _strategy
            .ExecuteAsync(
                Arg.Any<CheckoutResultDto>(), Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(), Arg.Any<Money>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutResultDto>.Failure("pay failed"));
        var cart = NewCheckoutableCart(command.UserId);
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        cart.IsCheckedOut.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenAllStepsSucceed_ChecksOutCartAndReturnsStrategyResult()
    {
        var command = NewCommand();
        var orderDto = new CheckoutResultDto { OrderId = Guid.NewGuid(), OrderNumber = "ON-9", FinalAmount = 150_000m };
        StubHappyPath(command, orderDto);
        var cart = NewCheckoutableCart(command.UserId);
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.PaymentAuthority.ShouldBe("AUTH-1");
        result.Value.OrderId.ShouldBe(orderDto.OrderId);
        cart.IsCheckedOut.ShouldBeTrue();
        _cartRepository.Received(1).Update(cart);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_WhenCartNoLongerExists_StillReturnsSuccess()
    {
        var command = NewCommand();
        StubHappyPath(command);
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Cart.Aggregates.Cart?)null);

        var result = await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task ProcessCheckoutAsync_PassesSubtotalAndItemsBetweenSteps()
    {
        var command = NewCommand();
        StubHappyPath(command);

        await _sut.ProcessCheckoutAsync(command, CancellationToken.None);

        await _shippingValidator.Received(1).ValidateAndCalculateCostAsync(
            command.ShippingId,
            100_000m,
            Arg.Is<IReadOnlyCollection<OrderItemSnapshot>>(items => items.Count == 1),
            Arg.Any<CancellationToken>());
        await _discountApplicator.Received(1).ApplyAsync(
            command.DiscountCode,
            Arg.Is<Money>(m => m.Amount == 100_000m),
            command.UserId,
            Arg.Any<CancellationToken>());
    }
}
