using Application.Audit.Contracts;
using Application.Cart.Features.Commands.SyncCartPrices;
using Application.Common.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Carts = Domain.Cart.Aggregates.Cart;

namespace Tests.Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>(); private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly SyncCartPricesHandler _sut;

    public SyncCartPricesHandlerTests()
    {
        _sut = new SyncCartPricesHandler(
            _cartRepository,
            _variantRepository,
            _auditService,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(new SyncCartPricesCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCart_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.GuestToken.Returns((string?)null);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(new SyncCartPricesCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenGuestHasNoCart_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns("GUEST-TOKEN-SYN12345");
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(new SyncCartPricesCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserCartHasItems_RefreshesPricesUpdatesAndLogsAudit()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder()
            .WithVariantId(variantId)
            .WithUnitPrice(100m, "IRT")
            .WithOriginalPrice(120m, "IRT")
            .WithQuantity(2)
            .AddTo(cart);

        var refreshedVariant = new ProductVariantBuilder()
            .WithId(variantId)
            .WithSellingPrice(80m, "IRT")
            .WithOriginalPrice(150m, "IRT")
            .Build();

        _currentUserService.UserId.Returns((Guid?)userId.Value);
        _currentUserService.GuestToken.Returns((string?)null);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(refreshedVariant);

        var result = await _sut.Handle(new SyncCartPricesCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        cart.CartItems.Single().SellingPrice.Amount.ShouldBe(80m);
        cart.CartItems.Single().OriginalPrice.Amount.ShouldBe(150m);
        _cartRepository.Received(1).Update(cart);
        await _auditService.Received(1).LogAsync(
            "Cart",
            "SyncCartPrices",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            "Cart",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGuestCartHasItems_RefreshesPricesUpdatesButDoesNotAudit()
    {
        var guestTokenValue = "GUEST-TOKEN-SYN98765";
        var guestToken = GuestToken.Create(guestTokenValue);
        var variantId = VariantId.NewId();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder()
            .WithVariantId(variantId)
            .WithUnitPrice(50m, "IRT")
            .WithOriginalPrice(60m, "IRT")
            .AddTo(cart);

        var refreshedVariant = new ProductVariantBuilder()
            .WithId(variantId)
            .WithSellingPrice(45m, "IRT")
            .WithOriginalPrice(70m, "IRT")
            .Build();

        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns(guestTokenValue);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(refreshedVariant);

        var result = await _sut.Handle(new SyncCartPricesCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        cart.CartItems.Single().SellingPrice.Amount.ShouldBe(45m);
        _cartRepository.Received(1).Update(cart);
        await _auditService.DidNotReceiveWithAnyArgs().LogAsync(
            default!, default!, default!, default, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenVariantNoLongerExists_KeepsExistingPricesAndUpdatesCart()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder()
            .WithVariantId(variantId)
            .WithUnitPrice(100m, "IRT")
            .WithOriginalPrice(120m, "IRT")
            .AddTo(cart);

        _currentUserService.UserId.Returns((Guid?)userId.Value);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var result = await _sut.Handle(new SyncCartPricesCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        cart.CartItems.Single().SellingPrice.Amount.ShouldBe(100m);
        cart.CartItems.Single().OriginalPrice.Amount.ShouldBe(120m);
        _cartRepository.Received(1).Update(cart);
    }
}
