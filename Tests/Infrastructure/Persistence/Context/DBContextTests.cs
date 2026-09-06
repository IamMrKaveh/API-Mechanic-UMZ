using Application.Search.Features.Shared;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Security.Models;
using Infrastructure.Wallet.Configurations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Persistence.Context;

public class DBContextTests
{
    private static DBContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql("Host=localhost;Database=mechanic_model_tests;Username=test;Password=test")
            .Options;

        return new DBContext(
            options,
            new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>()),
            new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>()));
    }

    [Theory]
    [InlineData(typeof(global::Domain.Attribute.Aggregates.AttributeType))]
    [InlineData(typeof(global::Domain.Audit.Entities.AuditLog))]
    [InlineData(typeof(global::Domain.Brand.Aggregates.Brand))]
    [InlineData(typeof(global::Domain.Cart.Aggregates.Cart))]
    [InlineData(typeof(global::Domain.Category.Aggregates.Category))]
    [InlineData(typeof(global::Domain.Discount.Aggregates.DiscountCode))]
    [InlineData(typeof(global::Domain.Inventory.Aggregates.Inventory))]
    [InlineData(typeof(global::Domain.Media.Aggregates.Media))]
    [InlineData(typeof(global::Domain.Notification.Aggregates.Notification))]
    [InlineData(typeof(global::Domain.Order.Aggregates.Order))]
    [InlineData(typeof(global::Domain.Product.Aggregates.Product))]
    [InlineData(typeof(global::Domain.Review.Aggregates.ProductReview))]
    [InlineData(typeof(global::Domain.Shipping.Aggregates.Shipping))]
    [InlineData(typeof(global::Domain.Support.Aggregates.Ticket))]
    [InlineData(typeof(global::Domain.User.Aggregates.User))]
    [InlineData(typeof(global::Domain.Variant.Entities.VariantAttribute))]
    [InlineData(typeof(global::Domain.Wallet.Aggregates.Wallet))]
    [InlineData(typeof(global::Domain.Wallet.Aggregates.WalletFraudAlert))]
    [InlineData(typeof(global::Domain.Wallet.Entities.WalletLedgerEntry))]
    [InlineData(typeof(global::Domain.Wallet.Entities.WalletReservation))]
    [InlineData(typeof(global::Domain.Wishlist.Aggregates.Wishlist))]
    [InlineData(typeof(OutboxMessage))]
    [InlineData(typeof(RateLimitEntry))]
    [InlineData(typeof(FailedElasticOperation))]
    [InlineData(typeof(WalletReconciliationAudit))]
    public void Model_ContainsExpectedEntityType(Type entityType)
    {
        using var context = CreateContext();

        context.Model.FindEntityType(entityType).ShouldNotBeNull();
    }

    [Fact]
    public void DbSets_ExposeQueryableRoots()
    {
        using var context = CreateContext();

        context.AttributeTypes.ShouldNotBeNull();
        context.AuditLogs.ShouldNotBeNull();
        context.Brands.ShouldNotBeNull();
        context.Carts.ShouldNotBeNull();
        context.Categories.ShouldNotBeNull();
        context.DiscountCodes.ShouldNotBeNull();
        context.Inventories.ShouldNotBeNull();
        context.Medias.ShouldNotBeNull();
        context.Notifications.ShouldNotBeNull();
        context.Orders.ShouldNotBeNull();
        context.OutboxMessages.ShouldNotBeNull();
        context.PaymentTransactions.ShouldNotBeNull();
        context.Products.ShouldNotBeNull();
        context.RateLimitEntries.ShouldNotBeNull();
        context.Shippings.ShouldNotBeNull();
        context.Tickets.ShouldNotBeNull();
        context.TicketMessages.ShouldNotBeNull();
        context.Users.ShouldNotBeNull();
        context.UserAddresses.ShouldNotBeNull();
        context.UserOtps.ShouldNotBeNull();
        context.UserSessions.ShouldNotBeNull();
        context.Wallets.ShouldNotBeNull();
        context.WalletFraudAlerts.ShouldNotBeNull();
        context.WalletLedgerEntries.ShouldNotBeNull();
        context.WalletReservations.ShouldNotBeNull();
        context.Wishlists.ShouldNotBeNull();
        context.FailedElasticOperations.ShouldNotBeNull();
    }

    public static TheoryData<Type> StronglyTypedIds() => new()
    {
        typeof(global::Domain.Product.ValueObjects.ProductId),
        typeof(global::Domain.Review.ValueObjects.ReviewId),
        typeof(global::Domain.Security.ValueObjects.OtpId),
        typeof(global::Domain.Security.ValueObjects.SessionId),
        typeof(global::Domain.Shipping.ValueObjects.ShippingId),
        typeof(global::Domain.Support.ValueObjects.TicketId),
        typeof(global::Domain.Support.ValueObjects.TicketMessageId),
        typeof(global::Domain.User.ValueObjects.UserAddressId),
        typeof(global::Domain.User.ValueObjects.UserId),
        typeof(global::Domain.Variant.ValueObjects.VariantAttributeId),
        typeof(global::Domain.Variant.ValueObjects.VariantId),
        typeof(global::Domain.Variant.ValueObjects.VariantShippingId),
        typeof(global::Domain.Wallet.ValueObjects.WalletId),
        typeof(global::Domain.Wallet.ValueObjects.WalletLedgerEntryId),
        typeof(global::Domain.Wallet.ValueObjects.WalletReservationId),
        typeof(global::Domain.Wishlist.ValueObjects.WishlistId),
    };

    [Theory]
    [MemberData(nameof(StronglyTypedIds))]
    public void Model_StronglyTypedIdProperty_HasGuidBackedRoundtrippableConverter(Type idType)
    {
        using var context = CreateContext();

        var matching = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == idType)
            .ToList();

        matching.ShouldNotBeEmpty($"expected at least one mapped property of type {idType.Name}");

        var fromFactory = idType.GetMethod("From", [typeof(Guid)]);
        fromFactory.ShouldNotBeNull();

        foreach (var property in matching)
        {
            var converter = property.GetValueConverter();
            converter.ShouldNotBeNull();
            converter.ModelClrType.ShouldBe(idType);
            converter.ProviderClrType.ShouldBeOneOf(typeof(Guid), typeof(Guid?));

            var providerValue = Guid.NewGuid();
            var modelValue = fromFactory!.Invoke(null, [providerValue]);
            converter.ConvertToProviderExpression.Compile().DynamicInvoke(modelValue)
                .ShouldBe(providerValue);
        }
    }

    [Fact]
    public void Model_DecimalProperties_UsePrecision18WithScale2Or4()
    {
        using var context = CreateContext();

        var decimals = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
            .ToList();

        decimals.ShouldNotBeEmpty();

        foreach (var property in decimals)
        {
            if (property.GetColumnType() is string columnType &&
                columnType.Contains("decimal", StringComparison.OrdinalIgnoreCase))
                continue;

            property.GetPrecision().ShouldBe(18, $"decimal {property.DeclaringType.Name}.{property.Name} should default to precision 18");
            property.GetScale().ShouldBeOneOf(4, 2);
        }
    }

    [Theory]
    [InlineData(typeof(global::Domain.Product.ValueObjects.ProductSlug))]
    [InlineData(typeof(global::Domain.Brand.ValueObjects.BrandSlug))]
    [InlineData(typeof(global::Domain.Category.ValueObjects.CategorySlug))]
    public void Model_SlugValueObjects_AreOwned(Type slugType)
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(slugType);
        entityType.ShouldNotBeNull();
        entityType!.IsOwned().ShouldBeTrue();
    }

    [Fact]
    public void Construction_RegistersBothInterceptors()
    {
        var auditable = new AuditableEntityInterceptor(Substitute.For<IDateTimeProvider>());
        var domainEvents = new DomainEventInterceptor(Substitute.For<IOutboxEventTypeRegistry>());

        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql("Host=localhost;Database=mechanic_model_tests;Username=test;Password=test")
            .Options;

        using var context = new DBContext(options, auditable, domainEvents);

        var coreOptions = context.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>();

        coreOptions.ShouldNotBeNull();
        coreOptions!.Interceptors.ShouldContain(auditable);
        coreOptions.Interceptors.ShouldContain(domainEvents);
    }

    [Fact]
    public void Model_FailedElasticOperations_MappedToExpectedTable()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(FailedElasticOperation));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("FailedElasticOperations");
    }

    [Fact]
    public void Model_RateLimitEntries_MappedToExpectedTable()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(RateLimitEntry));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("RateLimitEntries");
    }

    [Fact]
    public void Model_CanBeBuiltRepeatedly_WithoutThrowing()
    {
        using var first = CreateContext();
        using var second = CreateContext();

        first.Model.ShouldNotBeNull();
        second.Model.ShouldNotBeNull();
        first.Model.GetEntityTypes().Count().ShouldBe(second.Model.GetEntityTypes().Count());
    }
}
