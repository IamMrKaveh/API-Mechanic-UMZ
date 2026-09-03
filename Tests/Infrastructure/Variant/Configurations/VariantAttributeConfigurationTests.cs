using global::Domain.Variant.Entities;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Variant.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class VariantAttributeConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<(global::Domain.Attribute.Aggregates.AttributeType attributeType, global::Domain.Attribute.Entities.AttributeValue attributeValue)> SeedAttributeAsync(
        CancellationToken ct = default)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var attributeType = await new AttributeTypeBuilder()
            .WithName($"Attr{suffix}")
            .WithDisplayName($"Attr {suffix}")
            .BuildAsync(ct);
        var attributeValue = attributeType.AddValue($"Val{suffix}", $"Val {suffix}");
        attributeType.ClearDomainEvents();

        Context.AttributeTypes.Add(attributeType);
        await Context.SaveChangesAsync(ct);
        return (attributeType, attributeValue);
    }

    private async Task<global::Domain.Variant.Aggregates.ProductVariant> SeedVariantAsync(CancellationToken ct = default)
    {
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Attr Product {suffix}")
            .WithSlug($"attr-product-{suffix}")
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

    [Fact]
    public async Task SaveChanges_SetAttributes_PersistsAttributeLink()
    {
        var (attributeType, attributeValue) = await SeedAttributeAsync();
        var variant = await SeedVariantAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "Display Red")]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.VariantAttributes.FirstOrDefaultAsync(a => a.VariantId == variant.Id && a.ValueId == attributeValue.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.VariantId.ShouldBe(variant.Id);
        reloaded.AttributeTypeId.ShouldBe(attributeType.Id);
        reloaded.ValueId.ShouldBe(attributeValue.Id);
        reloaded.DisplayValue.ShouldBe("Display Red");
    }

    [Fact]
    public async Task SaveChanges_SetAttributesTwice_UpdatesDisplayValueWithoutDuplicating()
    {
        var (attributeType, attributeValue) = await SeedAttributeAsync();
        var variant = await SeedVariantAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "First")]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "Second")]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var rows = await Context.VariantAttributes.Where(a => a.VariantId == variant.Id).ToListAsync();
        rows.Count.ShouldBe(1);
        rows[0].DisplayValue.ShouldBe("Second");
    }

    [Fact]
    public async Task SaveChanges_RemoveAssignment_DeletesAttributeRow()
    {
        var (attributeType, attributeValue) = await SeedAttributeAsync();
        var variant = await SeedVariantAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "Temp")]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();

        variant.SetAttributes([]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var count = await Context.VariantAttributes.CountAsync(a => a.VariantId == variant.Id);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task SaveChanges_DuplicateVariantValue_InsertedViaSql_ThrowsDbUpdateException()
    {
        var (attributeType, attributeValue) = await SeedAttributeAsync();
        var variant = await SeedVariantAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "Dup")]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var entityType = Context.Model.FindEntityType(typeof(VariantAttribute))!;
        var table = entityType.GetTableName();
        var columns = entityType.GetProperties().ToDictionary(p => p.Name, p => p.GetColumnName());

        var sql = $"INSERT INTO \"{table}\" (\"{columns["Id"]}\", \"{columns["VariantId"]}\", \"{columns["AttributeTypeId"]}\", \"{columns["ValueId"]}\", \"{columns["DisplayValue"]}\") " +
                  $"VALUES ('{Guid.NewGuid()}', '{variant.Id.Value}', '{attributeType.Id.Value}', '{attributeValue.Id.Value}', 'Dup2')";

        await Should.ThrowAsync<Exception>(() => Context.Database.ExecuteSqlRawAsync(sql));
        Context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task QueryFilter_WhenAttributeTypeIsDeleted_AttributeIsHidden()
    {
        var (attributeType, attributeValue) = await SeedAttributeAsync();
        var variant = await SeedVariantAsync();

        variant.SetAttributes([global::Domain.Variant.ValueObjects.AttributeAssignment.Create(attributeType.Id, attributeValue.Id, "Hidden")]);
        variant.ClearDomainEvents();
        await Context.SaveChangesAsync();

        attributeType.MarkAsDeleted(null);
        attributeType.ClearDomainEvents();
        Context.AttributeTypes.Update(attributeType);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var visible = await Context.VariantAttributes.FirstOrDefaultAsync(a => a.VariantId == variant.Id);
        visible.ShouldBeNull();

        var hidden = await Context.VariantAttributes.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.VariantId == variant.Id);
        hidden.ShouldNotBeNull();
    }

    [Fact]
    public void Model_MappedToProductVariantAttributesTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(VariantAttribute));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("ProductVariantAttributes");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantAttribute));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(VariantAttribute.Id));
    }

    [Fact]
    public void Model_DisplayValue_IsRequiredWithMaxLength256()
    {
        var property = Context.Model.FindEntityType(typeof(VariantAttribute))!.FindProperty(nameof(VariantAttribute.DisplayValue));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(256);
    }

    [Fact]
    public void Model_ValueAndAttributeType_ForeignKeysAreRequiredWithRestrictDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantAttribute));
        entityType.ShouldNotBeNull();

        var valueFk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(VariantAttribute.ValueId));
        valueFk.ShouldNotBeNull();
        valueFk!.IsRequired.ShouldBeTrue();
        valueFk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);

        var typeFk = entityType.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(VariantAttribute.AttributeTypeId));
        typeFk.ShouldNotBeNull();
        typeFk!.IsRequired.ShouldBeTrue();
        typeFk.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_HasUniqueIndexesWithExpectedNames()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantAttribute));
        entityType.ShouldNotBeNull();

        var byValue = entityType!.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_ProductVariantAttributes_Variant_Value");
        byValue.ShouldNotBeNull();
        byValue!.IsUnique.ShouldBeTrue();

        var byType = entityType.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_ProductVariantAttributes_Variant_Type");
        byType.ShouldNotBeNull();
        byType!.IsUnique.ShouldBeTrue();

        var byVariant = entityType.GetIndexes()
            .SingleOrDefault(i => i.GetDatabaseName() == "IX_ProductVariantAttributes_VariantId");
        byVariant.ShouldNotBeNull();
        byVariant!.IsUnique.ShouldBeFalse();
    }

    [Fact]
    public void Model_HasQueryFilter()
    {
        var entityType = Context.Model.FindEntityType(typeof(VariantAttribute));
        entityType.ShouldNotBeNull();
        entityType!.GetQueryFilter().ShouldNotBeNull();
    }
}
