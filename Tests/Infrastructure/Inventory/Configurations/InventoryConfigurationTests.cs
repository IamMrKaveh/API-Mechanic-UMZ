using Domain.Inventory.Aggregates;
using Domain.Inventory.ValueObjects;
using Domain.Variant.Aggregates;
using Infrastructure.Common.Services;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;
using InventoryAggregate = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Infrastructure.Inventory.Configurations;

public sealed class InventoryConfigurationTests : IDisposable
{
    private readonly DBContext _context;

    public InventoryConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<DBContext>()
            .UseNpgsql("Host=none;Database=none;Username=none;Password=none;")
            .Options;

        IDateTimeProvider dateTimeProvider = new DateTimeProvider();
        IOutboxEventTypeRegistry registry = new OutboxEventTypeRegistry();

        _context = new DBContext(
            options,
            new AuditableEntityInterceptor(dateTimeProvider),
            new DomainEventInterceptor(registry));
    }

    public void Dispose() => _context.Dispose();

    private IEntityType EntityType() =>
        _context.Model.FindEntityType(typeof(InventoryAggregate))
            ?? throw new InvalidOperationException("Inventory entity is not mapped.");

    [Fact]
    public void Configure_PrimaryKey_IsIdWithValueConverter()
    {
        var entityType = EntityType();

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(InventoryAggregate.Id));

        var idProperty = entityType.FindProperty(nameof(InventoryAggregate.Id));
        idProperty.ShouldNotBeNull();
        idProperty!.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_VariantId_IsRequiredAndHasValueConverter()
    {
        var property = EntityType().FindProperty(nameof(InventoryAggregate.VariantId));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(InventoryAggregate.StockQuantity), "StockQuantity")]
    [InlineData(nameof(InventoryAggregate.ReservedQuantity), "ReservedQuantity")]
    public void Configure_OwnedQuantities_HaveExpectedColumnNameAndRequirement(
        string navigationName,
        string expectedColumnName)
    {
        var entityType = EntityType();

        var navigation = entityType.FindNavigation(navigationName);
        navigation.ShouldNotBeNull();
        navigation!.TargetEntityType.IsOwned().ShouldBeTrue();

        var valueProperty = navigation.TargetEntityType.FindProperty(nameof(StockQuantity.Value));
        valueProperty.ShouldNotBeNull();
        valueProperty!.GetColumnName().ShouldBe(expectedColumnName);
        valueProperty.IsNullable.ShouldBeFalse();
    }

    [Theory]
    [InlineData(nameof(InventoryAggregate.IsUnlimited))]
    [InlineData(nameof(InventoryAggregate.LowStockThreshold))]
    [InlineData(nameof(InventoryAggregate.CreatedAt))]
    public void Configure_RequiredProperties_AreNotNullable(string propertyName)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_UpdatedAt_IsRequiredByClrType()
    {
        // UpdatedAt is a non-nullable DateTime, so EF convention keeps it non-nullable
        // even though the configuration only calls builder.Property(e => e.UpdatedAt).
        var property = EntityType().FindProperty(nameof(InventoryAggregate.UpdatedAt));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_RowVersion_IsConcurrencyToken()
    {
        var property = EntityType().FindProperty("RowVersion");

        property.ShouldNotBeNull();
        property!.IsConcurrencyToken.ShouldBeTrue();
    }

    [Fact]
    public void Configure_VariantIdIndex_IsUnique()
    {
        var index = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(InventoryAggregate.VariantId));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Configure_VariantRelationship_IsOneToOneRequiredWithRestrictDeleteBehavior()
    {
        var entityType = EntityType();

        var foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ProductVariant));

        foreignKey.ShouldNotBeNull();
        foreignKey!.IsRequired.ShouldBeTrue();
        foreignKey.IsUnique.ShouldBeTrue();
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        foreignKey.Properties.Select(p => p.Name)
            .ShouldBe([nameof(InventoryAggregate.VariantId)]);
    }

    [Fact]
    public void Configure_LedgerEntriesRelationship_UsesFieldAccessAndCascadeDelete()
    {
        var entityType = EntityType();

        var navigation = entityType.FindNavigation(nameof(InventoryAggregate.LedgerEntries));
        navigation.ShouldNotBeNull();
        navigation!.IsCollection.ShouldBeTrue();
        navigation.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        navigation.GetPropertyAccessMode().ShouldBe(PropertyAccessMode.Field);
    }

    [Fact]
    public void Configure_TableName_IsInventories()
    {
        EntityType().GetTableName().ShouldBe("Inventories");
    }
}
