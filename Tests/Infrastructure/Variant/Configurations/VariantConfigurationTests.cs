using global::Domain.Variant.Aggregates;
using Tests.TestInfrastructure.Base;
using Variants = global::Domain.Variant.Aggregates.ProductVariant;

namespace Tests.Infrastructure.Variant.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class VariantConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<global::Domain.Product.Aggregates.Product> SeedProductAsync(CancellationToken ct = default)
    {
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Variant Product {suffix}")
            .WithSlug($"variant-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();

        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);
        return product;
    }

    private async Task<Variants> PersistAsync(Variants variant, CancellationToken ct = default)
    {
        variant.ClearDomainEvents();
        Context.ProductVariants.Add(variant);
        await Context.SaveChangesAsync(ct);
        return variant;
    }

    [Fact]
    public async Task SaveChanges_PersistsVariantAndRoundTripsAllScalarAndOwnedProperties()
    {
        var product = await SeedProductAsync();
        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku($"SKU-{Guid.NewGuid():N}"[..20])
            .WithSellingPrice(250_000m, "IRT")
            .WithOriginalPrice(300_000m, "IRT")
            .Build();
        await PersistAsync(variant);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variant.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(variant.Id);
        reloaded.ProductId.ShouldBe(product.Id);
        reloaded.Sku.Value.ShouldBe(variant.Sku.Value);
        reloaded.SellingPrice.Amount.ShouldBe(250_000m);
        reloaded.SellingPrice.Currency.ShouldBe("IRT");
        reloaded.OriginalPrice.Amount.ShouldBe(300_000m);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsDeleted.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);
        reloaded.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicateSku_ThrowsDbUpdateException()
    {
        var product = await SeedProductAsync();
        var sku = $"SKU-{Guid.NewGuid():N}"[..20];
        await PersistAsync(new ProductVariantBuilder().WithProductId(product.Id).WithSku(sku).Build());

        var duplicate = new ProductVariantBuilder().WithProductId(product.Id).WithSku(sku).Build();
        duplicate.ClearDomainEvents();

        Context.ProductVariants.Add(duplicate);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_ChangePriceAndSku_PersistNewValues()
    {
        var product = await SeedProductAsync();
        var variant = await PersistAsync(new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSellingPrice(100_000m, "IRT")
            .Build());

        variant.ChangePrice(180_000m, 200_000m, "IRT");
        variant.ChangeSku(global::Domain.Variant.ValueObjects.Sku.Create($"SKU-{Guid.NewGuid():N}"[..20]));
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.ProductVariants.FirstAsync(v => v.Id == variant.Id);
        reloaded.SellingPrice.Amount.ShouldBe(180_000m);
        reloaded.OriginalPrice.Amount.ShouldBe(200_000m);
        reloaded.Sku.Value.ShouldBe(variant.Sku.Value);
        reloaded.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SaveChanges_DeactivateAndActivate_PersistIsActiveFlag()
    {
        var product = await SeedProductAsync();
        var variant = await PersistAsync(new ProductVariantBuilder().WithProductId(product.Id).Build());

        variant.Deactivate();
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();

        var deactivated = await Context.ProductVariants.FirstAsync(v => v.Id == variant.Id);
        deactivated.IsActive.ShouldBeFalse();

        deactivated.Activate();
        deactivated.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var activated = await Context.ProductVariants.FirstAsync(v => v.Id == variant.Id);
        activated.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_WhenVariantIsDeleted_AttributesAndShippingsAreCascadeDeleted()
    {
        var product = await SeedProductAsync();
        var variant = await PersistAsync(new ProductVariantBuilder().WithProductId(product.Id).Build());

        var attributeType = await new AttributeTypeBuilder()
            .WithName($"Color{Guid.NewGuid():N}"[..16])
            .WithDisplayName($"Color{Guid.NewGuid():N}"[..16])
            .BuildAsync();
        var attributeValue = attributeType.AddValue($"Red{Guid.NewGuid():N}"[..12], $"Red{Guid.NewGuid():N}"[..12]);
        attributeType.ClearDomainEvents();
        Context.AttributeTypes.Add(attributeType);
        await Context.SaveChangesAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "Red")]);

        var shipping = new ShippingBuilder().WithName($"Ship {Guid.NewGuid():N}"[..14]).Build();
        shipping.ClearDomainEvents();
        Context.Shippings.Add(shipping);
        await Context.SaveChangesAsync();

        variant.SetShippingMethods(1m, [new global::Domain.Shipping.ValueObjects.ShippingAssignment(shipping.Id, 1m, 10m, 10m, 10m)]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        (await Context.VariantAttributes.CountAsync(a => a.VariantId == variant.Id)).ShouldBe(1);
        (await Context.VariantShippings.CountAsync(s => s.VariantId == variant.Id)).ShouldBe(1);

        var loaded = await Context.ProductVariants
            .Include(v => v.Attributes)
            .Include(v => v.Shippings)
            .FirstAsync(v => v.Id == variant.Id);
        Context.ProductVariants.Remove(loaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        (await Context.VariantAttributes.CountAsync(a => a.VariantId == variant.Id)).ShouldBe(0);
        (await Context.VariantShippings.CountAsync(s => s.VariantId == variant.Id)).ShouldBe(0);
    }

    [Fact]
    public void Model_MappedToProductVariantsTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("ProductVariants");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Variants.Id));
    }

    [Fact]
    public void Model_Id_IsNeverValueGenerated()
    {
        var id = Context.Model.FindEntityType(typeof(Variants))!.FindProperty(nameof(Variants.Id));
        id.ShouldNotBeNull();
        id!.ValueGenerated.ShouldBe(ValueGenerated.Never);
        id.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_Sku_IsRequiredWithMaxLength100AndUniqueIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();

        var sku = entityType!.FindProperty(nameof(Variants.Sku));
        sku.ShouldNotBeNull();
        sku!.IsNullable.ShouldBeFalse();
        sku.GetMaxLength().ShouldBe(100);
        sku.GetValueConverter().ShouldNotBeNull();

        var index = entityType.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Variants.Sku));
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_SellingPriceAndOriginalPrice_AreRequiredOwnedTypes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();

        entityType!.FindNavigation(nameof(Variants.SellingPrice)).ShouldNotBeNull();
        entityType.FindNavigation(nameof(Variants.OriginalPrice)).ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasExpectedNonUniqueIndexes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(Variants.ProductId), nameof(Variants.IsActive) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
            index!.IsUnique.ShouldBeFalse();
        }
    }

    [Fact]
    public void Model_HasCascadeDeleteToAttributesAndShippings()
    {
        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();

        var attributes = entityType!.FindNavigation(nameof(Variants.Attributes));
        attributes.ShouldNotBeNull();
        attributes!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);

        var shippings = entityType.FindNavigation(nameof(Variants.Shippings));
        shippings.ShouldNotBeNull();
        shippings!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_CreatedAtIsRequired_UpdatedAtIsOptional()
    {
        var entityType = Context.Model.FindEntityType(typeof(Variants));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Variants.CreatedAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Variants.UpdatedAt))!.IsNullable.ShouldBeTrue();
    }
}
