using Domain.Inventory.Aggregates;
using Domain.Inventory.Entities;
using Domain.Variant.Aggregates;
using Infrastructure.Common.Services;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Inventory.Configurations;

public sealed class StockLedgerEntryConfigurationTests : IDisposable
{
    private readonly DBContext _context;

    public StockLedgerEntryConfigurationTests()
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
        _context.Model.FindEntityType(typeof(StockLedgerEntry))
            ?? throw new InvalidOperationException("StockLedgerEntry entity is not mapped.");

    [Fact]
    public void Configure_PrimaryKey_IsId()
    {
        var entityType = EntityType();

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(StockLedgerEntry.Id));
    }

    [Fact]
    public void Configure_IdempotencyKey_IsRequiredWithMaxLength200()
    {
        var property = EntityType().FindProperty(nameof(StockLedgerEntry.IdempotencyKey));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(200);
    }

    [Fact]
    public void Configure_EventType_IsStoredAsStringWithMaxLength50()
    {
        var property = EntityType().FindProperty(nameof(StockLedgerEntry.EventType));

        property.ShouldNotBeNull();
        property!.GetProviderClrType().ShouldBe(typeof(string));
        property.GetMaxLength().ShouldBe(50);
    }

    [Theory]
    [InlineData(nameof(StockLedgerEntry.ReferenceNumber), 100)]
    [InlineData(nameof(StockLedgerEntry.CorrelationId), 200)]
    [InlineData(nameof(StockLedgerEntry.Note), 500)]
    public void Configure_OptionalTextProperties_HaveExpectedMaxLength(string propertyName, int expectedMaxLength)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(expectedMaxLength);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Configure_UnitCost_UsesDecimal184ColumnType()
    {
        var property = EntityType().FindProperty(nameof(StockLedgerEntry.UnitCost));

        property.ShouldNotBeNull();
        // Npgsql normalizes "decimal(18,4)" to "numeric(18,4)"; precision/scale are preserved.
        property!.GetColumnType().ShouldBe("numeric(18,4)");
    }

    [Fact]
    public void Configure_IdempotencyKeyIndex_IsUnique()
    {
        var index = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(StockLedgerEntry.IdempotencyKey));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Configure_VariantIdIndex_ExistsAndIsNotUnique()
    {
        var index = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(StockLedgerEntry.VariantId));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeFalse();
    }

    [Fact]
    public void Configure_VariantIdCreatedAtCompositeIndex_ExistsWithExpectedSortOrder()
    {
        // IsDescending is only available on the design-time model, not the
        // read-optimized runtime model exposed via DbContext.Model.
        var designModel = _context.GetService<IDesignTimeModel>().Model;
        var entityType = designModel.FindEntityType(typeof(StockLedgerEntry))
            ?? throw new InvalidOperationException("StockLedgerEntry entity is not mapped.");

        var index = entityType
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2 &&
                                 i.Properties[0].Name == nameof(StockLedgerEntry.VariantId) &&
                                 i.Properties[1].Name == nameof(StockLedgerEntry.CreatedAt));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeFalse();
        index.IsDescending.ShouldNotBeNull();
        index.IsDescending![0].ShouldBeFalse();
        index.IsDescending![1].ShouldBeTrue();
    }

    [Fact]
    public void Configure_ProductVariantRelationship_IsManyToOneWithRestrictDeleteBehavior()
    {
        var entityType = EntityType();

        var foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ProductVariant));

        foreignKey.ShouldNotBeNull();
        foreignKey!.IsRequired.ShouldBeTrue();
        foreignKey.IsUnique.ShouldBeFalse();
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        foreignKey.Properties.Select(p => p.Name)
            .ShouldBe([nameof(StockLedgerEntry.VariantId)]);
    }

    [Fact]
    public void Configure_WarehouseRelationship_IsOptionalWithSetNullDeleteBehavior()
    {
        var entityType = EntityType();

        var foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Warehouse));

        foreignKey.ShouldNotBeNull();
        foreignKey!.IsRequired.ShouldBeFalse();
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.SetNull);
        foreignKey.Properties.Select(p => p.Name)
            .ShouldBe([nameof(StockLedgerEntry.WarehouseId)]);
    }
}
