using Domain.Attribute.Entities;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Attribute.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AttributeValueConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task SaveChanges_PersistsAllScalarPropertiesAndRoundTripsAttributeValueId()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("Color")
            .WithDisplayName("Color")
            .BuildAsync();
        var value = attributeType.AddValue("Red", "قرمز", "#FF0000", 5);
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AttributeValues.FirstOrDefaultAsync(v => v.Id == value.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(value.Id);
        reloaded.AttributeTypeId.ShouldBe(attributeType.Id);
        reloaded.Value.ShouldBe("Red");
        reloaded.DisplayValue.ShouldBe("قرمز");
        reloaded.HexCode.ShouldBe("#FF0000");
        reloaded.SortOrder.ShouldBe(5);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsDeleted.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);

        var rowVersion = _context.Entry(reloaded).Property<byte[]>("RowVersion").CurrentValue;
        rowVersion.ShouldNotBeNull();
        rowVersion!.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SaveChanges_WithoutHexCode_PersistsHexCodeAsNull()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("Material")
            .WithDisplayName("Material")
            .BuildAsync();
        var value = attributeType.AddValue("Wood", "چوب");
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AttributeValues.FirstAsync(v => v.Id == value.Id);

        reloaded.HexCode.ShouldBeNull();
    }

    [Fact]
    public async Task QueryFilter_WithSoftDeletedAttributeType_ExcludesValues()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("SoftFilterableSize")
            .WithDisplayName("SoftFilterableSize")
            .BuildAsync();
        var value = attributeType.AddValue("M", "M");
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();

        attributeType.MarkAsDeleted(Guid.NewGuid());
        value.Update("M", "M", null, 0, false);
        var entry = _context.Entry(value);
        entry.Property(nameof(AttributeValue.IsDeleted)).CurrentValue = true;
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var visible = await freshContext.AttributeValues
            .FirstOrDefaultAsync(v => v.Id == value.Id);

        visible.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_WithSoftDeletedAttributeValue_ReturnsSoftDeletedRow()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("IgnoreFilterSize")
            .WithDisplayName("IgnoreFilterSize")
            .BuildAsync();
        var value = attributeType.AddValue("L", "L");
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();

        var entry = _context.Entry(value);
        entry.Property(nameof(AttributeValue.IsDeleted)).CurrentValue = true;
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.AttributeValues
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == value.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Model_Value_IsRequiredAndHasMaxLength100()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AttributeValue.Value));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_DisplayValue_IsRequiredAndHasMaxLength100()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AttributeValue.DisplayValue));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_HexCode_IsNullableAndHasMaxLength50()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AttributeValue.HexCode));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetMaxLength().ShouldBe(50);
    }

    [Fact]
    public void Model_AttributeTypeId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AttributeValue.AttributeTypeId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(AttributeValue.Id));
    }

    [Fact]
    public void Model_HasRowVersionShadowProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        var rowVersion = entityType!.FindProperty("RowVersion");
        rowVersion.ShouldNotBeNull();
        rowVersion!.IsConcurrencyToken.ShouldBeTrue();
        rowVersion.ValueGenerated.ShouldBe(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void Model_HasQueryFilterConfigured()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeValue));
        entityType.ShouldNotBeNull();

        entityType!.GetQueryFilter().ShouldNotBeNull();
    }
}
