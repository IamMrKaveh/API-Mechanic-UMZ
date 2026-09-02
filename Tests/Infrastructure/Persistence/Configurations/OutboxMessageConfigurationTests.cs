using Infrastructure.Common.Services;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfigurationTests : IDisposable
{
    private readonly DBContext _context;

    public OutboxMessageConfigurationTests()
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
        _context.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException("OutboxMessage entity is not mapped.");

    [Fact]
    public void Configure_EntityType_MapsToOutboxMessagesTable()
    {
        var entityType = EntityType();

        entityType.GetTableName().ShouldBe("OutboxMessages");
    }

    [Fact]
    public void Configure_PrimaryKey_IsIdProperty()
    {
        var entityType = EntityType();

        var primaryKey = entityType.FindPrimaryKey();

        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(OutboxMessage.Id));
    }

    [Fact]
    public void Configure_Id_HasValueConverterAndSnakeCaseColumnName()
    {
        var property = EntityType().FindProperty(nameof(OutboxMessage.Id));

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe("id");
        property.GetValueConverter().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(nameof(OutboxMessage.Type), "type", 500, true)]
    [InlineData(nameof(OutboxMessage.TraceParent), "trace_parent", 55, false)]
    [InlineData(nameof(OutboxMessage.TraceState), "trace_state", 256, false)]
    public void Configure_TextualProperties_HaveExpectedColumnMappingAndRequirement(
        string propertyName,
        string expectedColumn,
        int expectedMaxLength,
        bool expectedRequired)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe(expectedColumn);
        property.GetMaxLength().ShouldBe(expectedMaxLength);
        property.IsNullable.ShouldBe(!expectedRequired);
    }

    [Theory]
    [InlineData(nameof(OutboxMessage.Payload), "payload", "text", true)]
    [InlineData(nameof(OutboxMessage.Error), "error", "text", false)]
    public void Configure_TextColumns_UseTextColumnTypeAndExpectedNullability(
        string propertyName,
        string expectedColumn,
        string expectedColumnType,
        bool expectedRequired)
    {
        var property = EntityType().FindProperty(propertyName);

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe(expectedColumn);
        property.GetColumnType().ShouldBe(expectedColumnType);
        property.IsNullable.ShouldBe(!expectedRequired);
    }

    [Fact]
    public void Configure_CreatedAt_IsRequiredAndMappedToCreatedAtColumn()
    {
        var property = EntityType().FindProperty(nameof(OutboxMessage.CreatedAt));

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe("created_at");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_ProcessedAt_IsNullableAndMappedToProcessedAtColumn()
    {
        var property = EntityType().FindProperty(nameof(OutboxMessage.ProcessedAt));

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe("processed_at");
        property.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Configure_RetryCount_IsRequiredAndMappedToRetryCountColumn()
    {
        var property = EntityType().FindProperty(nameof(OutboxMessage.RetryCount));

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe("retry_count");
        property.IsNullable.ShouldBeFalse();
    }

    [Fact]
    public void Configure_IsPoisoned_HasDefaultValueFalseAndIsRequired()
    {
        var property = EntityType().FindProperty(nameof(OutboxMessage.IsPoisoned));

        property.ShouldNotBeNull();
        property!.GetColumnName().ShouldBe("is_poisoned");
        property.IsNullable.ShouldBeFalse();
        property.GetDefaultValue().ShouldBe(false);
    }

    [Fact]
    public void Configure_PendingIndex_UsesExpectedNameAndFilter()
    {
        var index = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_OutboxMessages_Pending");

        index.ShouldNotBeNull();
        index!.Properties.Select(p => p.Name).ShouldBe(new[] { nameof(OutboxMessage.CreatedAt) });
        index.GetFilter().ShouldBe("\"processed_at\" IS NULL AND \"is_poisoned\" = false AND \"retry_count\" < 5");
    }

    [Fact]
    public void Configure_DispatchIndex_UsesExpectedCompositeColumnsAndName()
    {
        var index = EntityType()
            .GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_OutboxMessages_Dispatch");

        index.ShouldNotBeNull();
        index!.Properties.Select(p => p.Name).ShouldBe(new[]
        {
            nameof(OutboxMessage.ProcessedAt),
            nameof(OutboxMessage.IsPoisoned),
            nameof(OutboxMessage.RetryCount),
            nameof(OutboxMessage.CreatedAt)
        });
        index.GetFilter().ShouldBeNull();
    }
}
