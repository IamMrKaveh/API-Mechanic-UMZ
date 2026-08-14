using Application.Media.Contracts;
using Application.Media.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Cart.QueryServices;
using Infrastructure.Cart.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Carts = Domain.Cart.Aggregates.Cart;

namespace Tests.Infrastructure.Cart.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartQueryServiceIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private IMediaQueryService _mediaQueryService = null!; private CartQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _mediaQueryService = Substitute.For<IMediaQueryService>();
        _mediaQueryService
            .GetPrimaryByEntitiesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, MediaDto>());

        _sut = new CartQueryService(_context, _mediaQueryService);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [RequiresDockerFact]
    public async Task GetCartDetailAsync_WithUserId_WhenCartHasItems_ReturnsDetailWithAggregatedTotals()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();

        var item1 = new CartItemParametersBuilder()
            .WithProductName("Product A")
            .WithSku("SKU-A")
            .WithUnitPrice(100m, "IRT")
            .WithOriginalPrice(120m, "IRT")
            .WithQuantity(2);
        item1.AddTo(cart);

        var item2 = new CartItemParametersBuilder()
            .WithProductName("Product B")
            .WithSku("SKU-B")
            .WithUnitPrice(50m, "IRT")
            .WithOriginalPrice(50m, "IRT")
            .WithQuantity(3);
        item2.AddTo(cart);

        await SeedCartAsync(cart);

        var result = await _sut.GetCartDetailAsync(userId, null, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(cart.Id.Value);
        result.UserId.ShouldBe(userId.Value);
        result.GuestToken.ShouldBeNull();
        result.Items.Count.ShouldBe(2);
        result.TotalItems.ShouldBe(5);
        result.TotalPrice.ShouldBe(2 * 100m + 3 * 50m);
        result.PriceChanges.ShouldBeEmpty();

        var detailA = result.Items.Single(i => i.VariantSku == "SKU-A");
        detailA.ProductName.ShouldBe("Product A");
        detailA.Quantity.ShouldBe(2);
        detailA.UnitPrice.ShouldBe(100m);
        detailA.CurrentPrice.ShouldBe(100m);
        detailA.TotalPrice.ShouldBe(200m);
        detailA.IsAvailable.ShouldBeTrue();
        detailA.IsUnlimited.ShouldBeFalse();
        detailA.ProductImage.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetCartDetailAsync_WithGuestToken_WhenCartHasItems_ReturnsDetail()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();

        new CartItemParametersBuilder()
            .WithProductName("Guest Product")
            .WithUnitPrice(80m, "IRT")
            .WithOriginalPrice(80m, "IRT")
            .WithQuantity(1)
            .AddTo(cart);

        await SeedCartAsync(cart);

        var result = await _sut.GetCartDetailAsync(null, guestToken, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(cart.Id.Value);
        result.UserId.ShouldBeNull();
        result.GuestToken.ShouldBe(guestToken.Value);
        result.Items.Count.ShouldBe(1);
        result.TotalItems.ShouldBe(1);
        result.TotalPrice.ShouldBe(80m);
    }

    [RequiresDockerFact]
    public async Task GetCartDetailAsync_WhenNeitherUserIdNorGuestTokenProvided_ReturnsNull()
    {
        var result = await _sut.GetCartDetailAsync(null, null, CancellationToken.None);

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetCartDetailAsync_WithUnknownUserId_ReturnsNull()
    {
        var result = await _sut.GetCartDetailAsync(UserId.NewId(), null, CancellationToken.None);

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetCartDetailAsync_WhenCartIsCheckedOut_ReturnsNull()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().AddTo(cart);
        cart.Checkout();

        await SeedCartAsync(cart);

        var result = await _sut.GetCartDetailAsync(userId, null, CancellationToken.None);

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetCartDetailAsync_PopulatesProductImageFromMediaQueryService()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();

        var itemParams = new CartItemParametersBuilder()
            .WithProductName("Imaged Product")
            .WithSku("SKU-IMG")
            .WithUnitPrice(200m, "IRT")
            .WithOriginalPrice(200m, "IRT")
            .WithQuantity(1);
        itemParams.AddTo(cart);

        await SeedCartAsync(cart);

        var productGuid = itemParams.ProductId.Value;
        var mediaByProduct = new Dictionary<Guid, MediaDto>
        {
            [productGuid] = new MediaDto
            {
                Id = Guid.NewGuid(),
                EntityType = "Product",
                EntityId = productGuid,
                IsPrimary = true,
                PublicUrl = "https://cdn.example.test/imaged-product.jpg"
            }
        };

        _mediaQueryService
            .GetPrimaryByEntitiesAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(mediaByProduct);

        var result = await _sut.GetCartDetailAsync(userId, null, CancellationToken.None);

        result.ShouldNotBeNull();
        var item = result!.Items.Single();
        item.ProductImage.ShouldBe("https://cdn.example.test/imaged-product.jpg");

        await _mediaQueryService.Received(1).GetPrimaryByEntitiesAsync(
            "Product",
            Arg.Any<IEnumerable<Guid>>(),
            Arg.Any<CancellationToken>());
    }

    [RequiresDockerFact]
    public async Task GetCartSummaryAsync_WhenCartHasItems_ReturnsAggregatedCountsAndTotal()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();

        new CartItemParametersBuilder()
            .WithSku("SKU-S1")
            .WithUnitPrice(60m, "IRT")
            .WithOriginalPrice(60m, "IRT")
            .WithQuantity(2)
            .AddTo(cart);

        new CartItemParametersBuilder()
            .WithSku("SKU-S2")
            .WithUnitPrice(40m, "IRT")
            .WithOriginalPrice(40m, "IRT")
            .WithQuantity(5)
            .AddTo(cart);

        await SeedCartAsync(cart);

        var summary = await _sut.GetCartSummaryAsync(userId, null, CancellationToken.None);

        summary.ShouldNotBeNull();
        summary.ItemCount.ShouldBe(2);
        summary.TotalQuantity.ShouldBe(7);
        summary.TotalPrice.ShouldBe(2 * 60m + 5 * 40m);
    }

    [RequiresDockerFact]
    public async Task GetCartSummaryAsync_WhenNoCartExists_ReturnsZeroSummary()
    {
        var summary = await _sut.GetCartSummaryAsync(UserId.NewId(), null, CancellationToken.None);

        summary.ShouldNotBeNull();
        summary.ItemCount.ShouldBe(0);
        summary.TotalQuantity.ShouldBe(0);
        summary.TotalPrice.ShouldBe(0);
    }

    [RequiresDockerFact]
    public async Task GetCartSummaryAsync_WithGuestToken_ReturnsSummary()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder()
            .WithUnitPrice(30m, "IRT")
            .WithOriginalPrice(30m, "IRT")
            .WithQuantity(4)
            .AddTo(cart);

        await SeedCartAsync(cart);

        var summary = await _sut.GetCartSummaryAsync(null, guestToken, CancellationToken.None);

        summary.ItemCount.ShouldBe(1);
        summary.TotalQuantity.ShouldBe(4);
        summary.TotalPrice.ShouldBe(120m);
    }

    [RequiresDockerFact]
    public async Task ValidateCartForCheckoutAsync_WhenCartHasItems_ReturnsValid()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().AddTo(cart);

        await SeedCartAsync(cart);

        var validation = await _sut.ValidateCartForCheckoutAsync(userId, null, CancellationToken.None);

        validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeTrue();
        validation.Errors.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task ValidateCartForCheckoutAsync_WhenCartIsEmpty_ReturnsInvalidWithEmptyCartError()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();

        await SeedCartAsync(cart);

        var validation = await _sut.ValidateCartForCheckoutAsync(userId, null, CancellationToken.None);

        validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain("سبد خرید خالی است.");
    }

    [RequiresDockerFact]
    public async Task ValidateCartForCheckoutAsync_WhenCartDoesNotExist_ReturnsInvalidWithNotFoundError()
    {
        var validation = await _sut.ValidateCartForCheckoutAsync(UserId.NewId(), null, CancellationToken.None);

        validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain("سبد خرید یافت نشد.");
    }

    [RequiresDockerFact]
    public async Task ValidateCartForCheckoutAsync_WhenNeitherUserIdNorGuestTokenProvided_ReturnsInvalidWithNotFoundError()
    {
        var validation = await _sut.ValidateCartForCheckoutAsync(null, null, CancellationToken.None);

        validation.ShouldNotBeNull();
        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain("سبد خرید یافت نشد.");
    }

    private async Task SeedCartAsync(Carts cart)
    {
        var seedRepo = new CartRepository(_context);
        seedRepo.Add(cart);
        await _context.SaveChangesAsync();
    }
}
