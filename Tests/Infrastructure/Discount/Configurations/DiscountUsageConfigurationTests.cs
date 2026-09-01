using Domain.Discount.Entities;
using Infrastructure.Persistence.Context;

namespace Tests.Infrastructure.Discount.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class DiscountUsageConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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
    public void Model_MappedToDiscountUsageRecordsTable()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("DiscountUsageRecords");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(DiscountUsageRecord.Id));
    }

    [Fact]
    public void Model_DiscountCodeId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountUsageRecord.DiscountCodeId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_UserId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountUsageRecord.UserId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_OrderId_IsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountUsageRecord.OrderId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_Code_IsRequiredAndHasMaxLength50()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountUsageRecord.Code));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(50);
    }

    [Fact]
    public void Model_DiscountedAmount_HasDecimal184ColumnTypeAndIsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(DiscountUsageRecord.DiscountedAmount));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeFalse();
        property.GetColumnType().ShouldBe("decimal(18,4)");
    }

    [Fact]
    public void Model_UsageCountAtTimeAndUsedAt_AreRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        entityType!.FindProperty(nameof(DiscountUsageRecord.UsageCountAtTime))!.IsNullable.ShouldBeFalse();
        entityType.FindProperty(nameof(DiscountUsageRecord.UsedAt))!.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Model_HasRestrictDeleteBehaviorOnUser()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(DiscountUsageRecord.User));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_HasRestrictDeleteBehaviorOnOrder()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(DiscountUsageRecord.Order));
        navigation.ShouldNotBeNull();
        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnDiscountCodeId()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(DiscountUsageRecord.DiscountCodeId));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(DiscountUsageRecord.UserId));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasSingleColumnIndexOnOrderId()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 1
                && i.Properties[0].Name == nameof(DiscountUsageRecord.OrderId));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasQueryFilterConfigured()
    {
        var entityType = _context.Model.FindEntityType(typeof(DiscountUsageRecord));
        entityType.ShouldNotBeNull();

        entityType!.GetQueryFilter().ShouldNotBeNull();
    }
}
