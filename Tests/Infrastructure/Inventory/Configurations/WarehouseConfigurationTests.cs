using Domain.Inventory.Aggregates;
using Infrastructure.Common.Services;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Inventory.Configurations;

public sealed class WarehouseConfigurationTests : IDisposable
{
    private readonly DBContext _context;

    public WarehouseConfigurationTests()
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
        _context.Model.FindEntityType(typeof(Warehouse))
            ?? throw new InvalidOperationException("Warehouse entity is not mapped.");

    [Fact]
    public void Configure_PrimaryKey_IsIdWithValueConverter()
    {
        var entityType = EntityType();

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Warehouse.Id));

        var idProperty = entityType.FindProperty(nameof(Warehouse.Id));
        idProperty.ShouldNotBeNull();
        idProperty!.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_Code_IsRequiredWithConverterAndMaxLength50()
    {
        var property = EntityType().FindProperty(nameof(Warehouse.Code));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetValueConverter().ShouldNotBeNull();
        property.GetMaxLength().ShouldBe(50);
    }

    [Theory]
    [InlineData(nameof(Warehouse.Name), 100, true)]
    [InlineData(nameof(Warehouse.City), 100, true)]
    [InlineData(nameof(Warehouse.Address), 500, false)]
    [InlineData(nameof(Warehouse.Phone), 20, false)]
    public void Configure_TextProperties_HaveExpectedMaxLengthAndRequirement(
        string propertyName,
        int expectedMaxLength,
        bool expectedRequired)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(expectedMaxLength);
        property.IsNullable.ShouldBe(!expectedRequired);
    }

    [Theory]
    [InlineData(nameof(Warehouse.IsActive))]
    [InlineData(nameof(Warehouse.IsDefault))]
    [InlineData(nameof(Warehouse.Priority))]
    [InlineData(nameof(Warehouse.CreatedAt))]
    public void Configure_RequiredProperties_AreNotNullable(string propertyName)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_UpdatedAt_IsOptional()
    {
        var property = EntityType().FindProperty(nameof(Warehouse.UpdatedAt));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Configure_RowVersion_IsConcurrencyToken()
    {
        var property = EntityType().FindProperty("RowVersion");

        property.ShouldNotBeNull();
        property!.IsConcurrencyToken.ShouldBeTrue();
    }

    [Fact]
    public void Configure_CodeIndex_IsUnique()
    {
        var index = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(Warehouse.Code));

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Configure_TableName_IsWarehouses()
    {
        EntityType().GetTableName().ShouldBe("Warehouses");
    }
}
