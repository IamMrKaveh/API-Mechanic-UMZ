using global::Domain.Product.Aggregates;
using Tests.TestInfrastructure.Base;
using Products = global::Domain.Product.Aggregates.Product;

namespace Tests.Infrastructure.Product.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ProductConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<Products> SeedProductAsync(
        string? name = null,
        string? slug = null,
        CancellationToken ct = default)
    {
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName(name ?? $"Product-{suffix}")
            .WithSlug(slug ?? $"product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();

        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);
        return product;
    }

    [Fact]
    public async Task SaveChanges_PersistsProductAndRoundTripsAllScalarProperties()
    {
        var (brand, category) = await SeedBrandWithCategoryAsync();
        var product = new ProductBuilder()
            .WithName("Roundtrip Product")
            .WithSlug($"roundtrip-product-{Guid.NewGuid():N}"[..32])
            .WithDescription("A full description.")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();

        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(product.Id);
        reloaded.Name.Value.ShouldBe("Roundtrip Product");
        reloaded.Slug.Value.ShouldStartWith("roundtrip-product-");
        reloaded.Description.ShouldBe("A full description.");
        reloaded.BrandId.ShouldBe(brand.Id);
        reloaded.CategoryId.ShouldBe(category.Id);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsFeatured.ShouldBeFalse();
        reloaded.IsDeleted.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task SaveChanges_DuplicateSlug_ThrowsDbUpdateException()
    {
        var slug = $"dup-product-{Guid.NewGuid():N}"[..24];
        await SeedProductAsync(name: "First Product", slug: slug);

        var duplicate = await SeedProductAsync(name: "Second Product", slug: $"other-{Guid.NewGuid():N}"[..16]);
        duplicate.UpdateDetails(
            global::Domain.Product.ValueObjects.ProductName.Create("Second Product"),
            global::Domain.Product.ValueObjects.ProductSlug.Create(slug),
            duplicate.Description);
        duplicate.ClearDomainEvents();

        Context.Products.Update(duplicate);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenBrandHasProducts_DeletingBrandIsRestricted()
    {
        var product = await SeedProductAsync();
        Context.ChangeTracker.Clear();

        var brand = await Context.Brands.FirstAsync(b => b.Id == product.BrandId);
        Context.Brands.Remove(brand);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenCategoryHasProducts_DeletingCategoryIsRestricted()
    {
        var product = await SeedProductAsync();
        Context.ChangeTracker.Clear();

        var category = await Context.Categories.FirstAsync(c => c.Id == product.CategoryId);
        Context.Categories.Remove(category);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WhenProductIsDeleted_VariantsAreCascadeDeleted()
    {
        var product = await SeedProductAsync();

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku($"SKU-{Guid.NewGuid():N}"[..20])
            .Build();
        variant.ClearDomainEvents();

        Context.ProductVariants.Add(variant);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.Products.FirstAsync(p => p.Id == product.Id);
        Context.Products.Remove(loaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remaining = await Context.ProductVariants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == variant.Id);

        remaining.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_SoftDeleteLifecycle_PersistsFlags()
    {
        var product = await SeedProductAsync();
        Context.ChangeTracker.Clear();

        var loaded = await Context.Products.FirstAsync(p => p.Id == product.Id);
        loaded.MarkAsDeleted(Guid.NewGuid());
        loaded.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Products.FirstAsync(p => p.Id == product.Id);

        reloaded.IsDeleted.ShouldBeTrue();
        reloaded.IsActive.ShouldBeFalse();
        reloaded.DeletedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Model_MappedToProductsTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("Products");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Products.Id));
    }

    [Fact]
    public void Model_Id_IsNeverValueGenerated()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var id = entityType!.FindProperty(nameof(Products.Id));
        id.ShouldNotBeNull();
        id!.ValueGenerated.ShouldBe(ValueGenerated.Never);
        id.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_Name_IsRequiredWithMaxLengthAndNonUniqueIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Products.Name));
        navigation.ShouldNotBeNull();

        var owned = navigation!.TargetEntityType;
        var value = owned.FindProperty("Value");
        value.ShouldNotBeNull();
        value!.IsNullable.ShouldBeFalse();
        value.GetMaxLength().ShouldBe(global::Domain.Product.ValueObjects.ProductName.MaxLength);
        value.GetColumnName().ShouldBe("Name");

        var index = owned.GetIndexes().SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == "Value");
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeFalse();
    }

    [Fact]
    public void Model_Slug_IsRequiredWithMaxLengthAndUniqueIndex()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Products.Slug));
        navigation.ShouldNotBeNull();

        var owned = navigation!.TargetEntityType;
        var value = owned.FindProperty("Value");
        value.ShouldNotBeNull();
        value!.IsNullable.ShouldBeFalse();
        value.GetColumnName().ShouldBe("Slug");

        var index = owned.GetIndexes().SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == "Value");
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_Brand_ForeignKeyIsRequiredWithRestrictDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var fk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Products.BrandId));

        fk.ShouldNotBeNull();
        fk!.IsRequired.ShouldBeTrue();
        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_Category_ForeignKeyIsRequiredWithRestrictDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var fk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Products.CategoryId));

        fk.ShouldNotBeNull();
        fk!.IsRequired.ShouldBeTrue();
        fk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_HasCascadeDeleteFromProductToVariants()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Products.Variants));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }

    [Fact]
    public void Model_HasExpectedSingleColumnIndexes()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(Products.BrandId), nameof(Products.CategoryId), nameof(Products.IsActive), nameof(Products.IsDeleted) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
        }
    }

    [Fact]
    public void Model_CreatedAtAndUpdatedAt_AreRequired()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(Products.CreatedAt))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(Products.UpdatedAt))!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_HasXminConcurrencyToken()
    {
        var entityType = Context.Model.FindEntityType(typeof(Products));
        entityType.ShouldNotBeNull();

        var xmin = entityType!.FindProperty("xmin");
        xmin.ShouldNotBeNull();
        xmin!.IsConcurrencyToken.ShouldBeTrue();
    }
}
