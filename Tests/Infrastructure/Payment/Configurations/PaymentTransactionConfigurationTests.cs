using Domain.Payment.Aggregates;
using Infrastructure.Common.Services;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Infrastructure.Payment.Configurations;

public sealed class PaymentTransactionConfigurationTests : IDisposable
{
    private readonly DBContext _context;

    public PaymentTransactionConfigurationTests()
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
        _context.Model.FindEntityType(typeof(PaymentTransaction))
            ?? throw new InvalidOperationException("PaymentTransaction entity is not mapped.");

    [Fact]
    public void Configure_PrimaryKey_IsIdWithValueConverter()
    {
        var entityType = EntityType();

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(PaymentTransaction.Id));

        var idProperty = entityType.FindProperty(nameof(PaymentTransaction.Id));
        idProperty.ShouldNotBeNull();
        idProperty!.GetValueConverter().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(PaymentTransaction.Authority), 100, true)]
    [InlineData(nameof(PaymentTransaction.Gateway), 50, true)]
    [InlineData(nameof(PaymentTransaction.Status), 50, true)]
    public void Configure_ConvertedStringValueObjects_HaveExpectedMaxLengthAndRequirement(
        string propertyName,
        int expectedMaxLength,
        bool expectedRequired)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(expectedMaxLength);
        property.IsNullable.ShouldBe(!expectedRequired);
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_Amount_UsesDecimalColumnTypeAndValueConverter()
    {
        var property = EntityType().FindProperty(nameof(PaymentTransaction.Amount));

        property.ShouldNotBeNull();
        property!.GetColumnType().ShouldBe("numeric(18,2)");
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Configure_OrderId_IsRequiredAndHasValueConverter()
    {
        var property = EntityType().FindProperty(nameof(PaymentTransaction.OrderId));

        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(PaymentTransaction.ErrorMessage), 500)]
    [InlineData(nameof(PaymentTransaction.Description), 500)]
    public void Configure_OptionalTextProperties_HaveExpectedMaxLength(string propertyName, int expectedMaxLength)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.GetMaxLength().ShouldBe(expectedMaxLength);
        property.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Configure_OrderRelationship_IsManyToOneRequiredWithRestrictDeleteBehavior()
    {
        var entityType = EntityType();

        var foreignKey = entityType
            .GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(Orders));

        foreignKey.ShouldNotBeNull();
        foreignKey!.IsRequired.ShouldBeTrue();
        foreignKey.IsUnique.ShouldBeFalse();
        foreignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
        foreignKey.Properties.Select(p => p.Name)
            .ShouldBe([nameof(PaymentTransaction.OrderId)]);
    }

    [Fact]
    public void Configure_AuthorityIndex_IsUnique()
    {
        var authorityIndex = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(PaymentTransaction.Authority));

        authorityIndex.ShouldNotBeNull();
        authorityIndex!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Configure_OrderIdIndex_ExistsAndIsNotUnique()
    {
        var orderIndex = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(PaymentTransaction.OrderId));

        orderIndex.ShouldNotBeNull();
        orderIndex!.IsUnique.ShouldBeFalse();
    }

    [Fact]
    public void Configure_StatusCreatedAtCompositeIndex_ExistsAndIsNotUnique()
    {
        var compositeIndex = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 2 &&
                                 i.Properties[0].Name == nameof(PaymentTransaction.Status) &&
                                 i.Properties[1].Name == nameof(PaymentTransaction.CreatedAt));

        compositeIndex.ShouldNotBeNull();
        compositeIndex!.IsUnique.ShouldBeFalse();
    }

    [Fact]
    public void Configure_UserIdIndex_Exists()
    {
        var userIndex = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.Properties.Count == 1 &&
                                 i.Properties[0].Name == nameof(PaymentTransaction.UserId));

        userIndex.ShouldNotBeNull();
    }

    [Fact]
    public void Configure_QueryFilter_IsConfigured()
    {
        var queryFilter = EntityType().GetQueryFilter();
        queryFilter.ShouldNotBeNull();
    }
}
