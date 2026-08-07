using Domain.Cart.Enum;
using Domain.Cart.Events;
using Domain.Cart.Exceptions;
using Domain.Cart.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Carts = Domain.Cart.Aggregates.Cart;

namespace Tests.Domain.Cart.Aggregates;

public class CartTests
{
    [Fact]
    public void CreateForUser_WithValidUserId_ReturnsInitializedUserCart()
    {
        var userId = UserId.NewId();

        var sut = Carts.CreateForUser(userId);

        sut.Id.ShouldNotBeNull();
        sut.UserId.ShouldBe(userId);
        sut.GuestToken.ShouldBeNull();
        sut.IsCheckedOut.ShouldBeFalse();
        sut.CartItems.ShouldBeEmpty();
        sut.AppliedDiscountCodeId.ShouldBeNull();
        sut.AppliedDiscountCode.ShouldBeNull();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void CreateForUser_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = Carts.CreateForUser(UserId.NewId());

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void CreateForUser_ProducesCartWithVersionOne()
    {
        Carts.CreateForUser(UserId.NewId()).Version.ShouldBe(1);
    }

    [Fact]
    public void CreateForUser_RaisesExactlyOneCartCreatedEvent()
    {
        var sut = Carts.CreateForUser(UserId.NewId());

        sut.DomainEvents.Count.ShouldBe(1);
        sut.DomainEvents.Single().ShouldBeOfType<CartCreatedEvent>();
    }

    [Fact]
    public void CreateForUser_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Carts.CreateForUser(null!));
    }

    [Fact]
    public void CreateForGuest_WithValidGuestToken_ReturnsInitializedGuestCart()
    {
        var token = GuestToken.Generate();

        var sut = Carts.CreateForGuest(token);

        sut.UserId.ShouldBeNull();
        sut.GuestToken.ShouldBe(token);
        sut.IsCheckedOut.ShouldBeFalse();
        sut.CartItems.ShouldBeEmpty();
    }

    [Fact]
    public void CreateForGuest_WithNullToken_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => Carts.CreateForGuest(null!));
    }

    [Fact]
    public void AddItem_WithNewVariant_AddsNewCartItemAndRaisesEvent()
    {
        var sut = new CartBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var itemParams = new CartItemParametersBuilder().WithQuantity(2);

        itemParams.AddTo(sut);

        sut.CartItems.Count.ShouldBe(1);
        sut.CartItems.Single().VariantId.ShouldBe(itemParams.VariantId);
        sut.CartItems.Single().Quantity.ShouldBe(2);
        sut.Version.ShouldBe(versionBefore + 2);
        sut.DomainEvents.Single().ShouldBeOfType<CartItemAddedEvent>();
    }

    [Fact]
    public void AddItem_WithExistingVariant_IncrementsQuantityWithoutAddingNewEntry()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(2).AddTo(sut);

        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(3).AddTo(sut);

        sut.CartItems.Count.ShouldBe(1);
        sut.CartItems.Single().Quantity.ShouldBe(5);
    }

    [Fact]
    public void AddItem_WithDifferentVariants_AddsSeparateEntries()
    {
        var sut = new CartBuilder().Build();

        new CartItemParametersBuilder().AddTo(sut);
        new CartItemParametersBuilder().AddTo(sut);

        sut.CartItems.Count.ShouldBe(2);
    }

    [Fact]
    public void AddItem_OnCheckedOutCart_ThrowsCartAlreadyCheckedOutException()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(sut);
        sut.Checkout();

        Should.Throw<CartAlreadyCheckedOutException>(() => new CartItemParametersBuilder().AddTo(sut));
    }

    [Fact]
    public void AddItem_WithNullUnitPrice_ThrowsArgumentNullException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            sut.AddItem(VariantId.NewId(), ProductId.NewId(), ProductName.Create("PP"),
                Sku.Create("S"), null!, Money.Create(10m, "IRT"), 1));
    }

    [Fact]
    public void AddItem_WithNullOriginalPrice_ThrowsArgumentNullException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<ArgumentNullException>(() =>
            sut.AddItem(VariantId.NewId(), ProductId.NewId(), ProductName.Create("PP"),
                Sku.Create("S"), Money.Create(10m, "IRT"), null!, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithZeroOrNegativeQuantity_ThrowsInvalidCartQuantityException(int quantity)
    {
        var sut = new CartBuilder().Build();

        Should.Throw<InvalidCartQuantityException>(() =>
            new CartItemParametersBuilder().WithQuantity(quantity).AddTo(sut));
    }

    [Fact]
    public void RemoveItem_WithExistingVariant_RemovesAndRaisesEvent()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(4).AddTo(sut);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.RemoveItem(variantId);

        sut.CartItems.ShouldBeEmpty();
        sut.Version.ShouldBe(versionBefore + 2);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<CartItemRemovedEvent>();
        evt.RemovedQuantity.ShouldBe(4);
    }

    [Fact]
    public void RemoveItem_WithUnknownVariant_ThrowsCartItemNotFoundException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<CartItemNotFoundException>(() => sut.RemoveItem(VariantId.NewId()));
    }

    [Fact]
    public void RemoveItem_OnCheckedOutCart_ThrowsCartAlreadyCheckedOutException()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(sut);
        sut.Checkout();

        Should.Throw<CartAlreadyCheckedOutException>(() => sut.RemoveItem(variantId));
    }

    [Fact]
    public void UpdateItemQuantity_WithExistingVariant_UpdatesQuantityWithoutRaisingEvent()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(2).AddTo(sut);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.UpdateItemQuantity(variantId, 7);

        sut.CartItems.Single().Quantity.ShouldBe(7);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateItemQuantity_WithUnknownVariant_ThrowsCartItemNotFoundException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<CartItemNotFoundException>(() => sut.UpdateItemQuantity(VariantId.NewId(), 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateItemQuantity_WithZeroOrNegative_ThrowsInvalidCartQuantityException(int quantity)
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(sut);

        Should.Throw<InvalidCartQuantityException>(() => sut.UpdateItemQuantity(variantId, quantity));
    }

    [Fact]
    public void UpdateItemQuantity_OnCheckedOutCart_ThrowsCartAlreadyCheckedOutException()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(sut);
        sut.Checkout();

        Should.Throw<CartAlreadyCheckedOutException>(() => sut.UpdateItemQuantity(variantId, 3));
    }

    [Fact]
    public void RefreshItemPrice_WithExistingVariant_UpdatesPricesWithoutRaisingEvent()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder()
            .WithVariantId(variantId)
            .WithUnitPrice(100m, "IRT")
            .WithOriginalPrice(120m, "IRT")
            .AddTo(sut);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.RefreshItemPrice(variantId, Money.Create(90m, "IRT"), Money.Create(150m, "IRT"));

        sut.CartItems.Single().SellingPrice.Amount.ShouldBe(90m);
        sut.CartItems.Single().OriginalPrice.Amount.ShouldBe(150m);
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RefreshItemPrice_WithUnknownVariant_ThrowsCartItemNotFoundException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<CartItemNotFoundException>(() =>
            sut.RefreshItemPrice(VariantId.NewId(), Money.Create(1m, "IRT"), Money.Create(1m, "IRT")));
    }

    [Fact]
    public void RefreshItemPrice_WithNullPrice_ThrowsArgumentNullException()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(sut);

        Should.Throw<ArgumentNullException>(() =>
            sut.RefreshItemPrice(variantId, null!, Money.Create(1m, "IRT")));
    }

    [Fact]
    public void RefreshItemPrice_OnCheckedOutCart_ThrowsCartAlreadyCheckedOutException()
    {
        var sut = new CartBuilder().Build();
        var variantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(sut);
        sut.Checkout();

        Should.Throw<CartAlreadyCheckedOutException>(() =>
            sut.RefreshItemPrice(variantId, Money.Create(1m, "IRT"), Money.Create(1m, "IRT")));
    }

    [Fact]
    public void Clear_OnNonEmptyCart_EmptiesItemsWithoutRaisingEvent()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(sut);
        new CartItemParametersBuilder().AddTo(sut);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Clear();

        sut.CartItems.ShouldBeEmpty();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Clear_OnEmptyCart_StillBumpsVersionByOneWithoutRaisingEvent()
    {
        var sut = new CartBuilder().Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Clear();

        sut.CartItems.ShouldBeEmpty();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Clear_OnCheckedOutCart_ThrowsCartAlreadyCheckedOutException()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(sut);
        sut.Checkout();

        Should.Throw<CartAlreadyCheckedOutException>(sut.Clear);
    }

    [Fact]
    public void Checkout_OnNonEmptyCart_MarksCheckedOutAndRaisesEvent()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder()
            .WithUnitPrice(100m, "IRT")
            .WithQuantity(2)
            .AddTo(sut);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Checkout();

        sut.IsCheckedOut.ShouldBeTrue();
        sut.Version.ShouldBe(versionBefore + 2);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<CartCheckedOutEvent>();
        evt.ItemCount.ShouldBe(1);
        evt.TotalAmount.ShouldBe(200m);
    }

    [Fact]
    public void Checkout_OnEmptyCart_ThrowsInvalidOperationException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<InvalidOperationException>(sut.Checkout);
    }

    [Fact]
    public void Checkout_TwiceInARow_ThrowsCartAlreadyCheckedOutException()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(sut);
        sut.Checkout();

        Should.Throw<CartAlreadyCheckedOutException>(sut.Checkout);
    }

    [Fact]
    public void AssignToUser_OnGuestCart_ReplacesGuestTokenWithUserIdWithoutRaisingEvent()
    {
        var guestToken = GuestToken.Generate();
        var sut = new CartBuilder().ForGuest(guestToken).Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var newUserId = UserId.NewId();

        sut.AssignToUser(newUserId);

        sut.UserId.ShouldBe(newUserId);
        sut.GuestToken.ShouldBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void AssignToUser_WithNull_ThrowsArgumentNullException()
    {
        var sut = new CartBuilder().Build();

        Should.Throw<ArgumentNullException>(() => sut.AssignToUser(null!));
    }

    [Fact]
    public void AssignToUser_DoesNotEnforceCheckedOutGate()
    {
        var sut = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        new CartItemParametersBuilder().AddTo(sut);
        sut.Checkout();
        var newUserId = UserId.NewId();

        Should.NotThrow(() => sut.AssignToUser(newUserId));
        sut.UserId.ShouldBe(newUserId);
    }

    [Fact]
    public void MergeFrom_WithSumQuantitiesDefault_CallsAddItemPerSourceItem()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        new CartItemParametersBuilder().WithQuantity(2).AddTo(source);
        new CartItemParametersBuilder().WithQuantity(3).AddTo(source);
        target.ClearDomainEvents();
        var versionBefore = target.Version;

        target.MergeFrom(source);

        target.CartItems.Count.ShouldBe(2);
        target.Version.ShouldBe(versionBefore + (2 * 2) + 2);
        target.DomainEvents.Count(e => e is CartItemAddedEvent).ShouldBe(2);
        target.DomainEvents.OfType<CartMergedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void MergeFrom_WithSumQuantities_MergesQuantityWhenVariantExistsOnTarget()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        var sharedVariant = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(sharedVariant).WithQuantity(4).AddTo(target);
        new CartItemParametersBuilder().WithVariantId(sharedVariant).WithQuantity(3).AddTo(source);

        target.MergeFrom(source, CartMergeStrategy.SumQuantities);

        target.CartItems.Single(i => i.VariantId == sharedVariant).Quantity.ShouldBe(7);
    }

    [Fact]
    public void MergeFrom_WithKeepUserCart_LeavesTargetItemsUnchangedButStillRaisesCartMergedEvent()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        new CartItemParametersBuilder().WithQuantity(5).AddTo(target);
        new CartItemParametersBuilder().WithQuantity(9).AddTo(source);
        target.ClearDomainEvents();
        var versionBefore = target.Version;

        target.MergeFrom(source, CartMergeStrategy.KeepUserCart);

        target.CartItems.Count.ShouldBe(1);
        target.CartItems.Single().Quantity.ShouldBe(5);
        target.Version.ShouldBe(versionBefore + 2);
        target.DomainEvents.Single().ShouldBeOfType<CartMergedEvent>();
    }

    [Fact]
    public void MergeFrom_WithKeepGuestCart_ClearsTargetAndReplacesWithSourceItems()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        new CartItemParametersBuilder().WithQuantity(5).AddTo(target);
        new CartItemParametersBuilder().WithQuantity(9).AddTo(source);
        new CartItemParametersBuilder().WithQuantity(3).AddTo(source);
        target.ClearDomainEvents();
        var versionBefore = target.Version;

        target.MergeFrom(source, CartMergeStrategy.KeepGuestCart);

        target.CartItems.Count.ShouldBe(2);
        target.CartItems.Any(i => i.Quantity == 5).ShouldBeFalse();
        target.Version.ShouldBe(versionBefore + 2);
        target.DomainEvents.Count(e => e is CartItemAddedEvent).ShouldBe(0);
        target.DomainEvents.Single().ShouldBeOfType<CartMergedEvent>();
    }

    [Fact]
    public void MergeFrom_WithKeepHigherQuantity_KeepsMaxQuantityForSharedVariantAndAddsNewOnes()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        var sharedVariant = VariantId.NewId();
        var extraVariant = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(sharedVariant).WithQuantity(5).AddTo(target);
        new CartItemParametersBuilder().WithVariantId(sharedVariant).WithQuantity(3).AddTo(source);
        new CartItemParametersBuilder().WithVariantId(extraVariant).WithQuantity(7).AddTo(source);
        target.ClearDomainEvents();

        target.MergeFrom(source, CartMergeStrategy.KeepHigherQuantity);

        target.CartItems.Single(i => i.VariantId == sharedVariant).Quantity.ShouldBe(5);
        target.CartItems.Single(i => i.VariantId == extraVariant).Quantity.ShouldBe(7);
        target.DomainEvents.Count(e => e is CartItemAddedEvent).ShouldBe(0);
    }

    [Fact]
    public void MergeFrom_WithKeepHigherQuantity_UpgradesQuantityWhenSourceIsHigher()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        var sharedVariant = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(sharedVariant).WithQuantity(2).AddTo(target);
        new CartItemParametersBuilder().WithVariantId(sharedVariant).WithQuantity(9).AddTo(source);

        target.MergeFrom(source, CartMergeStrategy.KeepHigherQuantity);

        target.CartItems.Single().Quantity.ShouldBe(9);
    }

    [Fact]
    public void MergeFrom_MergedItemCountEqualsSourceCountRegardlessOfStrategy()
    {
        var target = new CartBuilder().Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        new CartItemParametersBuilder().AddTo(source);
        new CartItemParametersBuilder().AddTo(source);
        new CartItemParametersBuilder().AddTo(source);
        target.ClearDomainEvents();

        target.MergeFrom(source, CartMergeStrategy.KeepUserCart);

        var evt = target.DomainEvents.OfType<CartMergedEvent>().Single();
        evt.MergedItemCount.ShouldBe(3);
    }

    [Fact]
    public void MergeFrom_OnCartWithoutUserId_ThrowsInvalidOperationException()
    {
        var guestCart = new CartBuilder().ForGuest(GuestToken.Generate()).Build();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();

        Should.Throw<InvalidOperationException>(() => guestCart.MergeFrom(source));
    }

    [Fact]
    public void MergeFrom_OnCheckedOutTarget_ThrowsCartAlreadyCheckedOutException()
    {
        var target = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(target);
        target.Checkout();
        var source = new CartBuilder().ForGuest(GuestToken.Generate()).Build();

        Should.Throw<CartAlreadyCheckedOutException>(() => target.MergeFrom(source));
    }

    [Fact]
    public void MergeFrom_WithNullSourceCart_ThrowsInvalidOperationException()
    {
        var target = new CartBuilder().Build();

        Should.Throw<InvalidOperationException>(() => target.MergeFrom(null!, CartMergeStrategy.SumQuantities));
    }

    [Fact]
    public void IsEmpty_OnEmptyCart_ReturnsTrue()
    {
        new CartBuilder().Build().IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_OnNonEmptyCart_ReturnsFalse()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder().AddTo(sut);

        sut.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void TotalAmount_OnEmptyCart_IsZero()
    {
        new CartBuilder().Build().TotalAmount.Amount.ShouldBe(0m);
    }

    [Fact]
    public void TotalAmount_SumsAllCartItemsInIrtCurrency()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder().WithUnitPrice(50m, "IRT").WithQuantity(2).AddTo(sut);
        new CartItemParametersBuilder().WithUnitPrice(100m, "IRT").WithQuantity(3).AddTo(sut);

        sut.TotalAmount.Amount.ShouldBe(400m);
    }

    [Fact]
    public void CartItemTotalPrice_EqualsSellingPriceMultipliedByQuantity()
    {
        var sut = new CartBuilder().Build();
        new CartItemParametersBuilder()
            .WithUnitPrice(75m, "IRT")
            .WithQuantity(4)
            .AddTo(sut);

        sut.CartItems.Single().TotalPrice.Amount.ShouldBe(300m);
    }

    [Fact]
    public void Equality_TwoCartsWithSameId_AreConsideredEqualByEntitySemantics()
    {
        var sut = new CartBuilder().Build();

        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    public void Equality_TwoCartsWithDifferentIds_AreConsideredUnequal()
    {
        var a = new CartBuilder().Build();
        var b = new CartBuilder().Build();

        a.Equals(b).ShouldBeFalse();
    }
}
