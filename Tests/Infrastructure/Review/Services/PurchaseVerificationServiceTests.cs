using Application.Review.Configuration;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Review.Services;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Review.Services;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PurchaseVerificationServiceTests(PostgresContainerFixture fixture)
    : IntegrationTestBase(fixture)
{
    private PurchaseVerificationService BuildSut(int windowDays = 90) =>
        new(Context, Options.Create(new ReviewSettings { PurchaseReviewWindowDays = windowDays }));

    private async Task<(UserId userId, ProductId productId)> SeedDeliveredOrderAsync(
        int daysAgo = 5, CancellationToken ct = default)
    {
        var user = await SeedUserAsync(ct: ct);
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Purchase Product {suffix}")
            .WithSlug($"purchase-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();
        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku($"SKU-{Guid.NewGuid():N}"[..20])
            .Build();
        variant.ClearDomainEvents();
        Context.ProductVariants.Add(variant);
        await Context.SaveChangesAsync(ct);

        var order = new OrderBuilder()
            .WithUserId(user.Id)
            .WithItemSnapshots(new OrderItemSnapshotBuilder()
                .WithVariantId(variant.Id)
                .WithProductId(product.Id)
                .WithProductName(product.Name)
                .WithSku(variant.Sku)
                .WithQuantity(1)
                .WithUnitPrice(100_000m, "IRT")
                .Build())
            .Build();
        order.ClearDomainEvents();
        Context.Orders.Add(order);
        await Context.SaveChangesAsync(ct);

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .Build();
        transaction.ClearDomainEvents();
        Context.PaymentTransactions.Add(transaction);
        await Context.SaveChangesAsync(ct);

        order.MoveToPending();
        order.MarkAsPaid(transaction.Id);
        order.StartProcessing();
        order.MarkAsShipped();
        order.MarkAsDelivered();
        order.ClearDomainEvents();
        Context.Orders.Update(order);
        await Context.SaveChangesAsync(ct);

        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Orders\" SET \"DeliveredAt\" = {DateTime.UtcNow.AddDays(-daysAgo)} WHERE \"Id\" = {order.Id.Value}");
        Context.ChangeTracker.Clear();

        return (user.Id, product.Id);
    }

    [Fact]
    public async Task UserHasPurchasedProductAsync_WhenDeliveredOrderWithinWindow_ReturnsTrue()
    {
        var (userId, productId) = await SeedDeliveredOrderAsync(daysAgo: 5);

        var result = await BuildSut(windowDays: 90)
            .UserHasPurchasedProductAsync(userId, productId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task UserHasPurchasedProductAsync_WhenDeliveredBeforeWindow_ReturnsFalse()
    {
        var (userId, productId) = await SeedDeliveredOrderAsync(daysAgo: 100);

        var result = await BuildSut(windowDays: 90)
            .UserHasPurchasedProductAsync(userId, productId, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasPurchasedProductAsync_WhenOrderNotDelivered_ReturnsFalse()
    {
        var user = await SeedUserAsync();
        var (brand, category) = await SeedBrandWithCategoryAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var product = new ProductBuilder()
            .WithName($"Pending Product {suffix}")
            .WithSlug($"pending-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();
        Context.Products.Add(product);
        await Context.SaveChangesAsync();

        var result = await BuildSut()
            .UserHasPurchasedProductAsync(user.Id, product.Id, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasPurchasedProductAsync_WhenDifferentUser_ReturnsFalse()
    {
        var (_, productId) = await SeedDeliveredOrderAsync();
        var otherUser = await SeedUserAsync();

        var result = await BuildSut()
            .UserHasPurchasedProductAsync(otherUser.Id, productId, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasPurchasedProductAsync_WhenDifferentProduct_ReturnsFalse()
    {
        var (userId, _) = await SeedDeliveredOrderAsync();

        var result = await BuildSut()
            .UserHasPurchasedProductAsync(userId, ProductId.NewId(), CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasPurchasedProductAsync_WhenWindowIsNarrower_ExcludesOlderDelivery()
    {
        var (userId, productId) = await SeedDeliveredOrderAsync(daysAgo: 10);

        var withinWindow = await BuildSut(windowDays: 30)
            .UserHasPurchasedProductAsync(userId, productId, CancellationToken.None);
        var outsideWindow = await BuildSut(windowDays: 5)
            .UserHasPurchasedProductAsync(userId, productId, CancellationToken.None);

        withinWindow.ShouldBeTrue();
        outsideWindow.ShouldBeFalse();
    }
}
