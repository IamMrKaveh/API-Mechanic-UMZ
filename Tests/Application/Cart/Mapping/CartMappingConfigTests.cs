using Application.Cart.Features.Shared;
using Application.Cart.Mapping;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Mapster;
using SharedKernel.ValueObjects;
using CartAggregate = Domain.Cart.Aggregates.Cart;
using CartGuestToken = Domain.Cart.ValueObjects.GuestToken;

namespace Tests.Application.Cart.Mapping;

public class CartMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public CartMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new CartMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    private static CartAggregate CreateUserCartWithItem(int quantity = 2, decimal unitPrice = 100m, decimal originalPrice = 120m)
    {
        var cart = new CartBuilder().Build();
        cart.AddItem(
            VariantId.NewId(),
            ProductId.NewId(),
            ProductName.Create("Brake Pad"),
            Sku.Create("SKU-001"),
            Money.Create(unitPrice, "IRT"),
            Money.Create(originalPrice, "IRT"),
            quantity,
            DateTime.UtcNow);
        return cart;
    }

    [Fact]
    public void Map_Cart_ToCartDetailDto_MapsIdentityAndTotals()
    {
        var cart = CreateUserCartWithItem(quantity: 2, unitPrice: 100m);

        var dto = _mapper.Map<CartDetailDto>(cart);

        dto.Id.ShouldBe(cart.Id.Value);
        dto.UserId.ShouldBe(cart.UserId!.Value);
        dto.GuestToken.ShouldBeNull();
        dto.IsCheckedOut.ShouldBeFalse();
        dto.TotalPrice.ShouldBe(200m);
        dto.TotalItems.ShouldBe(2);
        dto.Items.ShouldBeEmpty();
        dto.PriceChanges.ShouldBeEmpty();
    }

    [Fact]
    public void Map_GuestCart_ToCartDetailDto_MapsGuestTokenAndNullUser()
    {
        var cart = new CartBuilder().ForGuest(CartGuestToken.Generate()).Build();

        var dto = _mapper.Map<CartDetailDto>(cart);

        dto.UserId.ShouldBeNull();
        dto.GuestToken.ShouldBe(cart.GuestToken!.Value);
    }

    [Fact]
    public void Map_Cart_WithMultipleItems_SumsQuantities()
    {
        var cart = new CartBuilder().Build();
        new CartItemParametersBuilder().WithQuantity(1).WithUnitPrice(50m).AddTo(cart);
        new CartItemParametersBuilder().WithQuantity(3).WithUnitPrice(50m).AddTo(cart);

        var dto = _mapper.Map<CartDetailDto>(cart);

        dto.TotalItems.ShouldBe(4);
        dto.TotalPrice.ShouldBe(200m);
    }

    [Fact]
    public void Map_CartItem_ToCartItemDto_MapsAllFields()
    {
        var cart = CreateUserCartWithItem(quantity: 3, unitPrice: 100m, originalPrice: 120m);
        var item = cart.CartItems.Single();

        var dto = _mapper.Map<CartItemDto>(item);

        dto.Id.ShouldBe(item.Id.Value);
        dto.CartId.ShouldBe(item.CartId.Value);
        dto.VariantId.ShouldBe(item.VariantId.Value);
        dto.ProductId.ShouldBe(item.ProductId.Value);
        dto.ProductName.ShouldBe("Brake Pad");
        dto.Sku.ShouldBe("SKU-001");
        dto.UnitPrice.ShouldBe(100m);
        dto.OriginalPrice.ShouldBe(120m);
        dto.Quantity.ShouldBe(3);
        dto.TotalPrice.ShouldBe(300m);
        dto.AddedAt.ShouldBe(item.AddedAt);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new CartMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
