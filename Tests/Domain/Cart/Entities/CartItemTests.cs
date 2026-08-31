using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Cart.Entities;

public class CartItemTests
{
    [Fact]
    public void AddItem_WithValidParameters_ProducesInitializedCartItem()
    {
        var cart = new CartBuilder().Build();
        var parameters = new CartItemParametersBuilder()
            .WithProductName("Product A")
            .WithSku("SKU-A-01")
            .WithUnitPrice(150m)
            .WithOriginalPrice(200m)
            .WithQuantity(3);

        parameters.AddTo(cart);
        var sut = cart.CartItems.Single();

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.CartId.ShouldBe(cart.Id);
        sut.VariantId.ShouldBe(parameters.VariantId);
        sut.ProductId.ShouldBe(parameters.ProductId);
        sut.ProductName.ShouldBe(parameters.ProductName);
        sut.VariantSku.ShouldBe(parameters.Sku);
        sut.Quantity.ShouldBe(3);
    }

    [Fact]
    public void AddItem_SetsAddedAtCloseToUtcNow()
    {
        var cart = new CartBuilder().Build();
        var before = DateTime.UtcNow.AddSeconds(-1);

        new CartItemParametersBuilder().AddTo(cart);
        var sut = cart.CartItems.Single();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.AddedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.AddedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void AddItem_CopiesUnitPriceInsteadOfSharingReference()
    {
        var cart = new CartBuilder().Build();
        var unitPrice = Money.Create(120m, "IRT");
        var originalPrice = Money.Create(150m, "IRT");

        new CartItemParametersBuilder()
            .WithUnitPrice(unitPrice)
            .WithOriginalPrice(originalPrice)
            .AddTo(cart);
        var sut = cart.CartItems.Single();

        sut.SellingPrice.ShouldNotBeSameAs(unitPrice);
        sut.OriginalPrice.ShouldNotBeSameAs(originalPrice);
        sut.SellingPrice.Amount.ShouldBe(120m);
        sut.OriginalPrice.Amount.ShouldBe(150m);
    }

    [Fact]
    public void AddItem_ComputesTotalPriceAsUnitPriceTimesQuantity()
    {
        var cart = new CartBuilder().Build();

        new CartItemParametersBuilder()
            .WithUnitPrice(100m)
            .WithQuantity(4)
            .AddTo(cart);
        var sut = cart.CartItems.Single();

        sut.TotalPrice.Amount.ShouldBe(400m);
        sut.TotalPrice.Currency.ShouldBe(sut.SellingPrice.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void AddItem_WithNonPositiveQuantity_ThrowsInvalidCartQuantityException(int quantity)
    {
        var cart = new CartBuilder().Build();
        var parameters = new CartItemParametersBuilder().WithQuantity(quantity);

        Should.Throw<InvalidCartQuantityException>(() => parameters.AddTo(cart));
    }

    [Fact]
    public void AddItem_WhenVariantAlreadyExists_IncrementsExistingQuantity()
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();

        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(2).AddTo(cart);
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(3).AddTo(cart);

        cart.CartItems.Count.ShouldBe(1);
        cart.CartItems.Single().Quantity.ShouldBe(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddItem_WhenVariantExistsAndIncrementNonPositive_ThrowsInvalidCartQuantityException(int increment)
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(1).AddTo(cart);

        Should.Throw<InvalidCartQuantityException>(
            () => new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(increment).AddTo(cart));
    }

    [Fact]
    public void UpdateItemQuantity_WithPositiveQuantity_SetsQuantityOnCartItem()
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(1).AddTo(cart);

        cart.UpdateItemQuantity(variantId, 10);

        cart.CartItems.Single().Quantity.ShouldBe(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateItemQuantity_WithNonPositiveQuantity_ThrowsInvalidCartQuantityException(int newQuantity)
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(cart);

        Should.Throw<InvalidCartQuantityException>(() => cart.UpdateItemQuantity(variantId, newQuantity));
    }

    [Fact]
    public void RefreshItemPrice_WithNewMoney_UpdatesSellingAndOriginalPrice()
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithUnitPrice(100m).WithOriginalPrice(120m).AddTo(cart);
        var newUnit = Money.Create(90m, "IRT");
        var newOriginal = Money.Create(110m, "IRT");

        cart.RefreshItemPrice(variantId, newUnit, newOriginal);
        var sut = cart.CartItems.Single();

        sut.SellingPrice.Amount.ShouldBe(90m);
        sut.OriginalPrice.Amount.ShouldBe(110m);
    }

    [Fact]
    public void RefreshItemPrice_CopiesProvidedMoneyInstances()
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(cart);
        var newUnit = Money.Create(90m, "IRT");
        var newOriginal = Money.Create(110m, "IRT");

        cart.RefreshItemPrice(variantId, newUnit, newOriginal);
        var sut = cart.CartItems.Single();

        sut.SellingPrice.ShouldNotBeSameAs(newUnit);
        sut.OriginalPrice.ShouldNotBeSameAs(newOriginal);
    }

    [Fact]
    public void TotalPrice_ReflectsUpdatedQuantity()
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithUnitPrice(50m).WithQuantity(1).AddTo(cart);

        cart.UpdateItemQuantity(variantId, 6);
        var sut = cart.CartItems.Single();

        sut.TotalPrice.Amount.ShouldBe(300m);
    }

    [Fact]
    public void CartItem_HasUniqueIdentityAcrossInstances()
    {
        var cart = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(cart);
        new CartItemParametersBuilder().AddTo(cart);

        var ids = cart.CartItems.Select(i => i.Id).ToList();
        ids.Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public void CartItem_AfterAddingAndRemovingSameVariant_LeavesCartWithoutIt()
    {
        var cart = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(cart);

        cart.RemoveItem(variantId);

        cart.CartItems.ShouldBeEmpty();
    }
}
