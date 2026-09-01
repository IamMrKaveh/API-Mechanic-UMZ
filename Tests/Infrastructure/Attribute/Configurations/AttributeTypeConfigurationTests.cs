using Domain.Attribute.Aggregates;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Attribute.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AttributeTypeConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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
    public async Task SaveChanges_PersistsAllScalarPropertiesAndRoundTripsAttributeTypeId()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("Color")
            .WithDisplayName("Color")
            .WithSortOrder(3)
            .WithIsActive(true)
            .BuildAsync();
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AttributeTypes.FirstOrDefaultAsync(a => a.Id == attributeType.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(attributeType.Id);
        reloaded.Name.ShouldBe("Color");
        reloaded.DisplayName.ShouldBe("Color");
        reloaded.SortOrder.ShouldBe(3);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.IsDeleted.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);

        var rowVersion = _context.Entry(reloaded).Property<byte[]>("RowVersion").CurrentValue;
        rowVersion.ShouldNotBeNull();
        rowVersion!.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task QueryFilter_WithSoftDeletedAttributeType_ExcludesFromDefaultQuery()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("SoftDeletedColor")
            .WithDisplayName("SoftDeletedColor")
            .BuildAsync();
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();

        attributeType.MarkAsDeleted(null);
        _context.AttributeTypes.Update(attributeType);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var visible = await freshContext.AttributeTypes
            .FirstOrDefaultAsync(a => a.Id == attributeType.Id);

        visible.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_WithSoftDeletedAttributeType_ReturnsSoftDeletedRow()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("VisibleSoftDeletedColor")
            .WithDisplayName("VisibleSoftDeletedColor")
            .BuildAsync();
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();

        attributeType.MarkAsDeleted(null);
        _context.AttributeTypes.Update(attributeType);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.AttributeTypes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == attributeType.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsDeleted.ShouldBeTrue();
        loaded.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_WhenAddingValueToAttributeType_CascadesValueOnDelete()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("Size")
            .WithDisplayName("Size")
            .BuildAsync();
        var value = attributeType.AddValue("XL", "XL");
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.AttributeTypes
            .Include(a => a.Values)
            .FirstAsync(a => a.Id == attributeType.Id);

        reloaded.Values.Count.ShouldBe(1);
        reloaded.Values.Single().Id.ShouldBe(value.Id);

        _context.AttributeTypes.Remove(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var remainingValue = await freshContext.AttributeValues
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == value.Id);

        remainingValue.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_UpdatingAttributeType_ChangesRowVersion()
    {
        var attributeType = await new AttributeTypeBuilder()
            .WithName("RowVersionAttribute")
            .WithDisplayName("RowVersionAttribute")
            .BuildAsync();
        attributeType.ClearDomainEvents();

        _context.AttributeTypes.Add(attributeType);
        await _context.SaveChangesAsync();

        var initialRowVersion = _context.Entry(attributeType).Property<byte[]>("RowVersion").CurrentValue!.ToArray();

        attributeType.MarkAsDeleted(Guid.NewGuid());
        await _context.SaveChangesAsync();

        var updatedRowVersion = _context.Entry(attributeType).Property<byte[]>("RowVersion").CurrentValue!;

        updatedRowVersion.SequenceEqual(initialRowVersion).ShouldBeFalse();
    }

    [Fact]
    public void Model_Name_IsRequiredAndHasMaxLength100()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = _context.Model.FindEntityType(typeof(AttributeType));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AttributeType.Name));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_DisplayName_IsRequiredAndHasMaxLength100()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeType));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(AttributeType.DisplayName));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(100);
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeType));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(AttributeType.Id));
    }

    [Fact]
    public void Model_HasRowVersionShadowProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeType));
        entityType.ShouldNotBeNull();

        var rowVersion = entityType!.FindProperty("RowVersion");
        rowVersion.ShouldNotBeNull();
        rowVersion!.IsConcurrencyToken.ShouldBeTrue();
        rowVersion.ValueGenerated.ShouldBe(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void Model_HasQueryFilterConfigured()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeType));
        entityType.ShouldNotBeNull();

        entityType!.GetQueryFilter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasCascadeDeleteFromAttributeTypeToValues()
    {
        var entityType = _context.Model.FindEntityType(typeof(AttributeType));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(AttributeType.Values));
        navigation.ShouldNotBeNull();

        var foreignKey = navigation!.ForeignKey;
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }
}
