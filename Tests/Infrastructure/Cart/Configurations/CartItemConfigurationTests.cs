using Domain.Cart.Entities;
using Domain.Product.ValueObjects;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Cart.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartItemConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SaveChanges_PersistsCartItemAndRoundTripsAllScalarPropertiesAndOwnedPrices()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();

        var itemParams = new CartItemParametersBuilder()
            .WithProductName("Alpha Product")
            .WithSku("SKU-ALPHA-1")
            .WithUnitPrice(250m, "IRT")
            .WithOriginalPrice(300m, "IRT")
            .WithQuantity(2);
        itemParams.AddTo(cart);
        cart.ClearDomainEvents();

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.CartItems.FirstAsync(ci => ci.CartId == cart.Id);

        reloaded.CartId.ShouldBe(cart.Id);
        reloaded.VariantId.ShouldBe(itemParams.VariantId);
        reloaded.ProductId.ShouldBe(itemParams.ProductId);
        reloaded.ProductName.Value.ShouldBe("Alpha Product");
        reloaded.VariantSku.Value.ShouldBe("SKU-ALPHA-1");
        reloaded.Quantity.ShouldBe(2);
        reloaded.SellingPrice.Amount.ShouldBe(250m);
        reloaded.SellingPrice.Currency.ShouldBe("IRT");
        reloaded.OriginalPrice.Amount.ShouldBe(300m);
        reloaded.OriginalPrice.Currency.ShouldBe("IRT");
        reloaded.AddedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_WhenCartDeleted_CascadesDeleteToCartItems()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();

        new CartItemParametersBuilder()
            .WithProductName("Cascade Product")
            .WithSku("SKU-CASCADE-1")
            .AddTo(cart);
        cart.ClearDomainEvents();

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        var cartId = cart.Id;
        Context.Carts.Remove(cart);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remainingItems = await Context.CartItems.CountAsync(ci => ci.CartId == cartId);

        remainingItems.ShouldBe(0);
    }

    [Fact]
    public void Model_MappedToCartItemsTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("CartItems");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(CartItem.Id));
    }

    [Fact]
    public void Model_Id_HasValueGeneratedNever()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var idProperty = entityType!.FindProperty(nameof(CartItem.Id));
        idProperty.ShouldNotBeNull();
        idProperty!.ValueGenerated.ShouldBe(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never);
    }

    [Fact]
    public void Model_VariantIdAndProductId_AreRequired()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var variantId = entityType!.FindProperty(nameof(CartItem.VariantId));
        variantId.ShouldNotBeNull();
        variantId!.IsNullable.ShouldBeFalse();

        var productId = entityType.FindProperty(nameof(CartItem.ProductId));
        productId.ShouldNotBeNull();
        productId!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_ProductName_IsRequiredAndHasMaxLengthFromProductName()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(CartItem.ProductName));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(ProductName.MaxLength);
    }

    [Fact]
    public void Model_VariantSku_IsRequiredAndHasMaxLength100()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(CartItem.VariantSku));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_QuantityAndAddedAt_AreRequired()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var quantity = entityType!.FindProperty(nameof(CartItem.Quantity));
        quantity.ShouldNotBeNull();
        quantity!.IsNullable.ShouldBeFalse();

        var addedAt = entityType.FindProperty(nameof(CartItem.AddedAt));
        addedAt.ShouldNotBeNull();
        addedAt!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_OwnsOriginalPrice_WithConfiguredColumnNamesAndPrecision()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(CartItem.OriginalPrice));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.IsOwnership.ShouldBeTrue();
        navigation.ForeignKey.IsRequired.ShouldBeTrue();

        var ownedType = navigation.ForeignKey.DeclaringEntityType;
        var amount = ownedType.FindProperty(nameof(Money.Amount));
        amount.ShouldNotBeNull();
        amount!.GetColumnName().ShouldBe("OriginalPriceAmount");
        amount.GetPrecision().ShouldBe(18);
        amount.GetScale().ShouldBe(2);
        amount.IsNullable.ShouldBeFalse();

        var currency = ownedType.FindProperty(nameof(Money.Currency));
        currency.ShouldNotBeNull();
        currency!.GetColumnName().ShouldBe("OriginalPriceCurrency");
        currency.GetMaxLength().ShouldBe(3);
        currency.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_OwnsSellingPrice_WithConfiguredColumnNamesAndPrecision()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(CartItem.SellingPrice));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.IsOwnership.ShouldBeTrue();
        navigation.ForeignKey.IsRequired.ShouldBeTrue();

        var ownedType = navigation.ForeignKey.DeclaringEntityType;
        var amount = ownedType.FindProperty(nameof(Money.Amount));
        amount.ShouldNotBeNull();
        amount!.GetColumnName().ShouldBe("SellingPriceAmount");
        amount.GetPrecision().ShouldBe(18);
        amount.GetScale().ShouldBe(2);
        amount.IsNullable.ShouldBeFalse();

        var currency = ownedType.FindProperty(nameof(Money.Currency));
        currency.ShouldNotBeNull();
        currency!.GetColumnName().ShouldBe("SellingPriceCurrency");
        currency.GetMaxLength().ShouldBe(3);
        currency.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_IgnoresTotalPriceVariantAndProductNavigations()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(CartItem.TotalPrice)).ShouldBeNull();
        entityType.FindNavigation(nameof(CartItem.Variant)).ShouldBeNull();
        entityType.FindNavigation(nameof(CartItem.Product)).ShouldBeNull();
    }

    [Fact]
    public void Model_HasIndexOnCartId()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(CartItem.CartId));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasIndexOnVariantId()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(CartItem.VariantId));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasIndexOnProductId()
    {
        var entityType = Context.Model.FindEntityType(typeof(CartItem));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(CartItem.ProductId));

        index.ShouldNotBeNull();
    }
}
