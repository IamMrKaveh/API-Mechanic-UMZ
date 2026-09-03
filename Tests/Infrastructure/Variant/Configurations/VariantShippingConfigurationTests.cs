using global::Domain.Variant.Entities;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Variant.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class VariantShippingConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<global::Domain.Variant.Aggregates.ProductVariant> SeedVariantAsync(CancellationToken ct = default)
    {
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Ship Product {suffix}")
            .WithSlug($"ship-product-{suffix}")
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
        return variant;
    }

    private async Task<global::Domain.Shipping.Aggregates.Shipping> SeedShippingAsync(CancellationToken ct = default)
    {
        var shipping = new ShippingBuilder()
            .WithName($"Ship {Guid.NewGuid():N}"[..14])
            .WithBaseCost(80_000m, "IRT")
            .Build();
        shipping.ClearDomainEvents();

        Context.Shippings.Add(shipping);
        await Context.SaveChangesAsync(ct);
        return shipping;
    }

    [Fact]
    public async Task SaveChanges_SetShippingMethods_PersistsDimensionsAndMultiplier()
    {
        var variant = await SeedVariantAsync();
        var shipping = await SeedShippingAsync();

        variant.SetShippingMethods(1.5m, [new global::Domain.Shipping.ValueObjects.ShippingAssignment(shipping.Id, 2.5m, 30m, 20m, 15m)]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.VariantShippings.FirstOrDefaultAsync(s => s.VariantId == variant.Id && s.ShippingId == shipping.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.VariantId.ShouldBe(variant.Id);
        reloaded.ShippingId.ShouldBe(shipping.Id);
        reloaded.Weight.ShouldBe(2.5m);
        reloaded.Width.ShouldBe(30m);
        reloaded.Height.ShouldBe(20m);
        reloaded.Length.ShouldBe(15m);
        reloaded.ShippingMultiplier.ShouldBe(1.5m);
    }

    [Fact]
    public async Task SaveChanges_UpdateShippingMethods_UpdatesDimensions()
    {
        var variant = await SeedVariantAsync();
        var shipping = await SeedShippingAsync();

        variant.SetShippingMethods(1m, [new global::Domain.Shipping.ValueObjects.ShippingAssignment(shipping.Id, 1m, 10m, 10m, 10m)]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();

        variant.SetShippingMethods(2m, [new global::Domain.Shipping.ValueObjects.ShippingAssignment(shipping.Id, 5m, 50m, 40m, 30m)]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var rows = await Context.VariantShippings.Where(s => s.VariantId == variant.Id).ToListAsync();
        rows.Count.ShouldBe(1);
        rows[0].Weight.ShouldBe(5m);
        rows[0].ShippingMultiplier.ShouldBe(2m);
    }

    [Fact]
    public async Task SaveChanges_RemoveShippingMethods_DeletesRows()
    {
        var variant = await SeedVariantAsync();
        var shipping = await SeedShippingAsync();

        variant.SetShippingMethods(1m, [new global::Domain.Shipping.ValueObjects.ShippingAssignment(shipping.Id, 1m, 10m, 10m, 10m)]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();

        variant.SetShippingMethods(1m, []);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var count = await Context.VariantShippings.CountAsync(s => s.VariantId == variant.Id);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveChanges_WhenShippingHasVariantShippings_DeletingShippingIsRestricted()
    {
        var variant = await SeedVariantAsync();
        var shipping = await SeedShippingAsync();

        variant.SetShippingMethods(1m, [new global::Domain.Shipping.ValueObjects.ShippingAssignment(shipping.Id, 1m, 10m, 10m, 10m)]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.Shippings.FirstAsync(s => s.Id == shipping.Id);
        Context.Shippings.Remove(loaded);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantShipping));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(VariantShipping.Id));
    }

    [Fact]
    public void Model_VariantIdAndShippingId_AreRequiredWithConverters()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantShipping));
        entityType.ShouldNotBeNull();

        var variantId = entityType!.FindProperty(nameof(VariantShipping.VariantId));
        variantId.ShouldNotBeNull();
        variantId!.IsNullable.ShouldBeFalse();
        variantId.GetValueConverter().ShouldNotBeNull();

        var shippingId = entityType.FindProperty(nameof(VariantShipping.ShippingId));
        shippingId.ShouldNotBeNull();
        shippingId!.IsNullable.ShouldBeFalse();
        shippingId.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_DimensionsAndMultiplier_HaveDecimalColumnTypesAndDefaults()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantShipping));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(VariantShipping.Weight), nameof(VariantShipping.Width), nameof(VariantShipping.Height), nameof(VariantShipping.Length), nameof(VariantShipping.ShippingMultiplier) })
        {
            var property = entityType!.FindProperty(propertyName);
            property.ShouldNotBeNull();
            property!.IsNullable.ShouldBeFalse();
            property.GetColumnType().ShouldBe("numeric(10,3)");
        }

        entityType!.FindProperty(nameof(VariantShipping.ShippingMultiplier))!.GetDefaultValue().ShouldBe(1m);
    }

    [Fact]
    public void Model_Shipping_ForeignKeyHasRestrictDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantShipping));
        entityType.ShouldNotBeNull();

        var fk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(VariantShipping.ShippingId));
        fk.ShouldNotBeNull();
        fk!.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_HasIndexesOnVariantIdAndShippingId()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantShipping));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(VariantShipping.VariantId), nameof(VariantShipping.ShippingId) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
        }
    }
}
